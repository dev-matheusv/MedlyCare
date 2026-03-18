namespace SFA.Application.AnexosPaciente;

public record AnexoPacienteListItemDto(
  Guid Id,
  Guid PacienteId,
  int TipoDocumento,
  string TipoDocumentoDescricao,
  string NomeArquivo,
  string ContentType,
  long TamanhoBytes,
  string? Descricao,
  DateTime? DataDocumento,
  Guid EnviadoPorId,
  DateTime CriadoEm
);

public record AnexoPacienteDetailsDto(
  Guid Id,
  Guid PacienteId,
  int CodEmpresa,
  int TipoDocumento,
  string TipoDocumentoDescricao,
  string NomeArquivo,
  string NomeArmazenado,
  string ContentType,
  long TamanhoBytes,
  string HashSha256,
  string UrlStorage,
  string? Descricao,
  DateTime? DataDocumento,
  Guid EnviadoPorId,
  DateTime CriadoEm,
  DateTime? AtualizadoEm
);

public record AnexoPacienteUploadDto(
  int TipoDocumento,
  string? Descricao,
  DateTime? DataDocumento
);

public record AnexoPacienteDeleteDto(
  string Motivo
);
