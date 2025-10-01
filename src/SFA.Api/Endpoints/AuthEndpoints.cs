using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using SFA.Application.Auth;
using SFA.Infrastructure;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace SFA.Api.Endpoints;

public static class AuthEndpoints
{
  public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
  {
    static IResult ApiError(string code, string message, int statusCode) =>
      Results.Json(new { code, message }, statusCode: statusCode);

    var group = routes.MapGroup("/api/v1/auth");
    
    group.MapPost("/login", async (LoginRequest req, SfaDbContext db, IJwtTokenService jwtSvc) =>
    {
        // helper local já declarado no topo do método
        // static IResult ApiError(...)

        var login = req.Login.Trim();
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(req.Password))
            return ApiError("invalid_request", "credenciais_invalidas", StatusCodes.Status400BadRequest);

        // normaliza codEmpresa: <=0 => não informado
        int? codEmpNormalized = (req.CodEmpresa.HasValue && req.CodEmpresa.Value > 0)
            ? req.CodEmpresa.Value
            : null;

        Domain.Entities.Usuario? user;

        if (codEmpNormalized is { } codEmp)
        {
            user = await db.Usuarios.FirstOrDefaultAsync(u =>
                u.CodEmpresa == codEmp &&
                u.Ativo &&
                (u.Login == login || EF.Functions.ILike(u.Login, login))
            );
            if (user is null)
                return ApiError("user_not_found", "Usuário não encontrado.", StatusCodes.Status404NotFound);
        }
        else
        {
            var matches = await db.Usuarios
                .Where(u => u.Ativo && (u.Login == login || EF.Functions.ILike(u.Login, login)))
                .Select(u => new { u.Id })
                .ToListAsync();

            if (matches.Count == 0)
                return ApiError("user_not_found", "Usuário não encontrado.", StatusCodes.Status404NotFound);

            if (matches.Count > 1)
                return ApiError("login_ambiguous", "Mesmo login em múltiplas empresas.", StatusCodes.Status409Conflict);

            user = await db.Usuarios.FirstAsync(u => u.Id == matches[0].Id);
        }

        // valida senha (pgcrypto)
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
    }).WithSummary("Autentica e retorna um JWT")
    .WithDescription("Se `codEmpresa` não for informado ou for <= 0, a empresa é inferida pelo login.")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status409Conflict)
    .WithOpenApi(op =>
    {
      // exemplo: login sem codEmpresa
      var exSemCodEmpresa = new OpenApiObject
      {
        ["login"] = new OpenApiString("admin@sfa"),
        ["password"] = new OpenApiString("admin123")
      };

      // exemplo: login com codEmpresa explícito
      var exComCodEmpresa = new OpenApiObject
      {
        ["codEmpresa"] = new OpenApiInteger(1),
        ["login"] = new OpenApiString("admin@sfa"),
        ["password"] = new OpenApiString("admin123")
      };

      op.RequestBody = new OpenApiRequestBody
      {
        Required = true,
        Content =
        {
          ["application/json"] = new OpenApiMediaType
          {
            Examples =
            {
              ["semCodEmpresa"] = new OpenApiExample
              {
                Summary = "Login sem codEmpresa (recomendado)",
                Value = exSemCodEmpresa
              },
              ["comCodEmpresa"] = new OpenApiExample
              {
                Summary = "Login com codEmpresa explícito (compatível)",
                Value = exComCodEmpresa
              }
            }
          }
        }
      };
      return op;
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

  public record LoginRequest(
    int? CodEmpresa,
    [Required, MaxLength(200)] string Login,
    [Required, MaxLength(200)] string Password);
}
