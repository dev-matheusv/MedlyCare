using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class ReceituarioMedicoItemConfiguration : IEntityTypeConfiguration<ReceituarioMedicoItem>
{
  public void Configure(EntityTypeBuilder<ReceituarioMedicoItem> builder)
  {
    builder.ToTable("receituario_medico_item");

    builder.HasKey(x => x.Id);

    builder.Property(x => x.Id)
      .HasColumnName("id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");

    builder.Property(x => x.ReceituarioMedicoId)
      .IsRequired();

    builder.Property(x => x.NomeMedicamento)
      .IsRequired()
      .HasMaxLength(300);

    builder.Property(x => x.FormaFarmaceutica)
      .HasMaxLength(100);

    builder.Property(x => x.Concentracao)
      .HasMaxLength(100);

    builder.Property(x => x.ViaAdministracao)
      .HasMaxLength(100);

    builder.Property(x => x.Posologia)
      .HasMaxLength(1000);

    builder.Property(x => x.Quantidade)
      .IsRequired()
      .HasMaxLength(200);

    builder.Property(x => x.QuantidadeExtenso)
      .HasMaxLength(200);

    builder.Property(x => x.Orientacoes)
      .HasMaxLength(1000);

    builder.HasIndex(x => x.ReceituarioMedicoId);
  }
}
