namespace SFA.Application.Agendamentos;

public record AgendamentoListItemDto(
  int Id,
  int PacienteId,
  int ProfissionalId,
  DateTimeOffset InicioUtc,
  DateTimeOffset FimUtc,
  string Status,
  string? Observacoes
);

public record AgendamentoCreateDto(
  int PacienteId,
  int ProfissionalId,
  DateTimeOffset InicioUtc,
  DateTimeOffset FimUtc,
  string? Observacoes
);

public record AgendamentoUpdateDto(
  DateTimeOffset InicioUtc,
  DateTimeOffset FimUtc,
  string Status,
  string? Observacoes
);
