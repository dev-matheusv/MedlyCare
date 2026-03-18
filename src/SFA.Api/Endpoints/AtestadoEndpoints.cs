using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Atestados;
using SFA.Domain.Entities;
using SFA.Domain.Enums;
using SFA.Infrastructure;

namespace SFA.Api.Endpoints;

public static class AtestadoEndpoints
{
    public static void MapAtestadoEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/atestados")
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

            var q = db.Atestados
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
                .Select(x => new AtestadoListItemDto(
                    x.Id,
                    x.PacienteId,
                    x.ProfissionalId,
                    x.AtendimentoId,
                    x.DataEmissao,
                    x.DiasAfastamento,
                    x.DataInicioAfastamento,
                    x.TipoAfastamento.HasValue ? (int)x.TipoAfastamento.Value : null,
                    x.DescricaoCurta,
                    x.InformarCid,
                    x.Cid,
                    x.LocalEmissao,
                    x.Crm,
                    x.AssinaturaNome,
                    x.Cancelado,
                    x.CriadoEm
                ))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var item = await db.Atestados
                .AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Select(x => new AtestadoDetailsDto(
                    x.Id,
                    x.CodEmpresa,
                    x.PacienteId,
                    x.ProfissionalId,
                    x.AtendimentoId,
                    x.DataEmissao,
                    x.DiasAfastamento,
                    x.DataInicioAfastamento,
                    x.TipoAfastamento.HasValue ? (int)x.TipoAfastamento.Value : null,
                    x.DescricaoCurta,
                    x.InformarCid,
                    x.Cid,
                    x.LocalEmissao,
                    x.Crm,
                    x.AssinaturaNome,
                    x.Cancelado,
                    x.MotivoCancelamento,
                    x.CriadoEm,
                    x.AtualizadoEm
                ))
                .FirstOrDefaultAsync();

            if (item is null)
                return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (item.ProfissionalId != uid)
                    return Results.Forbid();
            }

            return Results.Ok(item);
        });

        g.MapPost("/", async (
            ClaimsPrincipal u,
            AtestadoCreateDto dto,
            IValidator<AtestadoCreateDto> validator,
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

            var entity = new Atestado
            {
                CodEmpresa = codEmp,
                PacienteId = dto.PacienteId,
                ProfissionalId = dto.ProfissionalId,
                AtendimentoId = dto.AtendimentoId,
                DataEmissao = dto.DataEmissao,
                DiasAfastamento = dto.DiasAfastamento,
                DataInicioAfastamento = dto.DataInicioAfastamento,
                TipoAfastamento = dto.TipoAfastamento.HasValue
                    ? (TipoAfastamento)dto.TipoAfastamento.Value
                    : null,
                DescricaoCurta = dto.DescricaoCurta,
                InformarCid = dto.InformarCid,
                Cid = dto.InformarCid ? dto.Cid : null,
                LocalEmissao = dto.LocalEmissao,
                Crm = dto.Crm,
                AssinaturaNome = dto.AssinaturaNome,
                Cancelado = false,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };

            db.Atestados.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/atestados/{entity.Id}", new { entity.Id });
        });

        g.MapPost("/{id:guid}/cancelar", async (
            ClaimsPrincipal u,
            Guid id,
            AtestadoCancelDto dto,
            IValidator<AtestadoCancelDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var entity = await db.Atestados
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
                return Results.Conflict(new { message = "atestado_ja_cancelado" });

            entity.Cancelado = true;
            entity.MotivoCancelamento = dto.MotivoCancelamento;
            entity.AtualizadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapGet("/{id:guid}/imprimir", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var data = await db.Atestados
                .AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Join(db.Pacientes.AsNoTracking(),
                    a => a.PacienteId,
                    p => p.Id,
                    (a, p) => new
                    {
                        Atestado = a,
                        PacienteNome = p.Nome
                    })
                .FirstOrDefaultAsync();

            if (data is null)
                return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (data.Atestado.ProfissionalId != uid)
                    return Results.Forbid();
            }

            var inicio = data.Atestado.DataInicioAfastamento ?? data.Atestado.DataEmissao;
            var cidHtml = data.Atestado.InformarCid && !string.IsNullOrWhiteSpace(data.Atestado.Cid)
                ? $"<p><strong>CID:</strong> {System.Net.WebUtility.HtmlEncode(data.Atestado.Cid)}</p>"
                : "";

            var canceladoHtml = data.Atestado.Cancelado
                ? $"<p style=\"color:red;\"><strong>DOCUMENTO CANCELADO</strong><br/>Motivo: {System.Net.WebUtility.HtmlEncode(data.Atestado.MotivoCancelamento ?? "-")}</p>"
                : "";

            var html = $$"""
                         <!doctype html>
                         <html lang="pt-br">
                         <head>
                           <meta charset="utf-8"/>
                           <meta name="viewport" content="width=device-width,initial-scale=1"/>
                           <title>Atestado Médico</title>
                           <style>
                             body {
                               font-family: Arial, sans-serif;
                               max-width: 800px;
                               margin: 40px auto;
                               padding: 24px;
                               color: #111827;
                             }
                             h1 {
                               text-align: center;
                               margin-bottom: 32px;
                             }
                             p {
                               line-height: 1.6;
                               font-size: 16px;
                             }
                             .assinatura {
                               margin-top: 48px;
                             }
                           </style>
                         </head>
                         <body>
                           <h1>ATESTADO MÉDICO</h1>

                           {{canceladoHtml}}

                           <p>
                             Atesto para os devidos fins que o(a) Sr(a).
                             <strong>{{System.Net.WebUtility.HtmlEncode(data.PacienteNome)}}</strong>
                             necessita de afastamento de suas atividades por
                             <strong>{{data.Atestado.DiasAfastamento}}</strong> dias,
                             a contar de <strong>{{inicio:dd/MM/yyyy}}</strong>.
                           </p>

                           {{(string.IsNullOrWhiteSpace(data.Atestado.DescricaoCurta) ? "" : $"<p>{System.Net.WebUtility.HtmlEncode(data.Atestado.DescricaoCurta)}</p>")}}

                           {{cidHtml}}

                           <p>
                             {{System.Net.WebUtility.HtmlEncode(data.Atestado.LocalEmissao ?? "")}},
                             {{data.Atestado.DataEmissao:dd/MM/yyyy}}
                           </p>

                           <div class="assinatura">
                             <p><strong>{{System.Net.WebUtility.HtmlEncode(data.Atestado.AssinaturaNome)}}</strong></p>
                             <p>CRM: {{System.Net.WebUtility.HtmlEncode(data.Atestado.Crm)}}</p>
                           </div>
                         </body>
                         </html>
                         """;

            return Html(html);
        });
    }
}
