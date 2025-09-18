namespace SFA.Domain.Entities;

public class Agendamento
{
  public int Id { get; set; }
  public int CodEmpresa { get; set; }

  public int PacienteId { get; set; }
  public Paciente Paciente { get; set; } = null!;

  public int ProfissionalId { get; set; }
  public Usuario Profissional { get; set; } = null!;

  public DateTimeOffset InicioUtc { get; set; }
  public DateTimeOffset FimUtc { get; set; }

  public string Status { get; set; } = "agendado"; // agendado|confirmado|cancelado
  public string? Observacoes { get; set; }

  // Auditoria
  public int CriadoPorUsuarioId { get; set; }
  public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
  public int? AlteradoPorUsuarioId { get; set; }
  public DateTime? AlteradoEm { get; set; }
}
