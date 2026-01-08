using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class ConfirmacaoAgendamentoConfiguration : IEntityTypeConfiguration<ConfirmacaoAgendamento>
{
  public void Configure(EntityTypeBuilder<ConfirmacaoAgendamento> builder)
  {
    builder.ToTable("confirmacao_agendamento");

    builder.HasKey(x => x.Id);

    builder.Property(x => x.Id)
      .HasColumnName("id")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()");

    builder.Property(x => x.CodEmpresa)
      .HasColumnName("cod_empresa")
      .IsRequired();

    builder.Property(x => x.AgendamentoId)
      .HasColumnName("agendamento_id")
      .HasColumnType("uuid")
      .IsRequired();

    builder.Property(x => x.Token)
      .HasColumnName("token")
      .HasColumnType("uuid")
      .HasDefaultValueSql("gen_random_uuid()")
      .IsRequired();

    builder.Property(x => x.Status)
      .HasColumnName("status")
      .HasConversion<int>()
      .HasDefaultValue(ConfirmacaoAgendamentoStatus.Pendente)
      .IsRequired();

    builder.Property(x => x.ExpiresAtUtc)
      .HasColumnName("expires_at_utc")
      .HasColumnType("timestamptz")
      .IsRequired();

    builder.Property(x => x.RespondidoEmUtc)
      .HasColumnName("respondido_em_utc")
      .HasColumnType("timestamptz");

    builder.Property(x => x.RespondidoIp)
      .HasColumnName("respondido_ip")
      .HasColumnType("text");

    builder.Property(x => x.UserAgent)
      .HasColumnName("user_agent")
      .HasColumnType("text");

    builder.HasIndex(x => x.Token).IsUnique();
    builder.HasIndex(x => x.AgendamentoId).IsUnique();
    builder.HasIndex(x => new { x.CodEmpresa, x.Status, x.ExpiresAtUtc });
  }
}
