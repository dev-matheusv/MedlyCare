using System.Net;
using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Agendamentos;
using SFA.Domain.Entities;
using SFA.Infrastructure;

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

        static Guid? GetUserId(ClaimsPrincipal user)
        {
          var s = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
          return Guid.TryParse(s, out var id) ? id : null;   // antes: int
        }

        static bool IsProfissionalOnly(ClaimsPrincipal u)
            => u.Claims.Where(c => c.Type == ClaimTypes.Role).All(c => c.Value is "Profissional");

        // Função para checar overlap (conflito de horário)
        static Task<bool> ExisteConflitoAsync(SfaDbContext db, int codEmp, Guid profissionalId, DateTimeOffset inicio, DateTimeOffset fim, Guid? ignorarId = null)
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
        
        static async Task CancelarAtendimentoRelacionadoAsync(SfaDbContext db, int codEmp, Guid agendamentoId)
        {
          var atendimento = await db.Atendimentos
            .FirstOrDefaultAsync(a => a.CodEmpresa == codEmp && a.AgendamentoId == agendamentoId);

          if (atendimento is not null && atendimento.Status != AtendimentoStatus.Finalizado)
          {
            atendimento.Status = AtendimentoStatus.Cancelado;
          }
        }

        // GET /agendamentos?profissionalId=&pacienteId=&de=&ate=&status=&page=&pageSize=&order=
        g.MapGet("/", async (ClaimsPrincipal u, Guid? profissionalId, Guid? pacienteId, DateTimeOffset? de,
          DateTimeOffset? ate, string? status, SfaDbContext db, int page = 1, int pageSize = 20, string? order = null) =>
        {
            var codEmp = GetCodEmpresa(u);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.Agendamentos.AsNoTracking().Where(a => a.CodEmpresa == codEmp);

            // Profissional só enxerga os próprios
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
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
                .Select(x => new AgendamentoListItemDto(
                    x.a.Id,
                    new PessoaDto(x.a.PacienteId, x.PacienteNome),
                    new PessoaDto(x.a.ProfissionalId, x.ProfNome),
                    x.a.InicioUtc,
                    x.a.FimUtc,
                    x.a.Status,
                    x.a.Observacoes))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // GET /agendamentos/{id}
        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var a = await db.Agendamentos.AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Join(db.Pacientes.AsNoTracking(),
                      x => x.PacienteId, p => p.Id,
                      (x, p) => new { x, PacienteNome = p.Nome })
                .Join(db.Usuarios.AsNoTracking(),
                      xp => xp.x.ProfissionalId, u2 => u2.Id,
                      (xp, prof) => new AgendamentoListItemDto(
                          xp.x.Id,
                          new PessoaDto(xp.x.PacienteId, xp.PacienteNome),
                          new PessoaDto(xp.x.ProfissionalId, prof.Nome),
                          xp.x.InicioUtc,
                          xp.x.FimUtc,
                          xp.x.Status,
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

        // POST /agendamentos
        g.MapPost("/", async (ClaimsPrincipal u, AgendamentoCreateDto dto, IValidator<AgendamentoCreateDto> v, SfaDbContext db) =>
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

            // Overlap
            if (await ExisteConflitoAsync(db, codEmp, dto.ProfissionalId, dto.InicioUtc, dto.FimUtc))
                return Results.Conflict(new { message = "conflito_horario_profissional" });

            var userId = GetUserId(u) ?? Guid.Empty;

            var entity = new Agendamento
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
        g.MapPut("/{id:guid}", async (ClaimsPrincipal u, Guid id, AgendamentoUpdateDto dto, IValidator<AgendamentoUpdateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var entity = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            // Profissional só pode alterar se for dele
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
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
        g.MapPost("/{id:guid}/confirmar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var entity = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            entity.Status = "confirmado";
            entity.AlteradoPorUsuarioId = GetUserId(u);
            entity.AlteradoEm = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /agendamentos/{id}/cancelar
        g.MapPost("/{id:guid}/cancelar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var entity = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }

            entity.Status = "cancelado";
            entity.AlteradoPorUsuarioId = GetUserId(u);
            entity.AlteradoEm = DateTime.UtcNow;

            await CancelarAtendimentoRelacionadoAsync(db, codEmp, entity.Id);

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE /agendamentos/{id}
        g.MapDelete("/{id:guid}", async (ClaimsPrincipal u, Guid id, string? motivo, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            if (string.IsNullOrWhiteSpace(motivo)) return Results.BadRequest(new { message = "motivo_obrigatorio" });

            var entity = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                if (entity.ProfissionalId != uid) return Results.Forbid();
            }
            
            entity.Status = "cancelado";
            entity.AlteradoPorUsuarioId = GetUserId(u);
            entity.AlteradoEm = DateTime.UtcNow;

            entity.IsDeleted = true;
            entity.DeletedAt = DateTimeOffset.UtcNow;
            entity.DeletedBy = GetUserId(u);
            entity.DeletedReason = motivo;

            await CancelarAtendimentoRelacionadoAsync(db, codEmp, entity.Id);

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        
        g.MapGet("/events", async (ClaimsPrincipal u,
                           DateTimeOffset de,
                           DateTimeOffset ate,
                           Guid? profissionalId,
                           Guid? pacienteId,
                           string? status,
                           SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var q = db.Agendamentos.AsNoTracking()
                .Where(a => a.CodEmpresa == codEmp &&
                            a.FimUtc > de && a.InicioUtc < ate);

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
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(a => a.Status == status);

            // projetar com nomes (join leve)
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
                          ap.a.FimUtc,
                          ap.a.Status,
                          ap.a.PacienteId,
                          ap.a.ProfissionalId,
                          ap.a.Observacoes,
                          ap.PacienteNome,
                          ProfNome = prof.Nome
                      })
                .OrderBy(x => x.InicioUtc)
                .Select(x => new
                {
                  id = x.Id,
                  title = $"{x.PacienteNome} • {x.ProfNome}",
                  start = x.InicioUtc,
                  end   = x.FimUtc,
                  status = x.Status,
                  color = x.Status == "confirmado" ? "#22c55e"
                    : x.Status == "cancelado"  ? "#ef4444"
                    : "#3b82f6",
                  pacienteId = x.PacienteId,
                  profissionalId = x.ProfissionalId,
                  observacoes = x.Observacoes
                })
                .ToListAsync();

            return Results.Ok(events);
        });
        
        // GET /agendamentos/slots
        g.MapGet("/slots", async (ClaimsPrincipal u,
          Guid? profissionalId,
          DateOnly data,
          SfaDbContext db,
          int duracaoMin = 30,
          string inicioDia = "08:00",
          string fimDia = "18:00",
          int passoMin = 15) =>
        {
            var codEmp = GetCodEmpresa(u);

            // Se for Profissional sem profissionalId informado, usa o próprio userId
            if (IsProfissionalOnly(u))
            {
                var uid = GetUserId(u) ?? Guid.Empty;
                profissionalId ??= uid;
                if (profissionalId.Value != uid) return Results.Forbid();
            }
            else
            {
                if (profissionalId is null)
                    return Results.BadRequest(new { message = "profissional_id_obrigatorio" });
            }

            // Valida profissional ativo na empresa
            var profissionalOk = await db.Usuarios
                .AsNoTracking()
                .AnyAsync(p => p.Id == profissionalId && p.CodEmpresa == codEmp && p.Ativo);
            if (!profissionalOk) return Results.NotFound(new { message = "profissional_nao_encontrado" });

            // Intervalo do dia (UTC)
            static bool TryParseHm(string hm, out int h, out int m)
            {
                h = m = 0;
                var parts = hm.Split(':', 2);
                return parts.Length == 2
                       && int.TryParse(parts[0], out h) && int.TryParse(parts[1], out m)
                       && h >= 0 && h <= 23 && m >= 0 && m <= 59;
            }

            if (!TryParseHm(inicioDia, out var hIni, out var mIni) ||
                !TryParseHm(fimDia, out var hFim, out var mFim))
                return Results.BadRequest(new { message = "horario_invalido_use_HH:mm" });

            var dayStartUtc = new DateTimeOffset(data.Year, data.Month, data.Day, hIni, mIni, 0, TimeSpan.Zero);
            var dayEndUtc   = new DateTimeOffset(data.Year, data.Month, data.Day, hFim, mFim, 0, TimeSpan.Zero);

            if (dayEndUtc <= dayStartUtc)
                return Results.BadRequest(new { message = "fim_menor_ou_igual_ao_inicio" });

            var dur = TimeSpan.FromMinutes(Math.Max(1, duracaoMin));
            var step = TimeSpan.FromMinutes(Math.Clamp(passoMin, 1, 120));

            // Busca agendamentos do dia para o profissional (exceto cancelados)
            var agends = await db.Agendamentos.AsNoTracking()
                .Where(a => a.CodEmpresa == codEmp
                            && a.ProfissionalId == profissionalId
                            && a.Status != "cancelado"
                            && a.FimUtc > dayStartUtc
                            && a.InicioUtc < dayEndUtc)
                .Select(a => new { a.InicioUtc, a.FimUtc })
                .OrderBy(a => a.InicioUtc)
                .ToListAsync();

            // Normaliza e mescla intervalos ocupados dentro da janela do dia
            var ocupados = new List<(DateTimeOffset start, DateTimeOffset end)>();
            foreach (var a in agends)
            {
                var s = a.InicioUtc < dayStartUtc ? dayStartUtc : a.InicioUtc;
                var e = a.FimUtc   > dayEndUtc   ? dayEndUtc   : a.FimUtc;
                if (e <= s) continue;

                if (ocupados.Count == 0 || s > ocupados[^1].end)
                {
                    ocupados.Add((s, e));
                }
                else
                {
                    // mescla com o último
                    var last = ocupados[^1];
                    ocupados[^1] = (last.start, e > last.end ? e : last.end);
                }
            }

            // Gera slots livres “descontando” os ocupados
            var livres = new List<object>();
            var cursor = dayStartUtc;

            void EmitirBlocoLivre(DateTimeOffset from, DateTimeOffset to)
            {
                // Gera slots de tamanho "dur" pulando de "step"
                for (var start = from; start + dur <= to; start += step)
                {
                    var end = start + dur;
                    livres.Add(new
                    {
                        start,
                        end,
                        minutes = (int)dur.TotalMinutes
                    });
                }
            }

            foreach (var (start, end) in ocupados)
            {
                if (cursor < start)
                    EmitirBlocoLivre(cursor, start);

                if (end > cursor) cursor = end;
            }

            if (cursor < dayEndUtc)
                EmitirBlocoLivre(cursor, dayEndUtc);

            return Results.Ok(new
            {
                profissionalId = profissionalId.Value,
                data,
                inicioDia = dayStartUtc,
                fimDia = dayEndUtc,
                duracaoMin = (int)dur.TotalMinutes,
                passoMin = (int)step.TotalMinutes,
                slots = livres
            });
        });
        
        // PUBLIC: confirmação via link (sem autenticação)
        var pub = app.MapGroup("/api/v1/public/confirmacoes");

        static IResult Html(string title, string msg)
        {
          var html = """
           <!doctype html>
           <html lang="pt-br">
           <head>
             <meta charset="utf-8"/>
             <meta name="viewport" content="width=device-width,initial-scale=1"/>
             <title>{{TITLE}}</title>
             <style>
               body { font-family: Arial, sans-serif; padding: 24px; max-width: 720px; margin: 0 auto; }
               .box { border: 1px solid #e5e7eb; border-radius: 10px; padding: 18px; }
               h1 { font-size: 20px; margin: 0 0 8px 0; }
               p { margin: 0; line-height: 1.4; }
             </style>
           </head>
           <body>
             <div class="box">
               <h1>{{TITLE}}</h1>
               <p>{{MSG}}</p>
             </div>
           </body>
           </html>
           """;

          html = html.Replace("{{TITLE}}", WebUtility.HtmlEncode(title))
            .Replace("{{MSG}}", WebUtility.HtmlEncode(msg));

          return Results.Content(html, "text/html; charset=utf-8");
        }
        
        pub.MapGet("/{token:guid}/sim", async (Guid token, HttpContext ctx, SfaDbContext db) =>
        {
          var c = await db.ConfirmacoesAgendamento
            .SingleOrDefaultAsync(x => x.Token == token);

          if (c is null) return Html("Link inválido", "Esse link não é válido. Entre em contato com a recepção.");

          var now = DateTimeOffset.UtcNow;
          if (c.ExpiresAtUtc <= now) return Html("Link expirado", "Esse link expirou. Entre em contato com a recepção.");

          if (c.Status != ConfirmacaoAgendamentoStatus.Pendente)
            return Html("Já registrado", "Sua resposta já foi registrada. Obrigado!");

          // carrega agendamento (ignora filtro IsDeleted)
          var ag = await db.Agendamentos
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(a => a.Id == c.AgendamentoId && a.CodEmpresa == c.CodEmpresa);

          if (ag is null || ag.IsDeleted)
            return Html("Agendamento não encontrado", "Esse agendamento não existe mais. Entre em contato com a recepção.");

          c.Status = ConfirmacaoAgendamentoStatus.Confirmado;
          c.RespondidoEmUtc = now;
          c.RespondidoIp = ctx.Connection.RemoteIpAddress?.ToString();
          c.UserAgent = ctx.Request.Headers.UserAgent.ToString();

          // atualiza agendamento (se não estiver cancelado)
          if (ag.Status != "cancelado")
          {
            ag.Status = "confirmado";
            ag.AlteradoEm = DateTime.UtcNow;
            ag.AlteradoPorUsuarioId = null; // público (sistema)
          }

          await db.SaveChangesAsync();
          return Html("Confirmado", "Presença confirmada com sucesso. Obrigado!");
        });

        pub.MapGet("/{token:guid}/nao", async (Guid token, HttpContext ctx, SfaDbContext db) =>
        {
          var c = await db.ConfirmacoesAgendamento
            .SingleOrDefaultAsync(x => x.Token == token);

          if (c is null) return Html("Link inválido", "Esse link não é válido. Entre em contato com a recepção.");

          var now = DateTimeOffset.UtcNow;
          if (c.ExpiresAtUtc <= now) return Html("Link expirado", "Esse link expirou. Entre em contato com a recepção.");

          if (c.Status != ConfirmacaoAgendamentoStatus.Pendente)
            return Html("Já registrado", "Sua resposta já foi registrada. Obrigado!");

          // carrega agendamento (ignora filtro IsDeleted)
          var ag = await db.Agendamentos
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(a => a.Id == c.AgendamentoId && a.CodEmpresa == c.CodEmpresa);

          if (ag is null || ag.IsDeleted)
            return Html("Agendamento não encontrado", "Esse agendamento não existe mais. Entre em contato com a recepção.");

          c.Status = ConfirmacaoAgendamentoStatus.Recusado;
          c.RespondidoEmUtc = now;
          c.RespondidoIp = ctx.Connection.RemoteIpAddress?.ToString();
          c.UserAgent = ctx.Request.Headers.UserAgent.ToString();

          // cancela agendamento
          ag.Status = "cancelado";
          ag.AlteradoEm = DateTime.UtcNow;
          ag.AlteradoPorUsuarioId = null;

          await CancelarAtendimentoRelacionadoAsync(db, ag.CodEmpresa, ag.Id);

          await db.SaveChangesAsync();
          return Html("Ok", "Registramos que você não poderá comparecer. Obrigado!");
        });

        static string DigitsOnly(string? s)
        {
          if (string.IsNullOrWhiteSpace(s)) return "";
          var chars = s.Where(char.IsDigit).ToArray();
          return new string(chars);
        }

        static string NormalizeWhatsappPhone(string? telefone)
        {
          var d = DigitsOnly(telefone);
          if (string.IsNullOrWhiteSpace(d)) return "";

          // Se já vier com 55, mantém
          if (d.StartsWith("55")) return d;

          // Se vier 10/11 dígitos, assume BR e prefixa 55
          if (d.Length is 10 or 11) return "55" + d;

          return d; // fallback
        }

        g.MapPost("/{id:guid}/confirmacao/gerar", async (ClaimsPrincipal u, Guid id, HttpContext ctx, SfaDbContext db, int dias = 3) =>
        {
          var codEmp = GetCodEmpresa(u);

          // 1 query só: traz tudo que precisa pra validar e montar msg
          var data = await db.Agendamentos.AsNoTracking()
            .Where(a => a.Id == id && a.CodEmpresa == codEmp)
            .Join(db.Pacientes.AsNoTracking(), a => a.PacienteId, p => p.Id, (a, p) => new
            {
              Ag = a,
              PacienteNome = p.Nome,
              p.Telefone
            })
            .Join(db.Usuarios.AsNoTracking(), ap => ap.Ag.ProfissionalId, prof => prof.Id, (ap, prof) => new
            {
              ap.Ag.Id,
              ap.Ag.CodEmpresa,
              ap.Ag.Status,
              ap.Ag.InicioUtc,
              ap.Ag.FimUtc,
              ap.PacienteNome,
              ap.Telefone,
              ProfNome = prof.Nome
            })
            .FirstOrDefaultAsync();

          if (data is null) return Results.NotFound(new { message = "agendamento_nao_encontrado" });
          if (data.Status == "cancelado") return Results.Conflict(new { message = "agendamento_cancelado" });

          // expiração
          var now = DateTimeOffset.UtcNow;
          var maxExp = now.AddDays(Math.Clamp(dias, 1, 7));
          var expires = data.FimUtc <= now ? now.AddHours(2) : data.FimUtc;
          if (expires > maxExp) expires = maxExp;

          // cria/atualiza confirmação (track)
          var c = await db.ConfirmacoesAgendamento
            .FirstOrDefaultAsync(x => x.AgendamentoId == id);

          if (c is null)
          {
            c = new ConfirmacaoAgendamento
            {
              CodEmpresa = codEmp,
              AgendamentoId = id,
              Token = Guid.NewGuid(),
              Status = ConfirmacaoAgendamentoStatus.Pendente,
              ExpiresAtUtc = expires
            };
            db.ConfirmacoesAgendamento.Add(c);
          }
          else
          {
            // se já respondeu ou expirou, regenera token e volta pra pendente
            if (c.Status != ConfirmacaoAgendamentoStatus.Pendente || c.ExpiresAtUtc <= now)
            {
              c.Token = Guid.NewGuid();
              c.Status = ConfirmacaoAgendamentoStatus.Pendente;
              c.RespondidoEmUtc = null;
              c.RespondidoIp = null;
              c.UserAgent = null;
            }

            c.ExpiresAtUtc = expires;
          }

          await db.SaveChangesAsync();

          var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
          var linkSim = $"{baseUrl}/api/v1/public/confirmacoes/{c.Token}/sim";
          var linkNao = $"{baseUrl}/api/v1/public/confirmacoes/{c.Token}/nao";

          var msg =
          $"Olá, {data.PacienteNome}! Tudo bem?\n\n" +
          $"Estamos confirmando sua consulta com {data.ProfNome} em " +
          $"{data.InicioUtc:dd/MM/yyyy} às {data.InicioUtc:HH:mm}.\n\n" +
          $"👉 Confirmar presença:\n{linkSim}\n\n" +
          $"❌ Não poderei comparecer:\n{linkNao}";

          var phone = NormalizeWhatsappPhone(data.Telefone);
          var whatsappUrl = string.IsNullOrWhiteSpace(phone)
            ? null
            : $"https://wa.me/{phone}?text={Uri.EscapeDataString(msg)}";

          return Results.Ok(new
          {
            agendamentoId = id,
            token = c.Token,
            expiresAtUtc = c.ExpiresAtUtc,
            linkSim,
            linkNao,
            mensagem = msg,
            whatsappUrl
          });
        });
    }
}
