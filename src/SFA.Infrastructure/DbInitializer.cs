using Microsoft.EntityFrameworkCore;
using SFA.Domain.Entities;

namespace SFA.Infrastructure;

public static class DbInitializer
{
  public static async Task SeedAsync(SfaDbContext db)
  {
    await db.Database.EnsureCreatedAsync();

    if (!await db.Empresas.AnyAsync())
    {
      db.Empresas.Add(new Empresa { Nome = "Clínica SFA", Ativa = true });
      await db.SaveChangesAsync();
    }

    if (!await db.Usuarios.AnyAsync())
    {
      // senha seed (apenas dev)
      var plain = "admin123";

      // EF Core 8: SqlQuery<T> + interpolated => parâmetros (seguro)
      var hash = await db.Database
        .SqlQuery<string>($"SELECT crypt({plain}, gen_salt('bf')) AS \"Value\" ")
        .FirstAsync(); // mapeia a única coluna para string, indep. do nome

      db.Usuarios.Add(new Usuario
      {
        CodEmpresa = 1,
        Login = "admin@sfa",
        Nome = "Administrador",
        PasswordHash = hash,
        Ativo = true
      });
      await db.SaveChangesAsync();
    }

    if (!await db.Perfis.AnyAsync(p => p.CodEmpresa == 1))
    {
      db.Perfis.AddRange(
        new Perfil { CodEmpresa = 1, Nome = "Admin" },
        new Perfil { CodEmpresa = 1, Nome = "Profissional" },
        new Perfil { CodEmpresa = 1, Nome = "Recepcao" }
      );
      await db.SaveChangesAsync();
    }

    var admin = await db.Usuarios.FirstAsync(u => u.CodEmpresa == 1 && u.Login == "admin@sfa");
    var adminPerfil = await db.Perfis.FirstAsync(p => p.CodEmpresa == 1 && p.Nome == "Admin");

    if (!await db.UsuariosPerfis.AnyAsync(up => up.UsuarioId == admin.Id && up.PerfilId == adminPerfil.Id))
    {
      db.UsuariosPerfis.Add(new UsuarioPerfil { UsuarioId = admin.Id, PerfilId = adminPerfil.Id });
      await db.SaveChangesAsync();
    }
  }
}
