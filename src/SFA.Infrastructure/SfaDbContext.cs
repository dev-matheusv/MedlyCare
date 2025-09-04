using Microsoft.EntityFrameworkCore;
using SFA.Domain.Entities;

namespace SFA.Infrastructure;

public class SfaDbContext : DbContext
{
    public SfaDbContext(DbContextOptions<SfaDbContext> options) : base(options) { }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<UsuarioPerfil> UsuariosPerfis => Set<UsuarioPerfil>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SfaDbContext).Assembly);
    }
}
