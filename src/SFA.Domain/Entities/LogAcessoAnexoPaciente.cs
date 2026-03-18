using SFA.Domain.Enums;

namespace SFA.Domain.Entities;

public class LogAcessoAnexoPaciente
{
  public Guid Id { get; set; }

  public int CodEmpresa { get; set; }

  public Guid AnexoPacienteId { get; set; }
  public AnexoPaciente AnexoPaciente { get; set; } = null!;

  public Guid UsuarioId { get; set; }
  public Usuario Usuario { get; set; } = null!;

  public AcaoAcessoAnexoPaciente Acao { get; set; }

  public string? Ip { get; set; }
  public DateTime DataHora { get; set; }
}
