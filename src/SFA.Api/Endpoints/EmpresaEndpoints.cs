using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Empresas;
using SFA.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace SFA.Api.Endpoints;

public static class EmpresaEndpoints
{
    public static void MapEmpresaEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/empresas").RequireAuthorization("Admin");

        // GET paginado: /empresas?search=...&page=1&pageSize=20&order=nome
        g.MapGet("/", async (string? search, SfaDbContext db, int page = 1, int pageSize = 20, string? order = null) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.Empresas.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(e => EF.Functions.ILike(e.Nome, $"%{search}%"));

            q = order?.ToLower() switch
            {
                "nome" => q.OrderBy(e => e.Nome),
                "-nome" => q.OrderByDescending(e => e.Nome),
                "criadoem" => q.OrderBy(e => e.CriadoEm),
                "-criadoem" => q.OrderByDescending(e => e.CriadoEm),
                _ => q.OrderBy(e => e.Id)
            };

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(e => new EmpresaListItemDto(e.Id, e.Nome, e.Ativa, e.CriadoEm))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:int}", async (int id, SfaDbContext db) =>
        {
            var e = await db.Empresas.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new EmpresaListItemDto(x.Id, x.Nome, x.Ativa, x.CriadoEm))
                .FirstOrDefaultAsync();
            return e is null ? Results.NotFound() : Results.Ok(e);
        });

        g.MapPost("/", async (EmpresaCreateDto dto, IValidator<EmpresaCreateDto> v, SfaDbContext db) =>
        {
            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var entity = new Domain.Entities.Empresa { Nome = dto.Nome, Ativa = dto.Ativa };
            db.Empresas.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/empresas/{entity.Id}", new { entity.Id });
        });

        g.MapPut("/{id:int}", async (int id, EmpresaUpdateDto dto, IValidator<EmpresaUpdateDto> v, SfaDbContext db) =>
        {
            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var e = await db.Empresas.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return Results.NotFound();

            e.Nome = dto.Nome;
            e.Ativa = dto.Ativa;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapDelete("/{id:int}", async (int id, SfaDbContext db) =>
        {
            var e = await db.Empresas.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return Results.NotFound();

            db.Empresas.Remove(e);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
