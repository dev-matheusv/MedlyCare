using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
  public void Configure(EntityTypeBuilder<Perfil> builder)
  {
    builder.ToTable("perfil");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
      .HasColumnName("id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");
    builder.Property(x => x.Nome).IsRequired().HasMaxLength(80);
    builder.Property(x => x.CriadoEm).HasDefaultValueSql("now()");
    builder.Property(x => x.Ativo).HasDefaultValue(true);

    // Se perfil for por empresa:
    builder.HasIndex(x => new { x.CodEmpresa, x.Nome }).IsUnique();
  }
}
