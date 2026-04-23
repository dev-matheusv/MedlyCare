using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
  public void Configure(EntityTypeBuilder<Usuario> builder)
  {
    builder.ToTable("usuario");

    builder.HasKey(x => x.Id);

    builder.Property(x => x.Id)
      .HasColumnName("id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");

    builder.Property(x => x.CodEmpresa)
      .HasColumnName("cod_empresa")
      .IsRequired();

    builder.Property(x => x.Login)
      .HasColumnName("login")
      .IsRequired()
      .HasMaxLength(120);

    builder.Property(x => x.Nome)
      .HasColumnName("nome")
      .IsRequired()
      .HasMaxLength(200);

    builder.Property(x => x.Email)
      .HasColumnName("email")
      .IsRequired()
      .HasMaxLength(200);

    builder.Property(x => x.Telefone)
      .HasColumnName("telefone")
      .HasMaxLength(20);

    builder.Property(x => x.CelularWhatsapp)
      .HasColumnName("celular_whatsapp")
      .HasMaxLength(20);

    builder.Property(x => x.Crm)
      .HasColumnName("crm")
      .HasMaxLength(20);

    builder.Property(x => x.PasswordHash)
      .HasColumnName("password_hash")
      .IsRequired();

    builder.Property(x => x.Ativo)
      .HasColumnName("ativo")
      .IsRequired();

    builder.Property(x => x.CriadoEm)
      .HasColumnName("criado_em")
      .HasDefaultValueSql("now()");

    builder.HasIndex(x => new { x.CodEmpresa, x.Login }).IsUnique();
    builder.HasIndex(x => new { x.CodEmpresa, x.Email });
  }
}
