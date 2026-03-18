using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Receituarios;
using SFA.Domain.Entities;
using SFA.Infrastructure;

namespace SFA.Api.Endpoints;

public static class ReceituarioMedicoEndpoints
{
    public static void MapReceituarioMedicoEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/receituarios-medicos")
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

        static IResult Html(string html)
            => Results.Content(html, "text/html; charset=utf-8");

        g.MapGet("/", async (
            ClaimsPrincipal u,
            Guid? pacienteId,
            Guid? profissionalId,
            bool? cancelado,
            SfaDbContext db,
            int page = 1,
            int pageSize = 20) =>
        {
            var codEmp = GetCodEmpresa(u);

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.ReceituariosMedicos
                .AsNoTracking()
                .Where(x => x.CodEmpresa == codEmp);

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                q = q.Where(x => x.ProfissionalId == uid);
            }
            else if (profissionalId.HasValue)
            {
                q = q.Where(x => x.ProfissionalId == profissionalId.Value);
            }

            if (pacienteId.HasValue)
                q = q.Where(x => x.PacienteId == pacienteId.Value);

            if (cancelado.HasValue)
                q = q.Where(x => x.Cancelado == cancelado.Value);

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(x => x.DataEmissao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ReceituarioMedicoListItemDto(
                    x.Id,
                    x.PacienteId,
                    x.ProfissionalId,
                    x.AtendimentoId,
                    x.DataEmissao,
                    x.Observacoes,
                    x.Cancelado,
                    x.CriadoEm
                ))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var data = await db.ReceituariosMedicos
                .AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Select(x => new ReceituarioMedicoDetailsDto(
                    x.Id,
                    x.CodEmpresa,
                    x.PacienteId,
                    x.ProfissionalId,
                    x.AtendimentoId,
                    x.DataEmissao,
                    x.Observacoes,
                    x.Cancelado,
                    x.MotivoCancelamento,
                    x.CriadoEm,
                    x.AtualizadoEm,
                    x.Itens
                        .Select(i => new ReceituarioMedicoItemDto(
                            i.Id,
                            i.NomeMedicamento,
                            i.FormaFarmaceutica,
                            i.Concentracao,
                            i.ViaAdministracao,
                            i.Posologia,
                            i.Orientacoes
                        ))
                        .ToList()
                ))
                .FirstOrDefaultAsync();

            if (data is null)
                return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (data.ProfissionalId != uid)
                    return Results.Forbid();
            }

            return Results.Ok(data);
        });

        g.MapPost("/", async (
            ClaimsPrincipal u,
            ReceituarioMedicoCreateDto dto,
            IValidator<ReceituarioMedicoCreateDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (dto.ProfissionalId != uid)
                    return Results.Forbid();
            }

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var pacienteOk = await db.Pacientes.AnyAsync(x =>
                x.Id == dto.PacienteId &&
                x.CodEmpresa == codEmp &&
                x.Ativo);

            if (!pacienteOk)
                return Results.NotFound(new { message = "paciente_nao_encontrado" });

            var profissionalOk = await db.Usuarios.AnyAsync(x =>
                x.Id == dto.ProfissionalId &&
                x.CodEmpresa == codEmp &&
                x.Ativo);

            if (!profissionalOk)
                return Results.NotFound(new { message = "profissional_nao_encontrado" });

            if (dto.AtendimentoId.HasValue)
            {
                var atendimentoOk = await db.Atendimentos.AnyAsync(x =>
                    x.Id == dto.AtendimentoId.Value &&
                    x.CodEmpresa == codEmp);

                if (!atendimentoOk)
                    return Results.NotFound(new { message = "atendimento_nao_encontrado" });
            }

            var entity = new ReceituarioMedico
            {
                CodEmpresa = codEmp,
                PacienteId = dto.PacienteId,
                ProfissionalId = dto.ProfissionalId,
                AtendimentoId = dto.AtendimentoId,
                DataEmissao = dto.DataEmissao,
                Observacoes = dto.Observacoes,
                Cancelado = false,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow,
                Itens = dto.Itens.Select(i => new ReceituarioMedicoItem
                {
                    NomeMedicamento = i.NomeMedicamento,
                    FormaFarmaceutica = i.FormaFarmaceutica,
                    Concentracao = i.Concentracao,
                    ViaAdministracao = i.ViaAdministracao,
                    Posologia = i.Posologia,
                    Orientacoes = i.Orientacoes
                }).ToList()
            };

            db.ReceituariosMedicos.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/receituarios-medicos/{entity.Id}", new { entity.Id });
        });

        g.MapPost("/{id:guid}/cancelar", async (
            ClaimsPrincipal u,
            Guid id,
            ReceituarioMedicoCancelDto dto,
            IValidator<ReceituarioMedicoCancelDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var entity = await db.ReceituariosMedicos
                .FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);

            if (entity is null)
                return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (entity.ProfissionalId != uid)
                    return Results.Forbid();
            }

            if (entity.Cancelado)
                return Results.Conflict(new { message = "receituario_ja_cancelado" });

            entity.Cancelado = true;
            entity.MotivoCancelamento = dto.MotivoCancelamento;
            entity.AtualizadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapGet("/{id:guid}/imprimir", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var data = await db.ReceituariosMedicos
                .AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Join(db.Pacientes.AsNoTracking(),
                    r => r.PacienteId,
                    p => p.Id,
                    (r, p) => new
                    {
                        Receituario = r,
                        PacienteNome = p.Nome
                    })
                .Join(db.Usuarios.AsNoTracking(),
                    rp => rp.Receituario.ProfissionalId,
                    prof => prof.Id,
                    (rp, prof) => new
                    {
                        rp.Receituario,
                        rp.PacienteNome,
                        ProfissionalNome = prof.Nome
                    })
                .FirstOrDefaultAsync();

            if (data is null)
                return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (data.Receituario.ProfissionalId != uid)
                    return Results.Forbid();
            }

            var itensHtml = string.Join("", data.Receituario.Itens.Select((item, index) => $"""
                <div class="item">
                  <p><strong>{index + 1}. {System.Net.WebUtility.HtmlEncode(item.NomeMedicamento)}</strong></p>
                  {(string.IsNullOrWhiteSpace(item.FormaFarmaceutica) ? "" : $"<p><strong>Forma farmacêutica:</strong> {System.Net.WebUtility.HtmlEncode(item.FormaFarmaceutica)}</p>")}
                  {(string.IsNullOrWhiteSpace(item.Concentracao) ? "" : $"<p><strong>Concentração:</strong> {System.Net.WebUtility.HtmlEncode(item.Concentracao)}</p>")}
                  {(string.IsNullOrWhiteSpace(item.ViaAdministracao) ? "" : $"<p><strong>Via de administração:</strong> {System.Net.WebUtility.HtmlEncode(item.ViaAdministracao)}</p>")}
                  {(string.IsNullOrWhiteSpace(item.Posologia) ? "" : $"<p><strong>Posologia:</strong> {System.Net.WebUtility.HtmlEncode(item.Posologia)}</p>")}
                  {(string.IsNullOrWhiteSpace(item.Orientacoes) ? "" : $"<p><strong>Orientações:</strong> {System.Net.WebUtility.HtmlEncode(item.Orientacoes)}</p>")}
                </div>
                """));

            var canceladoHtml = data.Receituario.Cancelado
                ? $"<p style=\"color:red;\"><strong>DOCUMENTO CANCELADO</strong><br/>Motivo: {System.Net.WebUtility.HtmlEncode(data.Receituario.MotivoCancelamento ?? "-")}</p>"
                : "";

            var observacoesHtml = string.IsNullOrWhiteSpace(data.Receituario.Observacoes)
                ? ""
                : $"<p><strong>Observações gerais:</strong> {System.Net.WebUtility.HtmlEncode(data.Receituario.Observacoes)}</p>";

            var html = $$"""
            <!doctype html>
            <html lang="pt-br">
            <head>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width,initial-scale=1"/>
              <title>Receituário Médico</title>
              <style>
                body {
                  font-family: Arial, sans-serif;
                  max-width: 900px;
                  margin: 40px auto;
                  padding: 24px;
                  color: #111827;
                }
                h1 {
                  text-align: center;
                  margin-bottom: 32px;
                }
                p {
                  line-height: 1.5;
                  font-size: 15px;
                  margin: 6px 0;
                }
                .item {
                  border: 1px solid #e5e7eb;
                  border-radius: 8px;
                  padding: 12px;
                  margin-bottom: 16px;
                }
                .assinatura {
                  margin-top: 48px;
                }
              </style>
            </head>
            <body>
              <h1>RECEITUÁRIO MÉDICO</h1>

              {{canceladoHtml}}

              <p><strong>Paciente:</strong> {{System.Net.WebUtility.HtmlEncode(data.PacienteNome)}}</p>
              <p><strong>Profissional:</strong> {{System.Net.WebUtility.HtmlEncode(data.ProfissionalNome)}}</p>
              <p><strong>Data de emissão:</strong> {{data.Receituario.DataEmissao:dd/MM/yyyy}}</p>

              <hr style="margin: 24px 0;" />

              {{itensHtml}}

              {{observacoesHtml}}

              <div class="assinatura">
                <p>________________________________________</p>
                <p><strong>{{System.Net.WebUtility.HtmlEncode(data.ProfissionalNome)}}</strong></p>
              </div>
            </body>
            </html>
            """;

            return Html(html);
        });
    }
}
