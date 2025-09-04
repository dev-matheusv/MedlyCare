using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
  public void Configure(EntityTypeBuilder<Perfil> b)
  {
    b.ToTable("perfil");
    b.HasKey(x => x.Id);
    b.Property(x => x.Nome).IsRequired().HasMaxLength(80);
    b.Property(x => x.CriadoEm).HasDefaultValueSql("now()");
    b.Property(x => x.Ativo).HasDefaultValue(true);

    // Se perfil for por empresa:
    b.HasIndex(x => new { x.CodEmpresa, x.Nome }).IsUnique();
  }
}
