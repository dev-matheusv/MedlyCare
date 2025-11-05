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
    builder.Property(a => a.Id)
           .HasColumnName("id")
           .HasColumnType("uuid")
           .HasDefaultValueSql("gen_random_uuid()");

    builder.Property(a => a.CodEmpresa).HasColumnName("cod_empresa").IsRequired();

    builder.Property(a => a.PacienteId).HasColumnName("paciente_id").HasColumnType("uuid").IsRequired();
    builder.Property(a => a.ProfissionalId).HasColumnName("profissional_id").HasColumnType("uuid").IsRequired();

    builder.Property(a => a.InicioUtc).HasColumnName("inicio_utc").HasColumnType("timestamptz").IsRequired();
    builder.Property(a => a.FimUtc).HasColumnName("fim_utc").HasColumnType("timestamptz").IsRequired();

    builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("agendado").IsRequired();
    builder.Property(a => a.Observacoes).HasColumnName("observacoes").HasColumnType("text");

    builder.Property(a => a.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").HasColumnType("uuid").IsRequired();
    builder.Property(a => a.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("now() at time zone 'utc'");
    builder.Property(a => a.AlteradoPorUsuarioId).HasColumnName("alterado_por_usuario_id").HasColumnType("uuid");
    builder.Property(a => a.AlteradoEm).HasColumnName("alterado_em");

    // Soft delete
    builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
    builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
    builder.Property(a => a.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid");
    builder.Property(a => a.DeletedReason).HasColumnName("deleted_reason").HasColumnType("text");

    builder.HasOne(a => a.Paciente)
      .WithMany()
      .HasForeignKey(a => a.PacienteId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasOne(a => a.Profissional)
      .WithMany()
      .HasForeignKey(a => a.ProfissionalId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasIndex(a => new { a.CodEmpresa, a.ProfissionalId, a.InicioUtc });
    builder.HasIndex(a => new { a.CodEmpresa, a.PacienteId, a.InicioUtc });
  }
}
