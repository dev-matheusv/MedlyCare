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
                .Join(db.Pacientes.AsNoTracking(),
                    x => x.PacienteId,
                    p => p.Id,
                    (x, p) => new { Atestado = x, NomePaciente = p.Nome })
                .Join(db.Usuarios.AsNoTracking(),
                    x => x.Atestado.ProfissionalId,
                    us => us.Id,
                    (x, us) => new AtestadoListItemDto(
                        x.Atestado.Id,
                        x.Atestado.PacienteId,
                        x.NomePaciente,
                        x.Atestado.ProfissionalId,
                        us.Nome,
                        x.Atestado.AtendimentoId,
                        x.Atestado.DataEmissao,
                        x.Atestado.DiasAfastamento,
                        x.Atestado.HoraInicio,
                        x.Atestado.HoraFim,
                        x.Atestado.DataInicioAfastamento,
                        x.Atestado.TipoAfastamento.HasValue ? (int)x.Atestado.TipoAfastamento.Value : null,
                        x.Atestado.DescricaoCurta,
                        x.Atestado.InformarCid,
                        x.Atestado.Cid,
                        x.Atestado.LocalEmissao,
                        x.Atestado.Crm,
                        x.Atestado.AssinaturaNome,
                        x.Atestado.Cancelado,
                        x.Atestado.CriadoEm
                    ))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var raw = await db.Atestados
                .AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Join(db.Pacientes.AsNoTracking(),
                    x => x.PacienteId,
                    p => p.Id,
                    (x, p) => new { Atestado = x, NomePaciente = p.Nome })
                .Join(db.Usuarios.AsNoTracking(),
                    x => x.Atestado.ProfissionalId,
                    us => us.Id,
                    (x, us) => new { x.Atestado, x.NomePaciente, NomeProfissional = us.Nome })
                .FirstOrDefaultAsync();

            if (raw is null)
                return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (raw.Atestado.ProfissionalId != uid)
                    return Results.Forbid();
            }

            var item = new AtestadoDetailsDto(
                raw.Atestado.Id,
                raw.Atestado.CodEmpresa,
                raw.Atestado.PacienteId,
                raw.NomePaciente,
                raw.Atestado.ProfissionalId,
                raw.NomeProfissional,
                raw.Atestado.AtendimentoId,
                raw.Atestado.DataEmissao,
                raw.Atestado.DiasAfastamento,
                raw.Atestado.HoraInicio,
                raw.Atestado.HoraFim,
                raw.Atestado.DataInicioAfastamento,
                raw.Atestado.TipoAfastamento.HasValue ? (int)raw.Atestado.TipoAfastamento.Value : null,
                raw.Atestado.DescricaoCurta,
                raw.Atestado.InformarCid,
                raw.Atestado.Cid,
                raw.Atestado.LocalEmissao,
                raw.Atestado.Crm,
                raw.Atestado.AssinaturaNome,
                raw.Atestado.Cancelado,
                raw.Atestado.MotivoCancelamento,
                raw.Atestado.CriadoEm,
                raw.Atestado.AtualizadoEm
            );

            return Results.Ok(item);
        });

        g.MapPost("/", async (
            ClaimsPrincipal u,
            AtestadoCreateDto dto,
            IValidator<AtestadoCreateDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var userId = GetUserId(u) ?? Guid.Empty;

            if (IsProfissionalOnly(u))
            {
                if (dto.ProfissionalId != userId)
                    return Results.Forbid();
            }

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var paciente = await db.Pacientes.FirstOrDefaultAsync(x =>
                x.Id == dto.PacienteId &&
                x.CodEmpresa == codEmp &&
                x.Ativo);

            if (paciente is null)
                return Results.NotFound(new { message = "paciente_nao_encontrado" });

            // Busca o profissional para obter o CRM automaticamente
            var profissional = await db.Usuarios.FirstOrDefaultAsync(x =>
                x.Id == dto.ProfissionalId &&
                x.CodEmpresa == codEmp &&
                x.Ativo);

            if (profissional is null)
                return Results.NotFound(new { message = "profissional_nao_encontrado" });

            if (dto.AtendimentoId.HasValue)
            {
                var atendimentoOk = await db.Atendimentos.AnyAsync(x =>
                    x.Id == dto.AtendimentoId.Value &&
                    x.CodEmpresa == codEmp);

                if (!atendimentoOk)
                    return Results.NotFound(new { message = "atendimento_nao_encontrado" });
            }

            // LocalEmissao automático: Cidade/UF da empresa
            var empresa = await db.Empresas
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.CodEmpresa == codEmp);

            var localEmissao = empresa is not null
                ? $"{empresa.Cidade}/{empresa.Uf}"
                : null;

            var entity = new Atestado
            {
                CodEmpresa = codEmp,
                PacienteId = dto.PacienteId,
                ProfissionalId = dto.ProfissionalId,
                AtendimentoId = dto.AtendimentoId,
                DataEmissao = DateTime.UtcNow,         // automático
                DiasAfastamento = dto.DiasAfastamento,
                HoraInicio = dto.HoraInicio,
                HoraFim = dto.HoraFim,
                DataInicioAfastamento = dto.DataInicioAfastamento,
                TipoAfastamento = dto.TipoAfastamento.HasValue
                    ? (TipoAfastamento)dto.TipoAfastamento.Value
                    : null,
                DescricaoCurta = dto.DescricaoCurta,
                InformarCid = dto.InformarCid,
                Cid = dto.InformarCid ? dto.Cid : null,
                LocalEmissao = localEmissao,            // automático da empresa
                Crm = profissional.Crm,                 // automático do usuário
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

            // Afastamento: dias ou período de horas
            string afastamentoTexto;
            if (data.Atestado.DiasAfastamento > 0)
            {
                afastamentoTexto = $"<strong>{data.Atestado.DiasAfastamento}</strong> dia(s), a contar de <strong>{inicio:dd/MM/yyyy}</strong>";
            }
            else if (data.Atestado.HoraInicio.HasValue && data.Atestado.HoraFim.HasValue)
            {
                afastamentoTexto = $"o período de <strong>{data.Atestado.HoraInicio.Value:hh\\:mm}</strong> às <strong>{data.Atestado.HoraFim.Value:hh\\:mm}</strong> do dia <strong>{inicio:dd/MM/yyyy}</strong>";
            }
            else
            {
                afastamentoTexto = $"<strong>{data.Atestado.DiasAfastamento}</strong> dia(s), a contar de <strong>{inicio:dd/MM/yyyy}</strong>";
            }

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
                             {{afastamentoTexto}}.
                           </p>

                           {{(string.IsNullOrWhiteSpace(data.Atestado.DescricaoCurta) ? "" : $"<p>{System.Net.WebUtility.HtmlEncode(data.Atestado.DescricaoCurta)}</p>")}}

                           {{cidHtml}}

                           <p>
                             {{System.Net.WebUtility.HtmlEncode(data.Atestado.LocalEmissao ?? "")}},
                             {{data.Atestado.DataEmissao:dd/MM/yyyy}}
                           </p>

                           <div class="assinatura">
                             <p><strong>{{System.Net.WebUtility.HtmlEncode(data.Atestado.AssinaturaNome)}}</strong></p>
                             {{(string.IsNullOrWhiteSpace(data.Atestado.Crm) ? "" : $"<p>CRM: {System.Net.WebUtility.HtmlEncode(data.Atestado.Crm)}</p>")}}
                           </div>
                         </body>
                         </html>
                         """;

            return Html(html);
        });
    }
}
