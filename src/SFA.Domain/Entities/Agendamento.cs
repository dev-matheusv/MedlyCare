namespace SFA.Domain.Entities;

public class Agendamento
{
  public Guid Id { get; set; }                      // antes: int
  public int CodEmpresa { get; set; }               // mantém escopo multi-empresa via claim

  public Guid PacienteId { get; set; }              // antes: int
  public Paciente Paciente { get; set; } = null!;

  public Guid ProfissionalId { get; set; }          // antes: int
  public Usuario Profissional { get; set; } = null!;

  public DateTimeOffset InicioUtc { get; set; }
  public DateTimeOffset FimUtc { get; set; }

  public string Status { get; set; } = "agendado";
  public string? Observacoes { get; set; }

  // Auditoria
  public Guid CriadoPorUsuarioId { get; set; }      // antes: int
  public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
  public Guid? AlteradoPorUsuarioId { get; set; }   // antes: int?
  public DateTime? AlteradoEm { get; set; }

  // Soft delete
  public bool IsDeleted { get; set; }
  public DateTimeOffset? DeletedAt { get; set; }
  public Guid? DeletedBy { get; set; }              // antes: int?
  public string? DeletedReason { get; set; }
}
