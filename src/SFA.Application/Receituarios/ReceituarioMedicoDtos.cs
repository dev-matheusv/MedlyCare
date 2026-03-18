namespace SFA.Application.Receituarios;

public record ReceituarioMedicoItemDto(
  Guid Id,
  string NomeMedicamento,
  string? FormaFarmaceutica,
  string? Concentracao,
  string? ViaAdministracao,
  string? Posologia,
  string? Orientacoes
);

public record ReceituarioMedicoCreateItemDto(
  string NomeMedicamento,
  string? FormaFarmaceutica,
  string? Concentracao,
  string? ViaAdministracao,
  string? Posologia,
  string? Orientacoes
);

public record ReceituarioMedicoListItemDto(
  Guid Id,
  Guid PacienteId,
  Guid ProfissionalId,
  Guid? AtendimentoId,
  DateTime DataEmissao,
  string? Observacoes,
  bool Cancelado,
  DateTime CriadoEm
);

public record ReceituarioMedicoDetailsDto(
  Guid Id,
  int CodEmpresa,
  Guid PacienteId,
  Guid ProfissionalId,
  Guid? AtendimentoId,
  DateTime DataEmissao,
  string? Observacoes,
  bool Cancelado,
  string? MotivoCancelamento,
  DateTime CriadoEm,
  DateTime? AtualizadoEm,
  List<ReceituarioMedicoItemDto> Itens
);

public record ReceituarioMedicoCreateDto(
  Guid PacienteId,
  Guid ProfissionalId,
  Guid? AtendimentoId,
  DateTime DataEmissao,
  string? Observacoes,
  List<ReceituarioMedicoCreateItemDto> Itens
);

public record ReceituarioMedicoCancelDto(
  string MotivoCancelamento
);
