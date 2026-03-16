using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
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

      await using var conn = (Npgsql.NpgsqlConnection)db.Database.GetDbConnection();
      if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

      await using var cmd = new Npgsql.NpgsqlCommand("SELECT crypt(@p, @h) = @h", conn);
      cmd.Parameters.AddWithValue("p", NpgsqlTypes.NpgsqlDbType.Text, req.Password);
      cmd.Parameters.AddWithValue("h", NpgsqlTypes.NpgsqlDbType.Text, user.PasswordHash);

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

  private record LoginRequest(
    [Required] int CodEmpresa,
    [Required, MaxLength(200)] string Login,
    [Required, MaxLength(200)] string Password);
}
