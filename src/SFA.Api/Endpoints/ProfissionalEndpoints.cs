using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using SFA.Application.Usuarios;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class ProfissionalEndpoints
{
    public static void MapProfissionalEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/profissionais")
            .RequireAuthorization("Admin");

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }

        g.MapGet("/", async (ClaimsPrincipal u, string? search, bool? ativo, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var q = db.Usuarios
                .AsNoTracking()
                .Where(x => x.CodEmpresa == codEmp)
                .Where(x => x.UsuariosPerfis.Any(up => up.Perfil.Nome == "Profissional"));

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(x =>
                    EF.Functions.ILike(x.Nome, $"%{search}%") ||
                    EF.Functions.ILike(x.Login, $"%{search}%") ||
                    EF.Functions.ILike(x.Email, $"%{search}%"));
            }

            if (ativo.HasValue)
                q = q.Where(x => x.Ativo == ativo.Value);

            var items = await q
                .OrderBy(x => x.Nome)
                .Select(x => new UsuarioListItemDto(
                    x.Id,
                    x.CodEmpresa,
                    x.Login,
                    x.Nome,
                    x.Email,
                    x.Telefone,
                    x.CelularWhatsapp,
                    x.Ativo,
                    x.CriadoEm
                ))
                .ToListAsync();

            return Results.Ok(items);
        });

        g.MapPost("/", async (ClaimsPrincipal u, UsuarioCreateDto dto, IValidator<UsuarioCreateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var perfilProfissional = await db.Perfis
                .FirstOrDefaultAsync(p => p.CodEmpresa == codEmp && p.Nome == "Profissional" && p.Ativo);

            if (perfilProfissional is null)
                return Results.BadRequest(new { message = "perfil_profissional_nao_encontrado" });

            var loginExists = await db.Usuarios.AnyAsync(x => x.CodEmpresa == codEmp && x.Login == dto.Login);
            if (loginExists)
                return Results.Conflict(new { field = "login", message = "login já existe nesta empresa" });

            var emailExists = await db.Usuarios.AnyAsync(x => x.CodEmpresa == codEmp && x.Email == dto.Email);
            if (emailExists)
                return Results.Conflict(new { field = "email", message = "email já existe nesta empresa" });

            await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT crypt(@p, gen_salt('bf'))", conn);
            cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, dto.Password);
            var hashObj = await cmd.ExecuteScalarAsync();
            var hash = (hashObj as string) ?? throw new InvalidOperationException("Falha ao gerar hash de senha.");

            var entity = new Domain.Entities.Usuario
            {
                CodEmpresa = codEmp,
                Login = dto.Login,
                Nome = dto.Nome,
                Email = dto.Email,
                Telefone = dto.Telefone,
                CelularWhatsapp = dto.CelularWhatsapp,
                PasswordHash = hash,
                Ativo = dto.Ativo
            };

            db.Usuarios.Add(entity);
            await db.SaveChangesAsync();

            db.UsuariosPerfis.Add(new Domain.Entities.UsuarioPerfil
            {
                UsuarioId = entity.Id,
                PerfilId = perfilProfissional.Id
            });

            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/profissionais/{entity.Id}", new { entity.Id });
        });
    }
}
