using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Agendamentos;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class AgendamentoEndpoints
{
    public static void MapAgendamentoEndpoints(this IEndpointRouteBuilder app)
    {
        // Admin e Recepcao gerenciam toda a empresa; Profissional vê/edita somente os seus
        var g = app.MapGroup("/api/v1/agendamentos")
                   .RequireAuthorization(policy => policy.RequireRole("Admin", "Recepcao", "Profissional"));

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }

        static int? GetUserId(ClaimsPrincipal user)
        {
            var s = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("sub")?.Value;
            return int.TryParse(s, out var id) ? id : null;
        }

        static bool IsProfissionalOnly(ClaimsPrincipal u)
            => u.Claims.Where(c => c.Type == ClaimTypes.Role).All(c => c.Value is "Profissional");

        // Função para checar overlap (conflito de horário)
        static Task<bool> ExisteConflitoAsync(SfaDbContext db, int codEmp, int profissionalId, DateTimeOffset inicio, DateTimeOffset fim, int? ignorarId = null)
        {
            return db.Agendamentos.AsNoTracking().AnyAsync(a =>
                a.CodEmpresa == codEmp
                && a.ProfissionalId == profissionalId
                && a.Status != "cancelado"
                && (ignorarId == null || a.Id != ignorarId.Value)
                && a.InicioUtc < fim
                && a.FimUtc > inicio
            );
        }

        // GET /agendamentos?profissionalId=&pacienteId=&de=&ate=&status=&page=&pageSize=&order=
        g.MapGet("/", async (ClaimsPrincipal u, int? profissionalId, int? pacienteId, DateTimeOffset? de,
          DateTimeOffset? ate, string? status, SfaDbContext db, int page = 1, int pageSize = 20, string? order = null) =>
        {
            var codEmp = GetCodEmpresa(u);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.Agendamentos.AsNoTracking().Where(a => a.CodEmpresa == codEmp);

            // Profissional só enxerga os próprios
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? 0;
                q = q.Where(a => a.ProfissionalId == uid);
            }
            else
            {
                if (profissionalId.HasValue) q = q.Where(a => a.ProfissionalId == profissionalId);
            }

            if (pacienteId.HasValue) q = q.Where(a => a.PacienteId == pacienteId);
            if (de.HasValue) q = q.Where(a => a.FimUtc > de.Value);
            if (ate.HasValue) q = q.Where(a => a.InicioUtc < ate.Value);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(a => a.Status == status);

            q = order?.ToLower() switch
            {
                "inicio"   => q.OrderBy(a => a.InicioUtc),
                "-inicio"  => q.OrderByDescending(a => a.InicioUtc),
                "fim"      => q.OrderBy(a => a.FimUtc),
                "-fim"     => q.OrderByDescending(a => a.FimUtc),
                _          => q.OrderBy(a => a.Id)
            };

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new AgendamentoListItemDto(a.Id, a.PacienteId, a.ProfissionalId, a.InicioUtc, a.FimUtc, a.Status, a.Observacoes))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // GET /agendamentos/{id}
        g.MapGet("/{id:int}", async (ClaimsPrincipal u, int id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var a = await db.Agendamentos.AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Select(x => new AgendamentoListItemDto(x.Id, x.PacienteId, x.ProfissionalId, x.InicioUtc, x.FimUtc, x.Status, x.Observacoes))
                .FirstOrDefaultAsync();

            if (a is null) return Results.NotFound();

            // Profissional só pode ver se for dele
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? 0;
                if (a.ProfissionalId != uid) return Results.Forbid();
            }

            return Results.Ok(a);
        });

        // POST /agendamentos
        g.MapPost("/", async (ClaimsPrincipal u, AgendamentoCreateDto dto, IValidator<AgendamentoCreateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            // Profissional só pode criar para si mesmo
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? 0;
                if (dto.ProfissionalId != uid) return Results.Forbid();
            }

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            // Confirma existência e escopo
            var pacienteOk = await db.Pacientes.AnyAsync(p => p.Id == dto.PacienteId && p.CodEmpresa == codEmp && p.Ativo);
            if (!pacienteOk) return Results.NotFound(new { message = "paciente_nao_encontrado" });

            var profissionalOk = await db.Usuarios.AnyAsync(p => p.Id == dto.ProfissionalId && p.CodEmpresa == codEmp && p.Ativo);
            if (!profissionalOk) return Results.NotFound(new { message = "profissional_nao_encontrado" });

            // Overlap
            if (await ExisteConflitoAsync(db, codEmp, dto.ProfissionalId, dto.InicioUtc, dto.FimUtc))
                return Results.Conflict(new { message = "conflito_horario_profissional" });

            var userId = GetUserId(u) ?? 0;

            var entity = new Domain.Entities.Agendamento
            {
                CodEmpresa = codEmp,
                PacienteId = dto.PacienteId,
                ProfissionalId = dto.ProfissionalId,
                InicioUtc = dto.InicioUtc,
                FimUtc = dto.FimUtc,
                Status = "agendado",
                Observacoes = dto.Observacoes,
                CriadoPorUsuarioId = userId,
                // CriadoEm default via DB
            };

            db.Agendamentos.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/agendamentos/{entity.Id}", new { entity.Id });
        });

        // PUT /agendamentos/{id}
        g.MapPut("/{id:int}", async (ClaimsPrincipal u, int id, AgendamentoUpdateDto dto, IValidator<AgendamentoUpdateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var entity = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            // Profissional só pode alterar se for dele
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? 0;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            // Revalida overlap se mudou janela
            if (entity.InicioUtc != dto.InicioUtc || entity.FimUtc != dto.FimUtc)
            {
                if (await ExisteConflitoAsync(db, codEmp, entity.ProfissionalId, dto.InicioUtc, dto.FimUtc, ignorarId: id))
                    return Results.Conflict(new { message = "conflito_horario_profissional" });
            }

            entity.InicioUtc = dto.InicioUtc;
            entity.FimUtc = dto.FimUtc;
            entity.Status = dto.Status;
            entity.Observacoes = dto.Observacoes;
            entity.AlteradoPorUsuarioId = GetUserId(u);
            entity.AlteradoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /agendamentos/{id}/confirmar
        g.MapPost("/{id:int}/confirmar", async (ClaimsPrincipal u, int id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var entity = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? 0;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            entity.Status = "confirmado";
            entity.AlteradoPorUsuarioId = GetUserId(u);
            entity.AlteradoEm = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /agendamentos/{id}/cancelar
        g.MapPost("/{id:int}/cancelar", async (ClaimsPrincipal u, int id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var entity = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? 0;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            entity.Status = "cancelado";
            entity.AlteradoPorUsuarioId = GetUserId(u);
            entity.AlteradoEm = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE /agendamentos/{id}
        g.MapDelete("/{id:int}", async (ClaimsPrincipal u, int id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var entity = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? 0;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            db.Agendamentos.Remove(entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
