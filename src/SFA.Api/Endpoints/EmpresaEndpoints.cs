using FluentValidation;
using Microsoft.EntityFrameworkCore;
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

            var documentoJaExiste = await db.Empresas.AnyAsync(x => x.Documento == dto.Documento);
            if (documentoJaExiste)
                return Results.Conflict(new { field = "documento", message = "documento já cadastrado" });

            var emailJaExiste = await db.Empresas.AnyAsync(x => x.Email == dto.Email);
            if (emailJaExiste)
                return Results.Conflict(new { field = "email", message = "email já cadastrado" });

            var entity = new Domain.Entities.Empresa
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

            db.Empresas.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/empresas/{entity.Id}", new { entity.Id, entity.CodEmpresa });
        });

        g.MapPut("/{id:guid}", async (Guid id, EmpresaUpdateDto dto, IValidator<EmpresaUpdateDto> v, SfaDbContext db) =>
        {
            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var e = await db.Empresas.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return Results.NotFound();

            var documentoJaExiste = await db.Empresas.AnyAsync(x => x.Documento == dto.Documento && x.Id != id);
            if (documentoJaExiste)
                return Results.Conflict(new { field = "documento", message = "documento já cadastrado" });

            var emailJaExiste = await db.Empresas.AnyAsync(x => x.Email == dto.Email && x.Id != id);
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
