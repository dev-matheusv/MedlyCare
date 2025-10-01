using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Npgsql;
using NpgsqlTypes;
using SFA.Application.Auth;
using SFA.Domain.Entities;
using SFA.Infrastructure;

namespace SFA.Api.Endpoints;

public static class AuthEndpoints
{
  public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
  {
    var group = routes.MapGroup("/api/v1/auth");
    
    group.MapPost("/login", async (LoginRequest req, SfaDbContext db, IJwtTokenService jwtSvc) =>
    {
        var login = (req.Login).Trim();
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(req.Password))
            return Results.BadRequest(new { message = "credenciais_invalidas" });

        // 0 ou negativos => considera como não informado
        int? codEmpNormalized = (req.CodEmpresa.HasValue && req.CodEmpresa.Value > 0)
            ? req.CodEmpresa.Value
            : null;

        Usuario? user;

        if (codEmpNormalized is { } codEmp)
        {
            user = await db.Usuarios.FirstOrDefaultAsync(u =>
                u.CodEmpresa == codEmp &&
                u.Ativo &&
                (u.Login == login || EF.Functions.ILike(u.Login, login))
            );
            if (user is null) return Results.NotFound("user_not_found");
        }
        else
        {
            var matches = await db.Usuarios
                .Where(u => u.Ativo && (u.Login == login || EF.Functions.ILike(u.Login, login)))
                .Select(u => new { u.Id })
                .ToListAsync();

            if (matches.Count == 0) return Results.NotFound("user_not_found");
            if (matches.Count > 1) return Results.Conflict(new { message = "login_ambiguous" });

            var id = matches[0].Id;
            user = await db.Usuarios.FirstAsync(u => u.Id == id);
        }

        await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT crypt(@p, @h) = @h", conn);
        cmd.Parameters.AddWithValue("p", NpgsqlDbType.Text, req.Password);
        cmd.Parameters.AddWithValue("h", NpgsqlDbType.Text, user.PasswordHash);
        var valid = (bool)(await cmd.ExecuteScalarAsync() ?? false);
        if (!valid) return Results.Unauthorized();

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
  public record LoginRequest(int? CodEmpresa, string Login, string Password);
}
