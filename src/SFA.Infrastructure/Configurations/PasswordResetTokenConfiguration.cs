using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
  public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
  {
    builder.ToTable("password_reset_token");

    builder.HasKey(x => x.Id);

    builder.Property(x => x.Id)
      .HasColumnName("id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");

    builder.Property(x => x.UsuarioId)
      .HasColumnName("usuario_id")
      .IsRequired();

    builder.Property(x => x.Token)
      .HasColumnName("token")
      .IsRequired()
      .HasMaxLength(200);

    builder.Property(x => x.ExpiraEm)
      .HasColumnName("expira_em")
      .IsRequired();

    builder.Property(x => x.UsadoEm)
      .HasColumnName("usado_em");

    builder.Property(x => x.CriadoEm)
      .HasColumnName("criado_em")
      .HasDefaultValueSql("now()");

    builder.HasIndex(x => x.Token).IsUnique();
    builder.HasIndex(x => x.UsuarioId);

    builder.HasOne(x => x.Usuario)
      .WithMany(x => x.PasswordResetTokens)
      .HasForeignKey(x => x.UsuarioId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
