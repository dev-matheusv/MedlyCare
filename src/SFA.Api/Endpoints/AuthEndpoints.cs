using Microsoft.EntityFrameworkCore;
using Npgsql; 
using SFA.Infrastructure;

namespace SFA.Api.Endpoints;

public static class AuthEndpoints
{
  public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
  {
    var group = routes.MapGroup("/api/v1/auth");

    group.MapPost("/login", async (LoginRequest req, SfaDbContext db) =>
    {
      var user = await db.Usuarios
        .FirstOrDefaultAsync(u => u.Login == req.Login
                                  && u.CodEmpresa == req.CodEmpresa
                                  && u.Ativo);
      if (user is null) return Results.NotFound("user_not_found");

      // --- validação com ADO.NET/Npgsql (determinística) ---
      await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
      if (conn.State != System.Data.ConnectionState.Open)
        await conn.OpenAsync();

      await using var cmd = new NpgsqlCommand("SELECT crypt(@p, @h) = @h", conn);
      cmd.Parameters.AddWithValue("p", NpgsqlTypes.NpgsqlDbType.Text, req.Password);
      cmd.Parameters.AddWithValue("h", NpgsqlTypes.NpgsqlDbType.Text, user.PasswordHash);

      var scalar = await cmd.ExecuteScalarAsync();
      var valid = scalar is bool b && b;

      return !valid ? Results.Unauthorized() : Results.Ok(new { message = "Login OK (JWT vem depois)", user = new { user.Nome, user.CodEmpresa } });
    });
  }

  public record LoginRequest(int CodEmpresa, string Login, string Password);
}
