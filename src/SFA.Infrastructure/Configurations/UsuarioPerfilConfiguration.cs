using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class UsuarioPerfilConfiguration : IEntityTypeConfiguration<UsuarioPerfil>
{
  public void Configure(EntityTypeBuilder<UsuarioPerfil> builder)
  {
    builder.ToTable("usuario_perfil");
    builder.HasKey(x => new { x.UsuarioId, x.PerfilId });
    builder.Property(x => x.UsuarioId)
      .HasColumnName("usuario_id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");
    
    builder.Property(x => x.PerfilId)
      .HasColumnName("perfil_id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");

    builder.HasOne(x => x.Usuario)
      .WithMany(u => u.UsuariosPerfis)
      .HasForeignKey(x => x.UsuarioId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(x => x.Perfil)
      .WithMany(p => p.UsuariosPerfis)
      .HasForeignKey(x => x.PerfilId)
      .OnDelete(DeleteBehavior.Cascade);
    
    builder.HasIndex(x => x.UsuarioId);
    builder.HasIndex(x => x.PerfilId);
  }
}
