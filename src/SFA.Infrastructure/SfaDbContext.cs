using System.Linq.Expressions;
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
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.HasPostgresExtension("pgcrypto");
      modelBuilder.ApplyConfigurationsFromAssembly(typeof(SfaDbContext).Assembly);

      // filtro global IsDeleted
      foreach (var et in modelBuilder.Model.GetEntityTypes())
      {
        var prop = et.FindProperty("IsDeleted");
        if (prop?.ClrType == typeof(bool))
        {
          var p = Expression.Parameter(et.ClrType, "e");
          var body = Expression.Equal(Expression.Property(p, "IsDeleted"), Expression.Constant(false));
          modelBuilder.Entity(et.ClrType).HasQueryFilter(Expression.Lambda(body, p));
        }
      }
    }

}
