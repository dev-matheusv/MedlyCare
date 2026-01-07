using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
  public void Configure(EntityTypeBuilder<Empresa> builder)
  {
    builder.ToTable("empresa");

    // PK real do tenant (bate com a migration inicial)
    builder.HasKey(x => x.CodEmpresa);

    builder.Property(x => x.CodEmpresa)
      .HasColumnName("cod_empresa")
      .HasColumnType("integer")
      .ValueGeneratedOnAdd();

    // Identificador auxiliar (GUID)
    builder.Property(x => x.Id)
      .HasColumnName("id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");

    builder.HasIndex(x => x.Id).IsUnique();

    builder.Property(x => x.Nome)
      .HasColumnName("nome")
      .IsRequired()
      .HasMaxLength(200);

    builder.Property(x => x.Ativa)
      .HasColumnName("ativa")
      .IsRequired();

    builder.Property(x => x.CriadoEm)
      .HasColumnName("criado_em")
      .HasDefaultValueSql("now()");

    builder.HasIndex(x => x.Nome).IsUnique(false);
  }
}
