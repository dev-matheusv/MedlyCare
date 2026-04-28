using SFA.Domain.Enums;

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

  public TipoReceituario TipoReceituario { get; set; }

  public DateTime DataEmissao { get; set; }

  public string? Diagnostico { get; set; }

  public bool InformarCid { get; set; }
  public string? Cid { get; set; }

  public string? Observacoes { get; set; }

  public string AssinaturaNome { get; set; } = null!;
  public string? RegistroProfissional { get; set; }   // CRM/CRO/COREN etc — preenchido automaticamente do usuário
  public string? EnderecoProfissional { get; set; }   // Cidade/UF da empresa — preenchido automaticamente

  public bool Cancelado { get; set; }
  public string? MotivoCancelamento { get; set; }

  public ICollection<ReceituarioMedicoItem> Itens { get; set; } = new List<ReceituarioMedicoItem>();

  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }
}
