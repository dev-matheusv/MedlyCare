namespace SFA.Domain.Entities;

public class Atendimento
{
  public Guid Id { get; set; }  // uuid gerado no DB
  public int CodEmpresa { get; set; }

  public Guid PacienteId { get; set; }
  public Paciente Paciente { get; set; } = null!;

  public Guid ProfissionalId { get; set; }
  public Usuario Profissional { get; set; } = null!;

  // Encaixe: pode ser null
  public Guid? AgendamentoId { get; set; }
  public Agendamento? Agendamento { get; set; }

  public AtendimentoStatus Status { get; set; } = AtendimentoStatus.Aberto;

  public DateTimeOffset InicioUtc { get; set; }
  public DateTimeOffset? FinalizadoUtc { get; set; }

  public string? Observacoes { get; set; }

  // Auditoria
  public Guid CriadoPorUsuarioId { get; set; }
  public DateTime CriadoEm { get; set; }            // DB preenche (utc)
  public Guid? AlteradoPorUsuarioId { get; set; }
  public DateTime? AlteradoEm { get; set; }

  // Soft delete
  public bool IsDeleted { get; set; }
  public DateTimeOffset? DeletedAt { get; set; }
  public Guid? DeletedBy { get; set; }
  public string? DeletedReason { get; set; }
}

public enum AtendimentoStatus
{
  Aberto = 1,
  Finalizado = 2,
  Cancelado = 3
}
