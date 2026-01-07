namespace SFA.Application.Atendimentos;

public record PessoaDto(Guid Id, string Nome);

public record AtendimentoListItemDto(
  Guid Id,
  PessoaDto Paciente,
  PessoaDto Profissional,
  DateTimeOffset InicioUtc,
  DateTimeOffset? FinalizadoUtc,
  string Status,
  string? Observacoes
);

public record AtendimentoGetDto(
  Guid Id,
  Guid PacienteId,
  Guid ProfissionalId,
  Guid? AgendamentoId,
  string Status,
  DateTimeOffset InicioUtc,
  DateTimeOffset? FinalizadoUtc,
  string? Observacoes
);

public record AtendimentoCreateDto(
  Guid PacienteId,
  Guid ProfissionalId,
  Guid? AgendamentoId,
  DateTimeOffset? InicioUtc,
  string? Observacoes
);

public record AtendimentoUpdateDto(
  string? Observacoes
);

public record AtendimentoDetailsDto(
  Guid Id,
  PessoaDto Paciente,
  PessoaDto Profissional,
  Guid? AgendamentoId,
  string Status,
  DateTimeOffset InicioUtc,
  DateTimeOffset? FinalizadoUtc,
  string? Observacoes
);

public record AtendimentoEventDto(
  Guid Id,
  string Title,
  DateTimeOffset Start,
  DateTimeOffset? End,
  string Status,
  string Color,
  Guid PacienteId,
  Guid ProfissionalId,
  Guid? AgendamentoId
);
