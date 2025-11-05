namespace SFA.Application.Agendamentos;

public record PessoaDto(
  int Id,
  string Nome
);

public record AgendamentoListItemDto(
  int Id,
  PessoaDto Paciente,
  PessoaDto Profissional,
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