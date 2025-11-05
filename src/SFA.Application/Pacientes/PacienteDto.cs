namespace SFA.Application.Pacientes;

public record PacienteListItemDto(
  Guid Id, string Nome, string Documento, bool Ativo, DateTime CriadoEm,
  string? Telefone, string? Email, DateOnly? DataNascimento
);

public record PacienteCreateDto(
  string Nome,
  string Documento,
  DateOnly? DataNascimento,
  string? Telefone,
  string? Email,
  bool Ativo = true
);

public record PacienteUpdateDto(
  string Nome,
  string Documento,
  DateOnly? DataNascimento,
  string? Telefone,
  string? Email,
  bool Ativo
);
