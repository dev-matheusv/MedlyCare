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

        builder.Property(x => x.CodEmpresa).IsRequired();
        builder.Property(x => x.Login).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PasswordHash).IsRequired();

        builder.HasIndex(x => new { x.CodEmpresa, x.Login }).IsUnique();
    }
}
