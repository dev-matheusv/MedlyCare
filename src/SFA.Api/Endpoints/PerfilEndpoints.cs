using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Perfis;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class PerfilEndpoints
{
    public static void MapPerfilEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/perfis")
                   .RequireAuthorization("Admin");

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }

        // GET /perfis
        g.MapGet("/", async (ClaimsPrincipal u, string? search, bool? ativo,
          SfaDbContext db, int page = 1, int pageSize = 20, string? order = null) =>
        {
            var codEmp = GetCodEmpresa(u);

            var q = db.Perfis.AsNoTracking().Where(p => p.CodEmpresa == codEmp);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(p => EF.Functions.ILike(p.Nome, $"%{search}%"));

            if (ativo.HasValue)
                q = q.Where(p => p.Ativo == ativo.Value);

            q = order?.ToLower() switch
            {
                "nome"      => q.OrderBy(p => p.Nome),
                "-nome"     => q.OrderByDescending(p => p.Nome),
                "criadoem"  => q.OrderBy(p => p.CriadoEm),
                "-criadoem" => q.OrderByDescending(p => p.CriadoEm),
                _           => q.OrderBy(p => p.Id)
            };

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new PerfilDto(p.Id, p.CodEmpresa, p.Nome, p.Ativo, p.CriadoEm))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // GET /perfis/{id}
        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var p = await db.Perfis.AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Select(x => new PerfilDto(x.Id, x.CodEmpresa, x.Nome, x.Ativo, x.CriadoEm))
                .FirstOrDefaultAsync();

            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        // POST /perfis
        g.MapPost("/", async (ClaimsPrincipal u, PerfilCreateDto dto, IValidator<PerfilCreateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var exists = await db.Perfis.AnyAsync(p => p.CodEmpresa == codEmp && p.Nome == dto.Nome);
            if (exists) return Results.Conflict(new { field = "nome", message = "Perfil já existe nesta empresa" });

            var entity = new Domain.Entities.Perfil
            {
                CodEmpresa = codEmp,
                Nome = dto.Nome,
                Ativo = dto.Ativo
            };

            db.Perfis.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/perfis/{entity.Id}", new { entity.Id });
        });

        // PUT /perfis/{id}
        g.MapPut("/{id:guid}", async (ClaimsPrincipal u, Guid id, PerfilUpdateDto dto, IValidator<PerfilUpdateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var entity = await db.Perfis.FirstOrDefaultAsync(p => p.Id == id && p.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            var conflict = await db.Perfis.AnyAsync(p => p.CodEmpresa == codEmp && p.Nome == dto.Nome && p.Id != id);
            if (conflict) return Results.Conflict(new { field = "nome", message = "Perfil já existe nesta empresa" });

            entity.Nome = dto.Nome;
            entity.Ativo = dto.Ativo;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /perfis/{id}/ativar
        g.MapPost("/{id:guid}/ativar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var entity = await db.Perfis.FirstOrDefaultAsync(p => p.Id == id && p.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();
            entity.Ativo = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /perfis/{id}/inativar
        g.MapPost("/{id:guid}/inativar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var entity = await db.Perfis.FirstOrDefaultAsync(p => p.Id == id && p.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();
            entity.Ativo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE /perfis/{id}
        g.MapDelete("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var entity = await db.Perfis.FirstOrDefaultAsync(p => p.Id == id && p.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            db.Perfis.Remove(entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
