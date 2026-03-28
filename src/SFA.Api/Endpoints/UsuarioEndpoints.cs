using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using SFA.Application.Usuarios;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/usuarios")
                   .RequireAuthorization("Admin");

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }
        
        static async Task<bool> EhUnicoAdminAtivoAsync(SfaDbContext db, Guid usuarioId, int codEmp)
        {
          var isAdminAtivo = await db.UsuariosPerfis
            .AnyAsync(up => up.UsuarioId == usuarioId &&
                            up.Usuario.CodEmpresa == codEmp &&
                            up.Usuario.Ativo &&
                            up.Perfil.Nome == "Admin");

          if (!isAdminAtivo)
            return false;

          var totalAdminsAtivos = await db.UsuariosPerfis
            .CountAsync(up =>
              up.Usuario.CodEmpresa == codEmp &&
              up.Usuario.Ativo &&
              up.Perfil.Nome == "Admin");

          return totalAdminsAtivos <= 1;
        }

        static async Task<(string Code, string Message)?> ObterBloqueioExclusaoAsync(SfaDbContext db, Guid usuarioId, int codEmp)
        {
          var possuiAgendamentoEmAberto = await db.Agendamentos
            .IgnoreQueryFilters()
            .AnyAsync(x =>
              x.CodEmpresa == codEmp &&
              x.ProfissionalId == usuarioId &&
              (x.Status == "agendado" || x.Status == "confirmado"));

          if (possuiAgendamentoEmAberto)
          {
            return (
              "usuario_com_agendamentos_em_aberto",
              "Nao e possivel excluir este profissional porque existem agendamentos em aberto vinculados a ele. Cancele ou reatribua os agendamentos antes de excluir."
            );
          }

          var possuiAtendimentoAberto = await db.Atendimentos
            .IgnoreQueryFilters()
            .AnyAsync(x =>
              x.CodEmpresa == codEmp &&
              x.ProfissionalId == usuarioId &&
              x.Status == SFA.Domain.Entities.AtendimentoStatus.Aberto);

          if (possuiAtendimentoAberto)
          {
            return (
              "usuario_com_atendimentos_abertos",
              "Nao e possivel excluir este profissional porque existem atendimentos em aberto vinculados a ele. Finalize ou cancele os atendimentos antes de excluir."
            );
          }

          // Alguns agregados usam soft delete; IgnoreQueryFilters evita falso negativo
          // que acabaria gerando violacao de FK no SaveChanges.
          if (await db.Agendamentos
                .IgnoreQueryFilters()
                .AnyAsync(x => x.CodEmpresa == codEmp && x.ProfissionalId == usuarioId))
            return (
              "usuario_com_historico",
              "Nao e possivel excluir fisicamente um usuario com historico vinculado. Inative o usuario para preservar o historico."
            );

          if (await db.Atendimentos
                .IgnoreQueryFilters()
                .AnyAsync(x => x.CodEmpresa == codEmp && x.ProfissionalId == usuarioId))
            return (
              "usuario_com_historico",
              "Nao e possivel excluir fisicamente um usuario com historico vinculado. Inative o usuario para preservar o historico."
            );

          if (await db.Atestados
                .AnyAsync(x => x.CodEmpresa == codEmp && x.ProfissionalId == usuarioId))
            return (
              "usuario_com_historico",
              "Nao e possivel excluir fisicamente um usuario com historico vinculado. Inative o usuario para preservar o historico."
            );

          if (await db.ReceituariosMedicos
                .AnyAsync(x => x.CodEmpresa == codEmp && x.ProfissionalId == usuarioId))
            return (
              "usuario_com_historico",
              "Nao e possivel excluir fisicamente um usuario com historico vinculado. Inative o usuario para preservar o historico."
            );

          if (await db.AnexosPaciente
                .IgnoreQueryFilters()
                .AnyAsync(x => x.CodEmpresa == codEmp && x.EnviadoPorId == usuarioId))
            return (
              "usuario_com_historico",
              "Nao e possivel excluir fisicamente um usuario com historico vinculado. Inative o usuario para preservar o historico."
            );

          if (await db.LogsAcessoAnexoPaciente
                .AnyAsync(x => x.CodEmpresa == codEmp && x.UsuarioId == usuarioId))
            return (
              "usuario_com_historico",
              "Nao e possivel excluir fisicamente um usuario com historico vinculado. Inative o usuario para preservar o historico."
            );

          return null;
        }

        g.MapGet("/", async (ClaimsPrincipal u, string? search, bool? ativo,
            SfaDbContext db, int page = 1, int pageSize = 20, string? order = null) =>
        {
            var codEmp = GetCodEmpresa(u);

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.Usuarios.AsNoTracking().Where(x => x.CodEmpresa == codEmp);

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(x =>
                    EF.Functions.ILike(x.Login, $"%{search}%") ||
                    EF.Functions.ILike(x.Nome, $"%{search}%") ||
                    EF.Functions.ILike(x.Email, $"%{search}%"));
            }

            if (ativo.HasValue)
                q = q.Where(x => x.Ativo == ativo.Value);

            q = order?.ToLower() switch
            {
                "nome" => q.OrderBy(x => x.Nome),
                "-nome" => q.OrderByDescending(x => x.Nome),
                "criadoem" => q.OrderBy(x => x.CriadoEm),
                "-criadoem" => q.OrderByDescending(x => x.CriadoEm),
                _ => q.OrderBy(x => x.Id)
            };

            var total = await q.CountAsync();

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new UsuarioListItemDto(
                    x.Id,
                    x.CodEmpresa,
                    x.Login,
                    x.Nome,
                    x.Email,
                    x.Telefone,
                    x.CelularWhatsapp,
                    x.Ativo,
                    x.CriadoEm
                ))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var x = await db.Usuarios.AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Select(x => new UsuarioListItemDto(
                    x.Id,
                    x.CodEmpresa,
                    x.Login,
                    x.Nome,
                    x.Email,
                    x.Telefone,
                    x.CelularWhatsapp,
                    x.Ativo,
                    x.CriadoEm
                ))
                .FirstOrDefaultAsync();

            return x is null ? Results.NotFound() : Results.Ok(x);
        });

        g.MapPost("/", async (ClaimsPrincipal u, UsuarioCreateDto dto, IValidator<UsuarioCreateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
            
            var loginExists = await db.Usuarios.AnyAsync(x =>
              x.CodEmpresa == codEmp &&
              EF.Functions.ILike(x.Login, dto.Login));

            if (loginExists)
              return Results.Conflict(new { field = "login", message = "login já existe nesta empresa" });

            var emailExists = await db.Usuarios.AnyAsync(x =>
              x.CodEmpresa == codEmp &&
              EF.Functions.ILike(x.Email, dto.Email));

            if (emailExists)
              return Results.Conflict(new { field = "email", message = "email já existe nesta empresa" });

            await using var cmd = new NpgsqlCommand("SELECT crypt(@p, gen_salt('bf'))", conn);
            cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, dto.Password);
            var hashObj = await cmd.ExecuteScalarAsync();
            var hash = (hashObj as string) ?? throw new InvalidOperationException("Falha ao gerar hash de senha.");

            var entity = new Domain.Entities.Usuario
            {
                CodEmpresa = codEmp,
                Login = dto.Login,
                Nome = dto.Nome,
                Email = dto.Email,
                Telefone = dto.Telefone,
                CelularWhatsapp = dto.CelularWhatsapp,
                PasswordHash = hash,
                Ativo = dto.Ativo
            };

            db.Usuarios.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/usuarios/{entity.Id}", new { entity.Id });
        });

        g.MapPut("/{id:guid}", async (ClaimsPrincipal u, Guid id, UsuarioUpdateDto dto, IValidator<UsuarioUpdateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var entity = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
              var emailExists = await db.Usuarios.AnyAsync(x =>
                x.Id != id &&
                x.CodEmpresa == codEmp &&
                EF.Functions.ILike(x.Email, dto.Email));

              if (emailExists)
                return Results.Conflict(new { field = "email", message = "email já existe nesta empresa" });
            }

            entity.Nome = dto.Nome;
            entity.Email = dto.Email;
            entity.Telefone = dto.Telefone;
            entity.CelularWhatsapp = dto.CelularWhatsapp;
            
            if (!dto.Ativo && entity.Ativo)
            {
              var unicoAdminAtivo = await EhUnicoAdminAtivoAsync(db, entity.Id, codEmp);
              if (unicoAdminAtivo)
                return Results.Conflict(new { code = "usuario_admin_unico_ativo", message = "Nao e possivel inativar o unico administrador ativo da empresa." });
            }
            entity.Ativo = dto.Ativo;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
              await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
              if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

              await using var cmd = new NpgsqlCommand("SELECT crypt(@p, gen_salt('bf'))", conn);
              cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, dto.Password);
              var hashObj = await cmd.ExecuteScalarAsync();

              entity.PasswordHash = (hashObj as string) ?? throw new InvalidOperationException("Falha ao gerar hash de senha.");
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/{id:guid}/ativar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var entity = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            entity.Ativo = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/{id:guid}/inativar", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var entity = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            var unicoAdminAtivo = await EhUnicoAdminAtivoAsync(db, entity.Id, codEmp);
            if (unicoAdminAtivo)
              return Results.Conflict(new { code = "usuario_admin_unico_ativo", message = "Nao e possivel inativar o unico administrador ativo da empresa." });
            
            entity.Ativo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapDelete("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);
            var entity = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();
            
            var unicoAdminAtivo = await EhUnicoAdminAtivoAsync(db, entity.Id, codEmp);
            if (unicoAdminAtivo)
              return Results.Conflict(new { code = "usuario_admin_unico_ativo", message = "Nao e possivel excluir o unico administrador ativo da empresa." });
            
            var bloqueioExclusao = await ObterBloqueioExclusaoAsync(db, entity.Id, codEmp);
            if (bloqueioExclusao is not null)
              return Results.Conflict(new
              {
                code = bloqueioExclusao.Value.Code,
                message = bloqueioExclusao.Value.Message
              });

            db.Usuarios.Remove(entity);

            try
            {
              await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
            {
              return Results.Conflict(new
              {
                code = "usuario_com_registros_vinculados",
                message = "Nao e possivel excluir fisicamente o usuario porque existem registros vinculados."
              });
            }

            return Results.NoContent();
        });
    }
}
