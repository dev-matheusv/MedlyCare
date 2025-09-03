using Microsoft.EntityFrameworkCore;
using Npgsql;
using SFA.Infrastructure;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using SFA.Application.Auth;

namespace SFA.Api.Endpoints;

public static class AuthEndpoints
{
  public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
  {
    var group = routes.MapGroup("/api/v1/auth");

    group.MapPost("/login", async (LoginRequest req, SfaDbContext db, IJwtTokenService jwtSvc) =>
    {
      var user = await db.Usuarios
        .FirstOrDefaultAsync(u => u.Login == req.Login
                                  && u.CodEmpresa == req.CodEmpresa
                                  && u.Ativo);
      if (user is null) return Results.NotFound("user_not_found");

      await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
      if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
      await using var cmd = new NpgsqlCommand("SELECT crypt(@p, @h) = @h", conn);
      cmd.Parameters.AddWithValue("p", NpgsqlTypes.NpgsqlDbType.Text, req.Password);
      cmd.Parameters.AddWithValue("h", NpgsqlTypes.NpgsqlDbType.Text, user.PasswordHash);
      var valid = (bool)(await cmd.ExecuteScalarAsync() ?? false);
      if (!valid) return Results.Unauthorized();

      // TODO: buscar roles reais quando criar perfis; por ora, “Admin”
      var roles = new[] { "Admin" };
      var token = jwtSvc.CreateToken(user.Id, user.CodEmpresa, user.Nome, roles);
      return Results.Ok(new { access_token = token.AccessToken, token_type = token.TokenType, expires_at_utc = token.ExpiresAtUtc });
    });
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
  
  public record LoginRequest(int CodEmpresa, string Login, string Password);
}
