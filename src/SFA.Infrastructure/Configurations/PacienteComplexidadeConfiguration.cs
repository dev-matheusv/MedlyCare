using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class PacienteComplexidadeConfiguration : IEntityTypeConfiguration<PacienteComplexidade>
{
    public void Configure(EntityTypeBuilder<PacienteComplexidade> builder)
    {
        builder.ToTable("paciente_complexidade");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.CodEmpresa).IsRequired();
        builder.Property(x => x.PacienteId).IsRequired();
        builder.Property(x => x.ComplexidadeId).IsRequired();
        builder.Property(x => x.UsuarioId).IsRequired();

        builder.Property(x => x.Data)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.HasOne(x => x.Paciente)
            .WithMany()
            .HasForeignKey(x => x.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Complexidade)
            .WithMany()
            .HasForeignKey(x => x.ComplexidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Atendimento)
            .WithMany()
            .HasForeignKey(x => x.AtendimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(x => new { x.CodEmpresa, x.PacienteId, x.Data });
        builder.HasIndex(x => new { x.CodEmpresa, x.ComplexidadeId });
    }
}
