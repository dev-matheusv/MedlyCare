using Microsoft.EntityFrameworkCore;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class UsuarioPerfilEndpoints
{
    public static void MapUsuarioPerfilEndpoints(this IEndpointRouteBuilder app)
    {
      var g = app.MapGroup("/api/v1/usuarios/{usuarioId:guid}/perfis")
        .RequireAuthorization("Admin"); // somente Admin

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }

        // Lista perfis do usuário
        g.MapGet("/", async (ClaimsPrincipal u, Guid usuarioId, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var existsUser = await db.Usuarios.AnyAsync(x => x.Id == usuarioId && x.CodEmpresa == codEmp);
            if (!existsUser) return Results.NotFound();

            var perfis = await db.UsuariosPerfis
                .Where(up => up.UsuarioId == usuarioId)
                .Select(up => new { up.PerfilId, up.Perfil.Nome, up.Perfil.Ativo })
                .ToListAsync();

            return Results.Ok(perfis);
        });

        // Vincular perfil ao usuário
        g.MapPost("/{perfilId:guid}", async (ClaimsPrincipal u, Guid usuarioId, Guid perfilId, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId && x.CodEmpresa == codEmp);
            if (user is null) return Results.NotFound(new { message = "usuario_nao_encontrado" });

            var perfil = await db.Perfis.FirstOrDefaultAsync(p => p.Id == perfilId && p.CodEmpresa == codEmp && p.Ativo);
            if (perfil is null) return Results.NotFound(new { message = "perfil_nao_encontrado_ou_inativo" });

            var jaVinculado = await db.UsuariosPerfis.AnyAsync(up => up.UsuarioId == usuarioId && up.PerfilId == perfilId);
            if (jaVinculado) return Results.Conflict(new { message = "usuario_ja_possui_este_perfil" });

            db.UsuariosPerfis.Add(new Domain.Entities.UsuarioPerfil { UsuarioId = usuarioId, PerfilId = perfilId });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Desvincular perfil do usuário
        g.MapDelete("/{perfilId:guid}", async (ClaimsPrincipal u, Guid usuarioId, Guid perfilId, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId && x.CodEmpresa == codEmp);
            if (user is null) return Results.NotFound(new { message = "usuario_nao_encontrado" });

            // garante que o perfil é da mesma empresa (evita desvincular algo “cruzado”)
            var perfilOk = await db.Perfis.AnyAsync(p => p.Id == perfilId && p.CodEmpresa == codEmp);
            if (!perfilOk) return Results.NotFound(new { message = "perfil_nao_encontrado" });

            var vinc = await db.UsuariosPerfis.FirstOrDefaultAsync(up => up.UsuarioId == usuarioId && up.PerfilId == perfilId);
            if (vinc is null) return Results.NotFound(new { message = "vinculo_nao_existe" });

            db.UsuariosPerfis.Remove(vinc);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
