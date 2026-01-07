using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.Domain.Entities;

namespace SFA.Infrastructure.Configurations;

public class AtendimentoConfiguration : IEntityTypeConfiguration<Atendimento>
{
    public void Configure(EntityTypeBuilder<Atendimento> builder)
    {
        builder.ToTable("atendimento");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.CodEmpresa)
            .HasColumnName("cod_empresa")
            .IsRequired();

        builder.Property(a => a.PacienteId)
            .HasColumnName("paciente_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.ProfissionalId)
            .HasColumnName("profissional_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.AgendamentoId)
            .HasColumnName("agendamento_id")
            .HasColumnType("uuid");

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.InicioUtc)
            .HasColumnName("inicio_utc")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(a => a.FinalizadoUtc)
            .HasColumnName("finalizado_utc")
            .HasColumnType("timestamptz");

        builder.Property(a => a.Observacoes)
            .HasColumnName("observacoes")
            .HasColumnType("text");

        // Auditoria (igual ao Agendamento)
        builder.Property(a => a.CriadoPorUsuarioId)
            .HasColumnName("criado_por_usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(a => a.AlteradoPorUsuarioId)
            .HasColumnName("alterado_por_usuario_id")
            .HasColumnType("uuid");

        builder.Property(a => a.AlteradoEm)
            .HasColumnName("alterado_em");

        // Soft delete (igual ao Agendamento)
        builder.Property(a => a.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(a => a.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(a => a.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid");

        builder.Property(a => a.DeletedReason)
            .HasColumnName("deleted_reason")
            .HasColumnType("text");

        builder.HasOne(a => a.Paciente)
            .WithMany()
            .HasForeignKey(a => a.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Profissional)
            .WithMany()
            .HasForeignKey(a => a.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 quando houver agendamento
        builder.HasOne(a => a.Agendamento)
            .WithOne()
            .HasForeignKey<Atendimento>(a => a.AgendamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.CodEmpresa, a.ProfissionalId, a.InicioUtc });
        builder.HasIndex(a => new { a.CodEmpresa, a.PacienteId, a.InicioUtc });

        // Garantia 1:1 para agendamento (por empresa) quando AgendamentoId não for nulo
        builder.HasIndex(a => new { a.CodEmpresa, a.AgendamentoId })
            .IsUnique()
            .HasFilter("\"agendamento_id\" IS NOT NULL");
    }
}
