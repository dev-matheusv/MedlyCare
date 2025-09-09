using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
  public void Configure(EntityTypeBuilder<Paciente> b)
  {
    b.ToTable("paciente");
    b.HasKey(x => x.Id);

    b.Property(x => x.CodEmpresa).IsRequired();
    b.Property(x => x.Nome).IsRequired().HasMaxLength(120);
    b.Property(x => x.Documento).IsRequired().HasMaxLength(20);
    b.Property(x => x.DataNascimento).HasColumnType("date"); 
    b.Property(x => x.Telefone).HasMaxLength(20);
    b.Property(x => x.Email).HasMaxLength(180);
    b.Property(x => x.CriadoEm).HasDefaultValueSql("now()");
    b.Property(x => x.Ativo).HasDefaultValue(true);

    // Índices úteis para pesquisa
    b.HasIndex(x => new { x.CodEmpresa, x.Nome });
    b.HasIndex(x => new { x.CodEmpresa, x.Documento });
    // Se quiser documento único por empresa, troque por Unique:
    // b.HasIndex(x => new { x.CodEmpresa, x.Documento }).IsUnique();
  }
}
