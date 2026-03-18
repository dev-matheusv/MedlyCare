using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class AtestadoConfiguration : IEntityTypeConfiguration<Atestado>
{
    public void Configure(EntityTypeBuilder<Atestado> builder)
    {
        builder.ToTable("atestado");

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

        builder.Property(x => x.DataEmissao)
            .IsRequired();

        builder.Property(x => x.DiasAfastamento)
            .IsRequired();

        builder.Property(x => x.DescricaoCurta)
            .HasMaxLength(500);

        builder.Property(x => x.Cid)
            .HasMaxLength(10);

        builder.Property(x => x.LocalEmissao)
            .HasMaxLength(200);

        builder.Property(x => x.Crm)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.AssinaturaNome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Cancelado)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.MotivoCancelamento)
            .HasMaxLength(500);

        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasDefaultValueSql("now()");

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

        builder.HasIndex(x => new { x.CodEmpresa, x.PacienteId, x.DataEmissao });
        builder.HasIndex(x => new { x.CodEmpresa, x.ProfissionalId, x.DataEmissao });
        builder.HasIndex(x => new { x.CodEmpresa, x.Cancelado });
    }
}
