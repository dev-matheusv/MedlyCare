using SFA.Domain.Enums;

namespace SFA.Domain.Entities;

public class Atestado
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
  public int DiasAfastamento { get; set; }
  public DateTime? DataInicioAfastamento { get; set; }

  public TipoAfastamento? TipoAfastamento { get; set; }

  public string? DescricaoCurta { get; set; }

  public bool InformarCid { get; set; }
  public string? Cid { get; set; }

  public string? LocalEmissao { get; set; }

  public string Crm { get; set; } = null!;
  public string AssinaturaNome { get; set; } = null!;

  public bool Cancelado { get; set; }
  public string? MotivoCancelamento { get; set; }

  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }
}
