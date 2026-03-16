using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Npgsql;
using NpgsqlTypes;
using SFA.Application.Auth;
using SFA.Infrastructure;

namespace SFA.Api.Endpoints;

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

            if (req.CodEmpresa <= 0 || string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(req.Password))
                return ApiError("invalid_request", "empresa_login_senha_obrigatorios", StatusCodes.Status400BadRequest);

            var user = await db.Usuarios.FirstOrDefaultAsync(u =>
                u.CodEmpresa == req.CodEmpresa &&
                u.Ativo &&
                EF.Functions.ILike(u.Login, login));

            if (user is null)
                return ApiError("user_not_found", "Usuário não encontrado.", StatusCodes.Status404NotFound);

            await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT crypt(@p, @h) = @h", conn);
            cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, req.Password);
            cmd.Parameters.AddWithValue("h", NpgsqlDbType.Text, user.PasswordHash);

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
        .WithDescription("Login com empresa obrigatória.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/recuperar-acesso", async (
            RecuperarAcessoRequest req,
            SfaDbContext db,
            IEmailService emailService,
            IOptions<SmtpOptions> smtpOptions) =>
        {
            if (req.CodEmpresa <= 0 || string.IsNullOrWhiteSpace(req.Email))
                return Results.BadRequest(new { message = "empresa_email_obrigatorios" });

            var email = req.Email.Trim();

            var user = await db.Usuarios.FirstOrDefaultAsync(u =>
                u.CodEmpresa == req.CodEmpresa &&
                u.Ativo &&
                EF.Functions.ILike(u.Email, email));

            if (user is not null)
            {
                var agora = DateTime.UtcNow;

                var tokensAtivos = await db.PasswordResetTokens
                    .Where(x => x.UsuarioId == user.Id && x.UsadoEm == null && x.ExpiraEm > agora)
                    .ToListAsync();

                foreach (var item in tokensAtivos)
                    item.UsadoEm = agora;

                var novoToken = Guid.NewGuid().ToString("N");

                db.PasswordResetTokens.Add(new Domain.Entities.PasswordResetToken
                {
                    UsuarioId = user.Id,
                    Token = novoToken,
                    ExpiraEm = agora.AddHours(1)
                });

                await db.SaveChangesAsync();

                var baseUrl = smtpOptions.Value.ResetBaseUrl.TrimEnd('/');
                var link = $"{baseUrl}?token={novoToken}";

                var html = $"""
                    <p>Olá, {user.Nome}.</p>
                    <p>Recebemos uma solicitação para redefinição de senha no MedlyCare.</p>
                    <p>Clique no link abaixo para cadastrar uma nova senha:</p>
                    <p><a href="{link}">{link}</a></p>
                    <p>Este link expira em 1 hora.</p>
                    <p>Se você não solicitou a redefinição, ignore este e-mail.</p>
                    """;

                try
                {
                    await emailService.SendAsync(user.Email, "Recuperação de acesso - MedlyCare", html);
                }
                catch
                {
                  // ignored
                }
            }

            return Results.Ok(new
            {
                message = "Se existir um usuário compatível, um e-mail de recuperação será enviado."
            });
        })
        .WithSummary("Solicita recuperação de acesso por e-mail")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/redefinir-senha", async (RedefinirSenhaRequest req, SfaDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NovaSenha))
                return Results.BadRequest(new { message = "token_novaSenha_obrigatorios" });

            var token = await db.PasswordResetTokens
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Token == req.Token);

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
            cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, req.NovaSenha);

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

        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var codEmpresa = user.FindFirstValue("cod_empresa");
            var nome = user.FindFirstValue(ClaimTypes.Name);
            var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();

            return Results.Ok(new { userId, codEmpresa, nome, roles });
        }).RequireAuthorization();
    }

    public record LoginRequest(
        [Required] int CodEmpresa,
        [Required, MaxLength(200)] string Login,
        [Required, MaxLength(200)] string Password);
}
