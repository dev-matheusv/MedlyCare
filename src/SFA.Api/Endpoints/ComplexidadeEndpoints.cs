using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Complexidades;
using SFA.Domain.Entities;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class ComplexidadeEndpoints
{
    public static void MapComplexidadeEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/complexidades")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Profissional"));

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }

        // GET /complexidades
        g.MapGet("/", async (ClaimsPrincipal u, SfaDbContext db, bool? ativo) =>
        {
            var codEmp = GetCodEmpresa(u);

            var q = db.Complexidades
                .AsNoTracking()
                .Where(x => x.CodEmpresa == codEmp);

            if (ativo.HasValue)
                q = q.Where(x => x.Ativo == ativo.Value);

            var items = await q
                .OrderBy(x => x.Descricao)
                .Select(x => new ComplexidadeListItemDto(x.Id, x.Descricao, x.Cor, x.Ativo))
                .ToListAsync();

            return Results.Ok(items);
        });

        // GET /complexidades/{id}
        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var item = await db.Complexidades
                .AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Select(x => new ComplexidadeDto(
                    x.Id, x.CodEmpresa, x.Descricao, x.Cor, x.Ativo, x.CriadoEm, x.AtualizadoEm))
                .FirstOrDefaultAsync();

            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        // POST /complexidades
        g.MapPost("/", async (
            ClaimsPrincipal u,
            ComplexidadeCreateDto dto,
            IValidator<ComplexidadeCreateDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var entity = new Complexidade
            {
                CodEmpresa = codEmp,
                Descricao = dto.Descricao.Trim(),
                Cor = dto.Cor.Trim(),
                Ativo = true,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };

            db.Complexidades.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/complexidades/{entity.Id}", new { entity.Id });
        });

        // PUT /complexidades/{id}
        g.MapPut("/{id:guid}", async (
            ClaimsPrincipal u,
            Guid id,
            ComplexidadeUpdateDto dto,
            IValidator<ComplexidadeUpdateDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var entity = await db.Complexidades
                .FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);

            if (entity is null)
                return Results.NotFound();

            entity.Descricao = dto.Descricao.Trim();
            entity.Cor = dto.Cor.Trim();
            entity.Ativo = dto.Ativo;
            entity.AtualizadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE /complexidades/{id}  (desativa, não exclui fisicamente)
        g.MapDelete("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var entity = await db.Complexidades
                .FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);

            if (entity is null)
                return Results.NotFound();

            // Verifica se há vínculos ativos com pacientes
            var emUso = await db.PacientesComplexidade
                .AnyAsync(pc => pc.ComplexidadeId == id);

            if (emUso)
                return Results.Conflict(new { message = "complexidade_em_uso_por_pacientes" });

            entity.Ativo = false;
            entity.AtualizadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
