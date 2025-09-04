using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class UsuarioPerfilConfiguration : IEntityTypeConfiguration<UsuarioPerfil>
{
  public void Configure(EntityTypeBuilder<UsuarioPerfil> b)
  {
    b.ToTable("usuario_perfil");
    b.HasKey(x => new { x.UsuarioId, x.PerfilId });

    b.HasOne(x => x.Usuario)
      .WithMany(u => u.UsuariosPerfis)
      .HasForeignKey(x => x.UsuarioId)
      .OnDelete(DeleteBehavior.Cascade);

    b.HasOne(x => x.Perfil)
      .WithMany(p => p.UsuariosPerfis)
      .HasForeignKey(x => x.PerfilId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
