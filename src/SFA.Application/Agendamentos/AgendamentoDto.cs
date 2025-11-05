namespace SFA.Application.Agendamentos;

public record PessoaDto(Guid Id, string Nome); // antes: int

public record AgendamentoListItemDto(
  Guid Id, // antes: int
  PessoaDto Paciente,
  PessoaDto Profissional,
  DateTimeOffset InicioUtc,
  DateTimeOffset FimUtc,
  string Status,
  string? Observacoes
);

public record AgendamentoCreateDto(
  Guid PacienteId, // antes: int
  Guid ProfissionalId, // antes: int
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
