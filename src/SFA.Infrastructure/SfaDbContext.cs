using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SFA.Domain.Entities;

namespace SFA.Infrastructure;

public class SfaDbContext(DbContextOptions<SfaDbContext> options) : DbContext(options)
{
  public DbSet<Empresa> Empresas => Set<Empresa>();
  public DbSet<Usuario> Usuarios => Set<Usuario>();
  public DbSet<Perfil> Perfis => Set<Perfil>();
  public DbSet<UsuarioPerfil> UsuariosPerfis => Set<UsuarioPerfil>();
  public DbSet<Paciente> Pacientes => Set<Paciente>();
  public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
  public DbSet<Atendimento> Atendimentos => Set<Atendimento>();
  public DbSet<ConfirmacaoAgendamento> ConfirmacoesAgendamento => Set<ConfirmacaoAgendamento>();
  public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
  public DbSet<AnexoPaciente> AnexosPaciente => Set<AnexoPaciente>();
  public DbSet<LogAcessoAnexoPaciente> LogsAcessoAnexoPaciente => Set<LogAcessoAnexoPaciente>();
  public DbSet<Atestado> Atestados => Set<Atestado>();
  public DbSet<ReceituarioMedico> ReceituariosMedicos => Set<ReceituarioMedico>();
  public DbSet<ReceituarioMedicoItem> ReceituariosMedicosItens => Set<ReceituarioMedicoItem>();

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
