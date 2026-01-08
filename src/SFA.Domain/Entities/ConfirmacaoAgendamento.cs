namespace SFA.Domain.Entities;

public class ConfirmacaoAgendamento
{
  public Guid Id { get; set; }
  public int CodEmpresa { get; set; }

  public Guid AgendamentoId { get; set; }
  // REMOVE isso:
  // public Agendamento? Agendamento { get; set; }

  public Guid Token { get; set; }

  public ConfirmacaoAgendamentoStatus Status { get; set; } = ConfirmacaoAgendamentoStatus.Pendente;

  public DateTimeOffset ExpiresAtUtc { get; set; }

  public DateTimeOffset? RespondidoEmUtc { get; set; }
  public string? RespondidoIp { get; set; }
  public string? UserAgent { get; set; }
}

public enum ConfirmacaoAgendamentoStatus
{
  Pendente = 0,
  Confirmado = 1,
  Recusado = 2
}
