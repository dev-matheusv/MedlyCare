using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
  public void Configure(EntityTypeBuilder<Paciente> builder)
  {
    builder.ToTable("paciente");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
      .HasColumnName("id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");

    builder.Property(x => x.CodEmpresa).IsRequired();
    builder.Property(x => x.Nome).IsRequired().HasMaxLength(120);
    builder.Property(x => x.Documento).IsRequired().HasMaxLength(20);
    builder.Property(x => x.DataNascimento).HasColumnType("date"); 
    builder.Property(x => x.Telefone).HasMaxLength(20);
    builder.Property(x => x.Email).HasMaxLength(180);
    builder.Property(x => x.CriadoEm).HasDefaultValueSql("now()");
    builder.Property(x => x.Ativo).HasDefaultValue(true);

    // Índices úteis para pesquisa
    builder.HasIndex(x => new { x.CodEmpresa, x.Nome });
    builder.HasIndex(x => new { x.CodEmpresa, x.Documento });
    // Se quiser documento único por empresa, troque por Unique:
    // b.HasIndex(x => new { x.CodEmpresa, x.Documento }).IsUnique();
  }
}
