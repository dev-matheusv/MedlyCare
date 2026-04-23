using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Npgsql;
using NpgsqlTypes;
using Serilog;
using SFA.Application.Auth;
using SFA.Domain.Entities;
using SFA.Infrastructure;

namespace SFA.Api.Endpoints;

public static class TokenHasher
{
  public static string Hash(string token)
  {
    using var sha = SHA256.Create();

    var bytes = Encoding.UTF8.GetBytes(token);

    var hash = sha.ComputeHash(bytes);

    return Convert.ToBase64String(hash);
  }
}
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (LoginRequest req, SfaDbContext db, IJwtTokenService jwtSvc) =>
        {
            static IResult ApiError(string code, string message, int statusCode) =>
                Results.Json(new { code, message }, statusCode: statusCode);

            var login = req.Login.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(req.Password))
                return ApiError("invalid_request", "login_senha_obrigatorios", StatusCodes.Status400BadRequest);

            Domain.Entities.Usuario? user;

            if (req.CodEmpresa.HasValue)
            {
                // Comportamento original: empresa informada explicitamente
                if (req.CodEmpresa.Value <= 0)
                    return ApiError("invalid_request", "cod_empresa_invalido", StatusCodes.Status400BadRequest);

                user = await db.Usuarios.FirstOrDefaultAsync(u =>
                    u.CodEmpresa == req.CodEmpresa.Value &&
                    u.Ativo &&
                    EF.Functions.ILike(u.Login, login));

                if (user is null)
                    return ApiError("user_not_found", "Usuário não encontrado.", StatusCodes.Status404NotFound);
            }
            else
            {
                // Auto-descoberta de empresa pelo login
                var matches = await db.Usuarios
                    .Where(u => u.Ativo && EF.Functions.ILike(u.Login, login))
                    .Select(u => new { u.Id, u.CodEmpresa, u.Nome, u.PasswordHash, u.Login })
                    .ToListAsync();

                if (matches.Count == 0)
                    return ApiError("user_not_found", "Usuário não encontrado.", StatusCodes.Status404NotFound);

                if (matches.Count > 1)
                {
                    // Login existe em mais de uma empresa — retorna a lista para o frontend exibir seletor
                    var empresas = await db.Empresas
                        .Where(e => matches.Select(m => m.CodEmpresa).Contains(e.CodEmpresa))
                        .Select(e => new { e.CodEmpresa, e.RazaoSocial })
                        .ToListAsync();

                    return Results.Json(
                        new
                        {
                            code = "multiple_companies",
                            message = "O login pertence a mais de uma empresa. Informe o código da empresa.",
                            empresas
                        },
                        statusCode: StatusCodes.Status409Conflict);
                }

                user = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == matches[0].Id);
            }

            await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT crypt(@p, @h) = @h", conn);
            cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, req.Password);
            cmd.Parameters.AddWithValue("h", NpgsqlDbType.Text, user!.PasswordHash);

            var valid = (bool)(await cmd.ExecuteScalarAsync() ?? false);
            if (!valid)
                return ApiError("invalid_credentials", "Credenciais inválidas.", StatusCodes.Status401Unauthorized);

            var roles = await db.UsuariosPerfis
                .Where(up => up.UsuarioId == user.Id)
                .Select(up => up.Perfil.Nome)
                .ToListAsync();

            var token = jwtSvc.CreateToken(user.Id, user.CodEmpresa, user.Nome, roles);

            return Results.Ok(new
            {
                access_token = token.AccessToken,
                token_type = token.TokenType,
                expires_at_utc = token.ExpiresAtUtc
            });
        })
        .WithSummary("Autentica e retorna um JWT")
        .WithDescription("Login sem empresa: auto-descoberta. Com empresa: busca direta. Se login existir em múltiplas empresas, retorna 409 com a lista.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/recuperar-acesso", async (
            RecuperarAcessoRequest req,
            SfaDbContext db,
            IEmailService emailService,
            IOptions<SmtpOptions> smtpOptions) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.BadRequest(new { message = "email_obrigatorio" });

            var smtp = smtpOptions.Value;

            if (string.IsNullOrWhiteSpace(smtp.Host) ||
                smtp.Port <= 0 ||
                string.IsNullOrWhiteSpace(smtp.User) ||
                string.IsNullOrWhiteSpace(smtp.Password) ||
                string.IsNullOrWhiteSpace(smtp.FromEmail) ||
                string.IsNullOrWhiteSpace(smtp.ResetBaseUrl))
            {
                return Results.Problem(
                    title: "Configuração inválida",
                    detail: "As configurações de e-mail não foram definidas corretamente.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var email = req.Email.Trim();

            // Busca por e-mail; se CodEmpresa informado filtra pela empresa, senão busca em todas
            var query = db.Usuarios
                .Where(u => u.Ativo && EF.Functions.ILike(u.Email, email));

            if (req.CodEmpresa.HasValue && req.CodEmpresa.Value > 0)
                query = query.Where(u => u.CodEmpresa == req.CodEmpresa.Value);

            var user = await query.FirstOrDefaultAsync();

            if (user is null)
              return Results.Ok(new
              {
                message = "Se existir um usuário compatível, um e-mail será enviado."
              });
            
            var agora = DateTime.UtcNow;

            var tokensAtivos = await db.PasswordResetTokens
              .Where(x => x.UsuarioId == user.Id && x.UsadoEm == null && x.ExpiraEm > agora)
              .ToListAsync();

            foreach (var item in tokensAtivos)
              item.UsadoEm = agora;

            var token = Guid.NewGuid().ToString("N");
            var tokenHash = TokenHasher.Hash(token);

            db.PasswordResetTokens.Add(new PasswordResetToken
            {
              UsuarioId = user.Id,
              TokenHash = tokenHash,
              ExpiraEm = agora.AddHours(1)
            });

            await db.SaveChangesAsync();

            var link = $"{smtp.ResetBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(token)}";

            var html = $"""
                        <p>Olá, {user.Nome}.</p>
                        <p>Recebemos uma solicitação de recuperação de senha.</p>
                        <p>Clique no link abaixo para redefinir:</p>
                        <p><a href="{link}">{link}</a></p>
                        <p>Este link expira em 1 hora.</p>
                        """;

            try
            {
              await emailService.SendAsync(
                user.Email,
                "Recuperação de acesso - MedlyCare",
                html);
            }
            catch (Exception ex)
            {
              Log.Error(ex, "Erro ao enviar e-mail de recuperação de acesso.");
            }

            return Results.Ok(new
            {
                message = "Se existir um usuário compatível, um e-mail será enviado."
            });
        });

        group.MapPost("/redefinir-senha", async (RedefinirSenhaRequest req, SfaDbContext db) =>
        {
          var tokenValue = req.Token.Trim();
          var novaSenha = req.NovaSenha.Trim();

          if (string.IsNullOrWhiteSpace(tokenValue) || string.IsNullOrWhiteSpace(novaSenha))
            return Results.BadRequest(new { message = "token_novaSenha_obrigatorios" });

          var tokenHash = TokenHasher.Hash(tokenValue);

          var token = await db.PasswordResetTokens
            .Include(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

          if (token is null)
            return Results.BadRequest(new { message = "token_invalido" });

          if (token.UsadoEm.HasValue)
            return Results.BadRequest(new { message = "token_ja_utilizado" });

          if (token.ExpiraEm < DateTime.UtcNow)
            return Results.BadRequest(new { message = "token_expirado" });

          await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
          if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

          await using var cmd = new NpgsqlCommand("SELECT crypt(@p, gen_salt('bf'))", conn);
          cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, novaSenha);

          var hashObj = await cmd.ExecuteScalarAsync();
          var hash = (hashObj as string) ?? throw new InvalidOperationException("Falha ao gerar hash de senha.");

          token.Usuario.PasswordHash = hash;
          token.UsadoEm = DateTime.UtcNow;

          var outrosTokens = await db.PasswordResetTokens
            .Where(x => x.UsuarioId == token.UsuarioId && x.Id != token.Id && x.UsadoEm == null)
            .ToListAsync();

          foreach (var item in outrosTokens)
            item.UsadoEm = DateTime.UtcNow;

          await db.SaveChangesAsync();

          return Results.Ok(new { message = "senha_redefinida_com_sucesso" });
        })
        .WithSummary("Redefine a senha usando token de recuperação")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/me", async (ClaimsPrincipal user, SfaDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var codEmpresaStr = user.FindFirstValue("cod_empresa");
            var nome = user.FindFirstValue(ClaimTypes.Name);
            var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();

            string? crm = null;
            string? email = null;
            string? telefone = null;

            if (Guid.TryParse(userId, out var uid))
            {
                var dbUser = await db.Usuarios
                    .AsNoTracking()
                    .Where(u => u.Id == uid)
                    .Select(u => new { u.Crm, u.Email, u.Telefone })
                    .FirstOrDefaultAsync();

                if (dbUser is not null)
                {
                    crm = dbUser.Crm;
                    email = dbUser.Email;
                    telefone = dbUser.Telefone;
                }
            }

            return Results.Ok(new { userId, codEmpresa = codEmpresaStr, nome, roles, crm, email, telefone });
        }).RequireAuthorization();
    }

    public record LoginRequest(
        int? CodEmpresa,
        [Required, MaxLength(200)] string Login,
        [Required, MaxLength(200)] string Password);
}
