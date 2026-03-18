namespace SFA.Domain.Entities;

public class ReceituarioMedico
{
  public Guid Id { get; set; }

  public int CodEmpresa { get; set; }

  public Guid PacienteId { get; set; }
  public Paciente Paciente { get; set; } = null!;

  public Guid ProfissionalId { get; set; }
  public Usuario Profissional { get; set; } = null!;

  public Guid? AtendimentoId { get; set; }
  public Atendimento? Atendimento { get; set; }

  public DateTime DataEmissao { get; set; }

  public string? Observacoes { get; set; }

  public bool Cancelado { get; set; }
  public string? MotivoCancelamento { get; set; }

  public ICollection<ReceituarioMedicoItem> Itens { get; set; } = new List<ReceituarioMedicoItem>();

  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }
}
