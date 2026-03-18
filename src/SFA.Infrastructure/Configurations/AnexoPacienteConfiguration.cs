using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class AnexoPacienteConfiguration : IEntityTypeConfiguration<AnexoPaciente>
{
    public void Configure(EntityTypeBuilder<AnexoPaciente> builder)
    {
        builder.ToTable("anexo_paciente");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.CodEmpresa)
            .IsRequired();

        builder.Property(x => x.PacienteId)
            .IsRequired();

        builder.Property(x => x.TipoDocumento)
            .IsRequired();

        builder.Property(x => x.NomeArquivo)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.NomeArmazenado)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.TamanhoBytes)
            .IsRequired();

        builder.Property(x => x.HashSha256)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.UrlStorage)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Descricao)
            .HasMaxLength(1000);

        builder.Property(x => x.EnviadoPorId)
            .IsRequired();

        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(x => x.AtualizadoEm);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedReason)
            .HasMaxLength(500);

        builder.HasOne(x => x.Paciente)
            .WithMany()
            .HasForeignKey(x => x.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EnviadoPor)
            .WithMany()
            .HasForeignKey(x => x.EnviadoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CodEmpresa, x.PacienteId });
        builder.HasIndex(x => new { x.CodEmpresa, x.TipoDocumento });
        builder.HasIndex(x => new { x.CodEmpresa, x.IsDeleted });
        builder.HasIndex(x => x.HashSha256);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
