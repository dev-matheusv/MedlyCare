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
      // Gera hash bcrypt usando pgcrypto
      var hash = await db.Database
        .SqlQuery<string>($"SELECT crypt(admin123, gen_salt('bf'))")
        .FirstAsync();

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
  }
}
