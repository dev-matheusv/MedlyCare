using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class ReceituarioMedicoConfiguration : IEntityTypeConfiguration<ReceituarioMedico>
{
    public void Configure(EntityTypeBuilder<ReceituarioMedico> builder)
    {
        builder.ToTable("receituario_medico");

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

        builder.Property(x => x.TipoReceituario)
            .IsRequired();

        builder.Property(x => x.DataEmissao)
            .IsRequired();

        builder.Property(x => x.Diagnostico)
            .HasMaxLength(1000);

        builder.Property(x => x.InformarCid)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Cid)
            .HasMaxLength(10);

        builder.Property(x => x.Observacoes)
            .HasMaxLength(2000);

        builder.Property(x => x.AssinaturaNome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.RegistroProfissional)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.EnderecoProfissional)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Cancelado)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.MotivoCancelamento)
            .HasMaxLength(500);

        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(x => x.AtualizadoEm);

        builder.HasOne(x => x.Paciente)
            .WithMany()
            .HasForeignKey(x => x.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Profissional)
            .WithMany()
            .HasForeignKey(x => x.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Atendimento)
            .WithMany()
            .HasForeignKey(x => x.AtendimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Itens)
            .WithOne(x => x.ReceituarioMedico)
            .HasForeignKey(x => x.ReceituarioMedicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CodEmpresa, x.PacienteId, x.DataEmissao });
        builder.HasIndex(x => new { x.CodEmpresa, x.ProfissionalId, x.DataEmissao });
        builder.HasIndex(x => new { x.CodEmpresa, x.Cancelado });
    }
}
