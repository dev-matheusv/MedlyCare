using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Atendimentos;
using SFA.Domain.Entities;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class AtendimentoEndpoints
{
    public static void MapAtendimentoEndpoints(this IEndpointRouteBuilder app)
    {
        // Admin e Recepcao gerenciam toda a empresa; Profissional vê/edita somente os seus
        var g = app.MapGroup("/api/v1/atendimentos")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Recepcao", "Profissional"));

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }

        static Guid? GetUserId(ClaimsPrincipal user)
        {
            var s = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            return Guid.TryParse(s, out var id) ? id : null;
        }

        static bool IsProfissionalOnly(ClaimsPrincipal u)
            => u.Claims.Where(c => c.Type == ClaimTypes.Role).All(c => c.Value is "Profissional");

        // GET /atendimentos?profissionalId=&pacienteId=&de=&ate=&status=&page=&pageSize=&order=
        g.MapGet("/", async (ClaimsPrincipal u,
            Guid? profissionalId,
            Guid? pacienteId,
            DateTimeOffset? de,
            DateTimeOffset? ate,
            string? status,
            SfaDbContext db,
            int page = 1,
            int pageSize = 20,
            string? order = null) =>
        {
            var codEmp = GetCodEmpresa(u);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.Atendimentos.AsNoTracking()
                .Where(a => a.CodEmpresa == codEmp);

            // Profissional só enxerga os próprios
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                q = q.Where(a => a.ProfissionalId == uid);
            }
            else
            {
                if (profissionalId.HasValue) q = q.Where(a => a.ProfissionalId == profissionalId.Value);
            }

            if (pacienteId.HasValue) q = q.Where(a => a.PacienteId == pacienteId.Value);
            if (de.HasValue) q = q.Where(a => a.FinalizadoUtc == null ? a.InicioUtc >= de.Value : a.FinalizadoUtc > de.Value);
            if (ate.HasValue) q = q.Where(a => a.InicioUtc < ate.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                // status esperado: "aberto", "finalizado", "cancelado"
                status = status.Trim().ToLowerInvariant();
                q = status switch
                {
                    "aberto" => q.Where(a => a.Status == AtendimentoStatus.Aberto),
                    "finalizado" => q.Where(a => a.Status == AtendimentoStatus.Finalizado),
                    "cancelado" => q.Where(a => a.Status == AtendimentoStatus.Cancelado),
                    _ => q
                };
            }

            q = order?.ToLowerInvariant() switch
            {
                "inicio" => q.OrderBy(a => a.InicioUtc),
                "-inicio" => q.OrderByDescending(a => a.InicioUtc),
                "finalizado" => q.OrderBy(a => a.FinalizadoUtc),
                "-finalizado" => q.OrderByDescending(a => a.FinalizadoUtc),
                _ => q.OrderByDescending(a => a.InicioUtc)
            };

            var total = await q.CountAsync();

            // Joins leves para trazer nomes, igual agendamento
            var projected = q
                .Join(db.Pacientes.AsNoTracking(),
                    a => a.PacienteId, p => p.Id,
                    (a, p) => new { a, PacienteNome = p.Nome })
                .Join(db.Usuarios.AsNoTracking(),
                    ap => ap.a.ProfissionalId, u2 => u2.Id,
                    (ap, prof) => new { ap.a, ap.PacienteNome, ProfNome = prof.Nome });

            var items = await projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AtendimentoListItemDto(
                    x.a.Id,
                    new PessoaDto(x.a.PacienteId, x.PacienteNome),
                    new PessoaDto(x.a.ProfissionalId, x.ProfNome),
                    x.a.InicioUtc,
                    x.a.FinalizadoUtc,
                    x.a.Status.ToString().ToLowerInvariant(),
                    x.a.Observacoes))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // GET /atendimentos/{id}
        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
          var codEmp = GetCodEmpresa(u);

          var a = await db.Atendimentos.AsNoTracking()
            .Where(x => x.Id == id && x.CodEmpresa == codEmp)
            .Join(db.Pacientes.AsNoTracking(),
              x => x.PacienteId, p => p.Id,
              (x, p) => new { x, PacienteNome = p.Nome })
            .Join(db.Usuarios.AsNoTracking(),
              xp => xp.x.ProfissionalId, u2 => u2.Id,
              (xp, prof) => new AtendimentoDetailsDto(
                xp.x.Id,
                new PessoaDto(xp.x.PacienteId, xp.PacienteNome),
                new PessoaDto(xp.x.ProfissionalId, prof.Nome),
                xp.x.AgendamentoId,
                xp.x.Status.ToString().ToLowerInvariant(),
                xp.x.InicioUtc,
                xp.x.FinalizadoUtc,
                xp.x.Observacoes))
            .FirstOrDefaultAsync();

          if (a is null) return Results.NotFound();

          // Profissional só pode ver se for dele
          if (IsProfissionalOnly(u))
          {
            var uid = GetUserId(u) ?? Guid.Empty;
            if (a.Profissional.Id != uid) return Results.Forbid();
          }

          return Results.Ok(a);
        });

        // GET /atendimentos/por-agendamento/{agendamentoId}
        g.MapGet("/por-agendamento/{agendamentoId:guid}", async (ClaimsPrincipal u, Guid agendamentoId, SfaDbContext db) =>
        {
          var codEmp = GetCodEmpresa(u);

          var a = await db.Atendimentos.AsNoTracking()
            .Where(x => x.CodEmpresa == codEmp && x.AgendamentoId == agendamentoId)
            .Join(db.Pacientes.AsNoTracking(),
              x => x.PacienteId, p => p.Id,
              (x, p) => new { x, PacienteNome = p.Nome })
            .Join(db.Usuarios.AsNoTracking(),
              xp => xp.x.ProfissionalId, u2 => u2.Id,
              (xp, prof) => new AtendimentoDetailsDto(
                xp.x.Id,
                new PessoaDto(xp.x.PacienteId, xp.PacienteNome),
                new PessoaDto(xp.x.ProfissionalId, prof.Nome),
                xp.x.AgendamentoId,
                xp.x.Status.ToString().ToLowerInvariant(),
                xp.x.InicioUtc,
                xp.x.FinalizadoUtc,
                xp.x.Observacoes))
            .FirstOrDefaultAsync();

          if (a is null) return Results.NotFound();

          if (IsProfissionalOnly(u))
          {
            var uid = GetUserId(u) ?? Guid.Empty;
            if (a.Profissional.Id != uid) return Results.Forbid();
          }

          return Results.Ok(a);
        });

        // POST /atendimentos
        g.MapPost("/", async (ClaimsPrincipal u, AtendimentoCreateDto dto, IValidator<AtendimentoCreateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            // Profissional só pode criar para si mesmo
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (dto.ProfissionalId != uid) return Results.Forbid();
            }

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            // Confirma existência e escopo
            var pacienteOk = await db.Pacientes.AnyAsync(p => p.Id == dto.PacienteId && p.CodEmpresa == codEmp && p.Ativo);
            if (!pacienteOk) return Results.NotFound(new { message = "paciente_nao_encontrado" });

            var profissionalOk = await db.Usuarios.AnyAsync(p => p.Id == dto.ProfissionalId && p.CodEmpresa == codEmp && p.Ativo);
            if (!profissionalOk) return Results.NotFound(new { message = "profissional_nao_encontrado" });

            // Se veio agendamento, valida e garante 1:1
            if (dto.AgendamentoId is not null)
            {
                var agendamentoOk = await db.Agendamentos.AnyAsync(a => a.Id == dto.AgendamentoId && a.CodEmpresa == codEmp && a.Status != "cancelado");
                if (!agendamentoOk) return Results.NotFound(new { message = "agendamento_nao_encontrado" });

                var jaExiste = await db.Atendimentos.AnyAsync(a => a.CodEmpresa == codEmp && a.AgendamentoId == dto.AgendamentoId);
                if (jaExiste) return Results.Conflict(new { message = "atendimento_ja_existe_para_agendamento" });
            }

            var userId = GetUserId(u) ?? Guid.Empty;

            var entity = new Atendimento
            {
                CodEmpresa = codEmp,
                PacienteId = dto.PacienteId,
                ProfissionalId = dto.ProfissionalId,
                AgendamentoId = dto.AgendamentoId,
                Status = AtendimentoStatus.Aberto,
                InicioUtc = dto.InicioUtc ?? DateTimeOffset.UtcNow,
                Observacoes = dto.Observacoes,
                CriadoPorUsuarioId = userId,
                // CriadoEm default via DB
            };

            db.Atendimentos.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/atendimentos/{entity.Id}", new { entity.Id });
        });

        // PUT /atendimentos/{id}
        // (por enquanto só Observacoes; depois podemos abrir campos extras)
        g.MapPut("/{id:guid}", async (ClaimsPrincipal u, Guid id, AtendimentoUpdateDto dto, IValidator<AtendimentoUpdateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var entity = await db.Atendimentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            // Profissional só pode alterar se for dele
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            if (entity.Status != AtendimentoStatus.Aberto)
                return Results.Conflict(new { message = "somente_aberto_pode_editar" });

            entity.Observacoes = dto.Observacoes;
            entity.AlteradoPorUsuarioId = GetUserId(u);
            entity.AlteradoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /atendimentos/{id}/finalizar
        g.MapPost("/{id:guid}/finalizar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var entity = await db.Atendimentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            // Profissional só pode finalizar se for dele
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            if (entity.Status != AtendimentoStatus.Aberto)
                return Results.Conflict(new { message = "somente_aberto_pode_finalizar" });

            entity.Status = AtendimentoStatus.Finalizado;
            entity.FinalizadoUtc = DateTimeOffset.UtcNow;
            entity.AlteradoPorUsuarioId = GetUserId(u);
            entity.AlteradoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /atendimentos/{id}/cancelar
        g.MapPost("/{id:guid}/cancelar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var entity = await db.Atendimentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            // Profissional só pode cancelar se for dele
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            if (entity.Status != AtendimentoStatus.Aberto)
                return Results.Conflict(new { message = "somente_aberto_pode_cancelar" });

            entity.Status = AtendimentoStatus.Cancelado;
            entity.AlteradoPorUsuarioId = GetUserId(u);
            entity.AlteradoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE /atendimentos/{id}
        g.MapDelete("/{id:guid}", async (ClaimsPrincipal u, Guid id, string? motivo, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            if (string.IsNullOrWhiteSpace(motivo)) return Results.BadRequest(new { message = "motivo_obrigatorio" });

            var entity = await db.Atendimentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            // Profissional só pode deletar se for dele
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            entity.IsDeleted = true;
            entity.DeletedAt = DateTimeOffset.UtcNow;
            entity.DeletedBy = GetUserId(u);
            entity.DeletedReason = motivo;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        
        // GET /atendimentos/events
        g.MapGet("/events", async (ClaimsPrincipal u,
            DateTimeOffset de,
            DateTimeOffset ate,
            Guid? profissionalId,
            Guid? pacienteId,
            string? status,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var q = db.Atendimentos.AsNoTracking()
                .Where(a => a.CodEmpresa == codEmp &&
                            a.InicioUtc < ate &&
                            (a.FinalizadoUtc == null || a.FinalizadoUtc > de));

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                q = q.Where(a => a.ProfissionalId == uid);
            }
            else if (profissionalId.HasValue)
            {
                q = q.Where(a => a.ProfissionalId == profissionalId.Value);
            }

            if (pacienteId.HasValue) q = q.Where(a => a.PacienteId == pacienteId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim().ToLowerInvariant();
                q = status switch
                {
                    "aberto" => q.Where(a => a.Status == AtendimentoStatus.Aberto),
                    "finalizado" => q.Where(a => a.Status == AtendimentoStatus.Finalizado),
                    "cancelado" => q.Where(a => a.Status == AtendimentoStatus.Cancelado),
                    _ => q
                };
            }

            var events = await q
                .Join(db.Pacientes.AsNoTracking(),
                    a => a.PacienteId, p => p.Id,
                    (a, p) => new { a, PacienteNome = p.Nome })
                .Join(db.Usuarios.AsNoTracking(),
                    ap => ap.a.ProfissionalId, u2 => u2.Id,
                    (ap, prof) => new
                    {
                        ap.a.Id,
                        ap.a.InicioUtc,
                        ap.a.FinalizadoUtc,
                        ap.a.Status,
                        ap.a.PacienteId,
                        ap.a.ProfissionalId,
                        ap.a.AgendamentoId,
                        ap.PacienteNome,
                        ProfNome = prof.Nome
                    })
                .OrderBy(x => x.InicioUtc)
                .Select(x => new AtendimentoEventDto(
                  x.Id,
                  $"{x.PacienteNome} • {x.ProfNome}",
                  x.InicioUtc,
                  x.FinalizadoUtc,
                  x.Status.ToString().ToLowerInvariant(),
                  x.Status == AtendimentoStatus.Finalizado ? "#22c55e"
                  : x.Status == AtendimentoStatus.Cancelado ? "#ef4444"
                  : "#3b82f6",
                  x.PacienteId,
                  x.ProfissionalId,
                  x.AgendamentoId
                ))
                .ToListAsync();

            return Results.Ok(events);
        });
    }
}
