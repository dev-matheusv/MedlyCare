using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class LogAcessoAnexoPacienteConfiguration : IEntityTypeConfiguration<LogAcessoAnexoPaciente>
{
  public void Configure(EntityTypeBuilder<LogAcessoAnexoPaciente> builder)
  {
    builder.ToTable("log_acesso_anexo_paciente");

    builder.HasKey(x => x.Id);

    builder.Property(x => x.Id)
      .HasColumnName("id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");

    builder.Property(x => x.CodEmpresa)
      .IsRequired();

    builder.Property(x => x.AnexoPacienteId)
      .IsRequired();

    builder.Property(x => x.UsuarioId)
      .IsRequired();

    builder.Property(x => x.Acao)
      .IsRequired();

    builder.Property(x => x.Ip)
      .HasMaxLength(100);

    builder.Property(x => x.DataHora)
      .IsRequired()
      .HasDefaultValueSql("now()");

    builder.HasOne(x => x.AnexoPaciente)
      .WithMany()
      .HasForeignKey(x => x.AnexoPacienteId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasOne(x => x.Usuario)
      .WithMany()
      .HasForeignKey(x => x.UsuarioId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasIndex(x => new { x.CodEmpresa, x.AnexoPacienteId, x.DataHora });
    builder.HasIndex(x => new { x.CodEmpresa, x.UsuarioId, x.DataHora });
  }
}
