using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class ComplexidadePacienteConfiguration : IEntityTypeConfiguration<ComplexidadePaciente>
{
    public void Configure(EntityTypeBuilder<ComplexidadePaciente> builder)
    {
        builder.ToTable("complexidade_paciente");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.CodEmpresa)
            .IsRequired();

        builder.Property(x => x.PacienteId)
            .IsRequired();

        builder.Property(x => x.ProfissionalId)
            .IsRequired();

        builder.Property(x => x.Nivel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Cor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Observacoes)
            .HasMaxLength(500);

        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(x => x.CriadoPorUsuarioId)
            .IsRequired();

        builder.HasOne(x => x.Paciente)
            .WithMany()
            .HasForeignKey(x => x.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Profissional)
            .WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice para busca eficiente da última complexidade por paciente
        builder.HasIndex(x => new { x.CodEmpresa, x.PacienteId, x.CriadoEm });
        builder.HasIndex(x => new { x.CodEmpresa, x.ProfissionalId });
    }
}
