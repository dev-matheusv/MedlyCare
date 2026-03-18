using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using SFA.Application.Empresas;
using SFA.Infrastructure;

namespace SFA.Api.Endpoints;

public static class EmpresaEndpoints
{
    public static void MapEmpresaEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/empresas").RequireAuthorization("Admin");

        g.MapGet("/", async (string? search, SfaDbContext db, int page = 1, int pageSize = 20, string? order = null) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.Empresas.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(e =>
                    EF.Functions.ILike(e.RazaoSocial, $"%{search}%") ||
                    EF.Functions.ILike(e.Documento, $"%{search}%") ||
                    EF.Functions.ILike(e.Email, $"%{search}%") ||
                    EF.Functions.ILike(e.Cidade, $"%{search}%"));
            }

            q = order?.ToLower() switch
            {
                "razaosocial" => q.OrderBy(e => e.RazaoSocial),
                "-razaosocial" => q.OrderByDescending(e => e.RazaoSocial),
                "criadoem" => q.OrderBy(e => e.CriadoEm),
                "-criadoem" => q.OrderByDescending(e => e.CriadoEm),
                _ => q.OrderBy(e => e.CodEmpresa)
            };

            var total = await q.CountAsync();

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmpresaListItemDto(
                    e.Id,
                    e.CodEmpresa,
                    e.RazaoSocial,
                    e.Documento,
                    e.Email,
                    e.Telefone,
                    e.CelularWhatsapp,
                    e.UtilizarCelularParaEnvioMensagens,
                    e.Cidade,
                    e.Uf,
                    e.ResponsavelClinica,
                    e.Ativa,
                    e.CriadoEm
                ))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (Guid id, SfaDbContext db) =>
        {
            var e = await db.Empresas.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new EmpresaDetailsDto(
                    x.Id,
                    x.CodEmpresa,
                    x.RazaoSocial,
                    x.Documento,
                    x.InscricaoEstadualRg,
                    x.Email,
                    x.Telefone,
                    x.CelularWhatsapp,
                    x.UtilizarCelularParaEnvioMensagens,
                    x.Endereco,
                    x.NumeroImovel,
                    x.Bairro,
                    x.Cidade,
                    x.Uf,
                    x.Cep,
                    x.Pais,
                    x.ResponsavelClinica,
                    x.PathLogotipo,
                    x.Cnae,
                    x.RedesSociais,
                    x.Ativa,
                    x.CriadoEm
                ))
                .FirstOrDefaultAsync();

            return e is null ? Results.NotFound() : Results.Ok(e);
        });

        g.MapGet("/codigo/{codEmpresa:int}", async (int codEmpresa, SfaDbContext db) =>
        {
            var e = await db.Empresas.AsNoTracking()
                .Where(x => x.CodEmpresa == codEmpresa)
                .Select(x => new EmpresaDetailsDto(
                    x.Id,
                    x.CodEmpresa,
                    x.RazaoSocial,
                    x.Documento,
                    x.InscricaoEstadualRg,
                    x.Email,
                    x.Telefone,
                    x.CelularWhatsapp,
                    x.UtilizarCelularParaEnvioMensagens,
                    x.Endereco,
                    x.NumeroImovel,
                    x.Bairro,
                    x.Cidade,
                    x.Uf,
                    x.Cep,
                    x.Pais,
                    x.ResponsavelClinica,
                    x.PathLogotipo,
                    x.Cnae,
                    x.RedesSociais,
                    x.Ativa,
                    x.CriadoEm
                ))
                .FirstOrDefaultAsync();

            return e is null ? Results.NotFound() : Results.Ok(e);
        });

        g.MapPost("/", async (EmpresaCreateDto dto, IValidator<EmpresaCreateDto> v, SfaDbContext db) =>
        {
            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var documentoJaExiste = await db.Empresas.AnyAsync(x =>
              EF.Functions.ILike(x.Documento, dto.Documento));

            if (documentoJaExiste)
              return Results.Conflict(new { field = "documento", message = "documento já cadastrado" });

            var emailJaExiste = await db.Empresas.AnyAsync(x =>
              EF.Functions.ILike(x.Email, dto.Email));

            if (emailJaExiste)
              return Results.Conflict(new { field = "email", message = "email já cadastrado" });

            var loginAdminJaExisteGlobal = await db.Usuarios.AnyAsync(x =>
              EF.Functions.ILike(x.Login, dto.LoginUsuarioAdmin));

            if (loginAdminJaExisteGlobal)
              return Results.Conflict(new { field = "loginUsuarioAdmin", message = "login do admin já está em uso" });

            var emailAdminJaExisteGlobal = await db.Usuarios.AnyAsync(x =>
              EF.Functions.ILike(x.Email, dto.EmailUsuarioAdmin));

            if (emailAdminJaExisteGlobal)
              return Results.Conflict(new { field = "emailUsuarioAdmin", message = "email do admin já está em uso" });

            await using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var empresa = new Domain.Entities.Empresa
                {
                    RazaoSocial = dto.RazaoSocial,
                    Documento = dto.Documento,
                    InscricaoEstadualRg = dto.InscricaoEstadualRg,
                    Email = dto.Email,
                    Telefone = dto.Telefone,
                    CelularWhatsapp = dto.CelularWhatsapp,
                    UtilizarCelularParaEnvioMensagens = dto.UtilizarCelularParaEnvioMensagens,
                    Endereco = dto.Endereco,
                    NumeroImovel = dto.NumeroImovel,
                    Bairro = dto.Bairro,
                    Cidade = dto.Cidade,
                    Uf = dto.Uf,
                    Cep = dto.Cep,
                    Pais = dto.Pais,
                    ResponsavelClinica = dto.ResponsavelClinica,
                    PathLogotipo = dto.PathLogotipo,
                    Cnae = dto.Cnae,
                    RedesSociais = dto.RedesSociais,
                    Ativa = dto.Ativa
                };

                db.Empresas.Add(empresa);
                await db.SaveChangesAsync();

                var perfilAdmin = await db.Perfis.FirstOrDefaultAsync(p =>
                    p.CodEmpresa == empresa.CodEmpresa &&
                    p.Nome == "Admin");

                if (perfilAdmin is null)
                {
                    perfilAdmin = new Domain.Entities.Perfil
                    {
                        CodEmpresa = empresa.CodEmpresa,
                        Nome = "Admin",
                        Ativo = true,
                        CriadoEm = DateTime.UtcNow
                    };

                    db.Perfis.Add(perfilAdmin);
                    await db.SaveChangesAsync();
                }

                await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand("SELECT crypt(@p, gen_salt('bf'))", conn);
                cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, dto.SenhaUsuarioAdmin);
                var hashObj = await cmd.ExecuteScalarAsync();
                var hash = (hashObj as string) ?? throw new InvalidOperationException("Falha ao gerar hash de senha.");

                var usuarioAdmin = new Domain.Entities.Usuario
                {
                    CodEmpresa = empresa.CodEmpresa,
                    Login = dto.LoginUsuarioAdmin,
                    Nome = dto.NomeUsuarioAdmin,
                    Email = dto.EmailUsuarioAdmin,
                    Telefone = dto.TelefoneUsuarioAdmin,
                    CelularWhatsapp = dto.CelularWhatsappUsuarioAdmin,
                    PasswordHash = hash,
                    Ativo = true
                };

                db.Usuarios.Add(usuarioAdmin);
                await db.SaveChangesAsync();

                db.UsuariosPerfis.Add(new Domain.Entities.UsuarioPerfil
                {
                    UsuarioId = usuarioAdmin.Id,
                    PerfilId = perfilAdmin.Id
                });

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Created($"/api/v1/empresas/{empresa.Id}", new
                {
                    empresa.Id,
                    empresa.CodEmpresa,
                    UsuarioAdminId = usuarioAdmin.Id
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        g.MapPut("/{id:guid}", async (Guid id, EmpresaUpdateDto dto, IValidator<EmpresaUpdateDto> v, SfaDbContext db) =>
        {
            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var e = await db.Empresas.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return Results.NotFound();

            var documentoJaExiste = await db.Empresas.AnyAsync(x =>
              x.Id != id &&
              EF.Functions.ILike(x.Documento, dto.Documento));

            if (documentoJaExiste)
              return Results.Conflict(new { field = "documento", message = "documento já cadastrado" });

            var emailJaExiste = await db.Empresas.AnyAsync(x =>
              x.Id != id &&
              EF.Functions.ILike(x.Email, dto.Email));

            if (emailJaExiste)
              return Results.Conflict(new { field = "email", message = "email já cadastrado" });

            e.RazaoSocial = dto.RazaoSocial;
            e.Documento = dto.Documento;
            e.InscricaoEstadualRg = dto.InscricaoEstadualRg;
            e.Email = dto.Email;
            e.Telefone = dto.Telefone;
            e.CelularWhatsapp = dto.CelularWhatsapp;
            e.UtilizarCelularParaEnvioMensagens = dto.UtilizarCelularParaEnvioMensagens;
            e.Endereco = dto.Endereco;
            e.NumeroImovel = dto.NumeroImovel;
            e.Bairro = dto.Bairro;
            e.Cidade = dto.Cidade;
            e.Uf = dto.Uf;
            e.Cep = dto.Cep;
            e.Pais = dto.Pais;
            e.ResponsavelClinica = dto.ResponsavelClinica;
            e.PathLogotipo = dto.PathLogotipo;
            e.Cnae = dto.Cnae;
            e.RedesSociais = dto.RedesSociais;
            e.Ativa = dto.Ativa;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapDelete("/{id:guid}", async (Guid id, SfaDbContext db) =>
        {
            var e = await db.Empresas.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return Results.NotFound();

            db.Empresas.Remove(e);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
