using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
  public void Configure(EntityTypeBuilder<Agendamento> builder)
  {
    builder.ToTable("agendamento");

    builder.HasKey(a => a.Id);

    builder.Property(a => a.CodEmpresa).IsRequired();

    builder.Property(a => a.InicioUtc).HasColumnType("timestamptz").IsRequired();
    builder.Property(a => a.FimUtc).HasColumnType("timestamptz").IsRequired();

    builder.Property(a => a.Status)
      .HasMaxLength(20)
      .HasDefaultValue("agendado")
      .IsRequired();

    builder.Property(a => a.Observacoes).HasColumnType("text");

    builder.Property(a => a.CriadoEm).HasDefaultValueSql("now() at time zone 'utc'");
    builder.Property(a => a.AlteradoEm);

    // Relacionamentos (FK por Id; escopo por empresa será validado nos endpoints)
    builder.HasOne(a => a.Paciente)
      .WithMany()
      .HasForeignKey(a => a.PacienteId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasOne(a => a.Profissional)
      .WithMany()
      .HasForeignKey(a => a.ProfissionalId)
      .OnDelete(DeleteBehavior.Restrict);

    // Índices para consultas rápidas
    builder.HasIndex(a => new { a.CodEmpresa, a.ProfissionalId, a.InicioUtc });
    builder.HasIndex(a => new { a.CodEmpresa, a.PacienteId, a.InicioUtc });
  }
}
