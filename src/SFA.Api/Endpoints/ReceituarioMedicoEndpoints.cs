using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Receituarios;
using SFA.Domain.Entities;
using SFA.Domain.Enums;
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

        static string TipoReceituarioLabel(TipoReceituario tipo) => tipo switch
        {
            TipoReceituario.BrancaSimples     => "Receituário Branco Simples",
            TipoReceituario.BrancaEspecial    => "Receituário Branco Especial",
            TipoReceituario.ControleEspecial  => "Receituário de Controle Especial",
            TipoReceituario.ReceitaB          => "Receita B (Azul)",
            TipoReceituario.ReceitaA          => "Receita A (Amarela)",
            _                                 => "Receituário Médico"
        };

        // GET /receituarios-medicos
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
                    (int)x.TipoReceituario,
                    x.DataEmissao,
                    x.AssinaturaNome,
                    x.RegistroProfissional,
                    x.Cancelado,
                    x.CriadoEm
                ))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // GET /receituarios-medicos/{id}
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
                    (int)x.TipoReceituario,
                    x.DataEmissao,
                    x.Diagnostico,
                    x.InformarCid,
                    x.Cid,
                    x.Observacoes,
                    x.AssinaturaNome,
                    x.RegistroProfissional,
                    x.EnderecoProfissional,
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
                            i.Quantidade,
                            i.QuantidadeExtenso,
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

        // POST /receituarios-medicos
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
                TipoReceituario = (TipoReceituario)dto.TipoReceituario,
                DataEmissao = dto.DataEmissao,
                Diagnostico = dto.Diagnostico,
                InformarCid = dto.InformarCid,
                Cid = dto.InformarCid ? dto.Cid : null,
                Observacoes = dto.Observacoes,
                AssinaturaNome = dto.AssinaturaNome,
                RegistroProfissional = dto.RegistroProfissional,
                EnderecoProfissional = dto.EnderecoProfissional,
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
                    Quantidade = i.Quantidade,
                    QuantidadeExtenso = i.QuantidadeExtenso,
                    Orientacoes = i.Orientacoes
                }).ToList()
            };

            db.ReceituariosMedicos.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/receituarios-medicos/{entity.Id}", new { entity.Id });
        });

        // POST /receituarios-medicos/{id}/cancelar
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

        // ─── CRUD de itens (MC-32) ────────────────────────────────────────────

        // POST /receituarios-medicos/{id}/itens
        g.MapPost("/{id:guid}/itens", async (
            ClaimsPrincipal u,
            Guid id,
            ReceituarioMedicoItemUpsertDto dto,
            IValidator<ReceituarioMedicoItemUpsertDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var receituario = await db.ReceituariosMedicos
                .FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);

            if (receituario is null)
                return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (receituario.ProfissionalId != uid)
                    return Results.Forbid();
            }

            if (receituario.Cancelado)
                return Results.Conflict(new { message = "receituario_cancelado_nao_editavel" });

            var item = new ReceituarioMedicoItem
            {
                ReceituarioMedicoId = id,
                NomeMedicamento = dto.NomeMedicamento,
                FormaFarmaceutica = dto.FormaFarmaceutica,
                Concentracao = dto.Concentracao,
                ViaAdministracao = dto.ViaAdministracao,
                Posologia = dto.Posologia,
                Quantidade = dto.Quantidade,
                QuantidadeExtenso = dto.QuantidadeExtenso,
                Orientacoes = dto.Orientacoes
            };

            db.ReceituariosMedicosItens.Add(item);

            receituario.AtualizadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/receituarios-medicos/{id}", new { item.Id });
        });

        // PUT /receituarios-medicos/{id}/itens/{itemId}
        g.MapPut("/{id:guid}/itens/{itemId:guid}", async (
            ClaimsPrincipal u,
            Guid id,
            Guid itemId,
            ReceituarioMedicoItemUpsertDto dto,
            IValidator<ReceituarioMedicoItemUpsertDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var receituario = await db.ReceituariosMedicos
                .Include(x => x.Itens)
                .FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);

            if (receituario is null)
                return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (receituario.ProfissionalId != uid)
                    return Results.Forbid();
            }

            if (receituario.Cancelado)
                return Results.Conflict(new { message = "receituario_cancelado_nao_editavel" });

            var item = receituario.Itens.FirstOrDefault(i => i.Id == itemId);
            if (item is null)
                return Results.NotFound();

            item.NomeMedicamento = dto.NomeMedicamento;
            item.FormaFarmaceutica = dto.FormaFarmaceutica;
            item.Concentracao = dto.Concentracao;
            item.ViaAdministracao = dto.ViaAdministracao;
            item.Posologia = dto.Posologia;
            item.Quantidade = dto.Quantidade;
            item.QuantidadeExtenso = dto.QuantidadeExtenso;
            item.Orientacoes = dto.Orientacoes;

            receituario.AtualizadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE /receituarios-medicos/{id}/itens/{itemId}
        g.MapDelete("/{id:guid}/itens/{itemId:guid}", async (
            ClaimsPrincipal u,
            Guid id,
            Guid itemId,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var receituario = await db.ReceituariosMedicos
                .Include(x => x.Itens)
                .FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);

            if (receituario is null)
                return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (receituario.ProfissionalId != uid)
                    return Results.Forbid();
            }

            if (receituario.Cancelado)
                return Results.Conflict(new { message = "receituario_cancelado_nao_editavel" });

            var item = receituario.Itens.FirstOrDefault(i => i.Id == itemId);
            if (item is null)
                return Results.NotFound();

            if (receituario.Itens.Count <= 1)
                return Results.Conflict(new { message = "receituario_deve_conter_ao_menos_um_item" });

            db.ReceituariosMedicosItens.Remove(item);

            receituario.AtualizadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // GET /receituarios-medicos/{id}/imprimir
        g.MapGet("/{id:guid}/imprimir", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var data = await db.ReceituariosMedicos
                .AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Join(db.Pacientes.AsNoTracking(),
                    r => r.PacienteId,
                    p => p.Id,
                    (r, p) => new { Receituario = r, PacienteNome = p.Nome })
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
                  <p><strong>Quantidade:</strong> {System.Net.WebUtility.HtmlEncode(item.Quantidade)}{(string.IsNullOrWhiteSpace(item.QuantidadeExtenso) ? "" : $" ({System.Net.WebUtility.HtmlEncode(item.QuantidadeExtenso)})")}</p>
                  {(string.IsNullOrWhiteSpace(item.Orientacoes) ? "" : $"<p><strong>Orientações:</strong> {System.Net.WebUtility.HtmlEncode(item.Orientacoes)}</p>")}
                </div>
                """));

            var canceladoHtml = data.Receituario.Cancelado
                ? $"<p style=\"color:red;\"><strong>DOCUMENTO CANCELADO</strong><br/>Motivo: {System.Net.WebUtility.HtmlEncode(data.Receituario.MotivoCancelamento ?? "-")}</p>"
                : "";

            var diagnosticoHtml = data.Receituario.InformarCid && !string.IsNullOrWhiteSpace(data.Receituario.Cid)
                ? $"<p><strong>CID:</strong> {System.Net.WebUtility.HtmlEncode(data.Receituario.Cid)}</p>"
                : "";

            var observacoesHtml = string.IsNullOrWhiteSpace(data.Receituario.Observacoes)
                ? ""
                : $"<p><strong>Observações:</strong> {System.Net.WebUtility.HtmlEncode(data.Receituario.Observacoes)}</p>";

            var tipoLabel = TipoReceituarioLabel(data.Receituario.TipoReceituario);

            var html = $$"""
            <!doctype html>
            <html lang="pt-br">
            <head>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width,initial-scale=1"/>
              <title>{{tipoLabel}}</title>
              <style>
                body {
                  font-family: Arial, sans-serif;
                  max-width: 900px;
                  margin: 40px auto;
                  padding: 24px;
                  color: #111827;
                }
                h1 { text-align: center; margin-bottom: 8px; }
                .subtitulo { text-align: center; font-size: 13px; color: #6b7280; margin-bottom: 32px; }
                p { line-height: 1.5; font-size: 15px; margin: 6px 0; }
                .item { border: 1px solid #e5e7eb; border-radius: 8px; padding: 12px; margin-bottom: 16px; }
                .assinatura { margin-top: 48px; }
                hr { border: none; border-top: 1px solid #e5e7eb; margin: 24px 0; }
              </style>
            </head>
            <body>
              <h1>{{tipoLabel.ToUpper()}}</h1>
              <p class="subtitulo">{{data.Receituario.DataEmissao:dd/MM/yyyy}}</p>

              {{canceladoHtml}}

              <p><strong>Paciente:</strong> {{System.Net.WebUtility.HtmlEncode(data.PacienteNome)}}</p>
              {{diagnosticoHtml}}

              <hr />

              {{itensHtml}}

              {{observacoesHtml}}

              <div class="assinatura">
                <p>________________________________________</p>
                <p><strong>{{System.Net.WebUtility.HtmlEncode(data.Receituario.AssinaturaNome)}}</strong></p>
                <p>{{System.Net.WebUtility.HtmlEncode(data.Receituario.RegistroProfissional)}}</p>
                <p>{{System.Net.WebUtility.HtmlEncode(data.Receituario.EnderecoProfissional)}}</p>
              </div>
            </body>
            </html>
            """;

            return Html(html);
        });
    }
}
