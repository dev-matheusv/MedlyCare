using SFA.Domain.Enums;

namespace SFA.Domain.Entities;

public class AnexoPaciente
{
  public Guid Id { get; set; }

  public int CodEmpresa { get; set; }

  public Guid PacienteId { get; set; }
  public Paciente Paciente { get; set; } = null!;

  public TipoDocumentoPaciente TipoDocumento { get; set; }

  public string NomeArquivo { get; set; } = null!;
  public string NomeArmazenado { get; set; } = null!;
  public string ContentType { get; set; } = null!;
  public long TamanhoBytes { get; set; }
  public string HashSha256 { get; set; } = null!;
  public string UrlStorage { get; set; } = null!;

  public string? Descricao { get; set; }
  public DateTime? DataDocumento { get; set; }

  public Guid EnviadoPorId { get; set; }
  public Usuario EnviadoPor { get; set; } = null!;

  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }

  public bool IsDeleted { get; set; }
  public DateTimeOffset? DeletedAt { get; set; }
  public Guid? DeletedBy { get; set; }
  public string? DeletedReason { get; set; }
}
