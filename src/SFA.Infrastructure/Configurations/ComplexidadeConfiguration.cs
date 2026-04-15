using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class ComplexidadeConfiguration : IEntityTypeConfiguration<Complexidade>
{
    public void Configure(EntityTypeBuilder<Complexidade> builder)
    {
        builder.ToTable("complexidade");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.CodEmpresa).IsRequired();

        builder.Property(x => x.Descricao)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Cor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Ativo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CriadoEm)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(x => x.AtualizadoEm)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.HasIndex(x => new { x.CodEmpresa, x.Ativo });
    }
}
