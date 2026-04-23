namespace SFA.Application.Atestados;

public record AtestadoListItemDto(
  Guid Id,
  Guid PacienteId,
  string NomePaciente,
  Guid ProfissionalId,
  string NomeProfissional,
  Guid? AtendimentoId,
  DateTime DataEmissao,
  int DiasAfastamento,
  TimeSpan? HoraInicio,
  TimeSpan? HoraFim,
  DateTime? DataInicioAfastamento,
  int? TipoAfastamento,
  string? DescricaoCurta,
  bool InformarCid,
  string? Cid,
  string? LocalEmissao,
  string? Crm,
  string AssinaturaNome,
  bool Cancelado,
  DateTime CriadoEm
);

public record AtestadoDetailsDto(
  Guid Id,
  int CodEmpresa,
  Guid PacienteId,
  string NomePaciente,
  Guid ProfissionalId,
  string NomeProfissional,
  Guid? AtendimentoId,
  DateTime DataEmissao,
  int DiasAfastamento,
  TimeSpan? HoraInicio,
  TimeSpan? HoraFim,
  DateTime? DataInicioAfastamento,
  int? TipoAfastamento,
  string? DescricaoCurta,
  bool InformarCid,
  string? Cid,
  string? LocalEmissao,
  string? Crm,
  string AssinaturaNome,
  bool Cancelado,
  string? MotivoCancelamento,
  DateTime CriadoEm,
  DateTime? AtualizadoEm
);

public record AtestadoCreateDto(
  Guid PacienteId,
  Guid ProfissionalId,
  Guid? AtendimentoId,
  int DiasAfastamento,
  TimeSpan? HoraInicio,
  TimeSpan? HoraFim,
  DateTime? DataInicioAfastamento,
  int? TipoAfastamento,
  string? DescricaoCurta,
  bool InformarCid,
  string? Cid,
  string AssinaturaNome
);

public record AtestadoCancelDto(
  string MotivoCancelamento
);
