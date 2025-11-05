using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using SFA.Application.Usuarios;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/usuarios")
                   .RequireAuthorization("Admin"); // Somente Admin

        // Helper pra pegar cod_empresa das claims
        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }

        // GET /usuarios?search=&ativo=&page=1&pageSize=20&order=nome|-nome|criadoem|-criadoem
        g.MapGet("/", async (ClaimsPrincipal u, string? search, bool? ativo,
          SfaDbContext db, int page = 1, int pageSize = 20, string? order = null) =>
        {
            var codEmp = GetCodEmpresa(u);

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.Usuarios.AsNoTracking().Where(x => x.CodEmpresa == codEmp);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(x =>
                    EF.Functions.ILike(x.Login, $"%{search}%") ||
                    EF.Functions.ILike(x.Nome , $"%{search}%"));

            if (ativo.HasValue) q = q.Where(x => x.Ativo == ativo.Value);

            q = order?.ToLower() switch
            {
                "nome"       => q.OrderBy(x => x.Nome),
                "-nome"      => q.OrderByDescending(x => x.Nome),
                "criadoem"   => q.OrderBy(x => x.CriadoEm),
                "-criadoem"  => q.OrderByDescending(x => x.CriadoEm),
                _            => q.OrderBy(x => x.Id)
            };

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new UsuarioListItemDto(x.Id, x.CodEmpresa, x.Login, x.Nome, x.Ativo, x.CriadoEm))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var x = await db.Usuarios.AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Select(x => new UsuarioListItemDto(x.Id, x.CodEmpresa, x.Login, x.Nome, x.Ativo, x.CriadoEm))
                .FirstOrDefaultAsync();

            return x is null ? Results.NotFound() : Results.Ok(x);
        });

        g.MapPost("/", async (ClaimsPrincipal u, UsuarioCreateDto dto, IValidator<UsuarioCreateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            // Hash no banco
            await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT crypt(@p, gen_salt('bf'))", conn);
            cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, dto.Password);
            var hashObj = await cmd.ExecuteScalarAsync();
            var hash = (hashObj as string) ?? throw new InvalidOperationException("Falha ao gerar hash de senha.");


            // Unicidade (cod_empresa, login)
            var exists = await db.Usuarios.AnyAsync(x => x.CodEmpresa == codEmp && x.Login == dto.Login);
            if (exists) return Results.Conflict(new { field = "login", message = "login já existe nesta empresa" });

            var entity = new Domain.Entities.Usuario
            {
                CodEmpresa = codEmp,
                Login = dto.Login,
                Nome = dto.Nome,
                PasswordHash = hash,
                Ativo = dto.Ativo
            };

            db.Usuarios.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/usuarios/{entity.Id}", new { entity.Id });
        });

        g.MapPut("/{id:guid}", async (ClaimsPrincipal u, Guid id, UsuarioUpdateDto dto, IValidator<UsuarioUpdateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var entity = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            entity.Nome = dto.Nome;
            entity.Ativo = dto.Ativo;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
              await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
              if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
              await using var cmd = new NpgsqlCommand("SELECT crypt(@p, gen_salt('bf'))", conn);
              cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, dto.Password);
              var hashObj = await cmd.ExecuteScalarAsync();
              entity.PasswordHash = (hashObj as string) ?? throw new InvalidOperationException("Falha ao gerar hash de senha.");

            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Ativar / Inativar (atalhos)
        g.MapPost("/{id:guid}/ativar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var entity = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();
            entity.Ativo = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/{id:guid}/inativar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var entity = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();
            entity.Ativo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapDelete("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var entity = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();
            db.Usuarios.Remove(entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
