namespace SFA.Application.Receituarios;

public record ReceituarioMedicoItemDto(
  Guid Id,
  string NomeMedicamento,
  string? FormaFarmaceutica,
  string? Concentracao,
  string? ViaAdministracao,
  string? Posologia,
  string Quantidade,
  string? QuantidadeExtenso,
  string? Orientacoes
);

public record ReceituarioMedicoCreateItemDto(
  string NomeMedicamento,
  string? FormaFarmaceutica,
  string? Concentracao,
  string? ViaAdministracao,
  string? Posologia,
  string Quantidade,
  string? QuantidadeExtenso,
  string? Orientacoes
);

public record ReceituarioMedicoItemUpsertDto(
  string NomeMedicamento,
  string? FormaFarmaceutica,
  string? Concentracao,
  string? ViaAdministracao,
  string? Posologia,
  string Quantidade,
  string? QuantidadeExtenso,
  string? Orientacoes
);

public record ReceituarioMedicoListItemDto(
  Guid Id,
  Guid PacienteId,
  string NomePaciente,
  Guid ProfissionalId,
  string NomeProfissional,
  Guid? AtendimentoId,
  int TipoReceituario,
  DateTime DataEmissao,
  string AssinaturaNome,
  string? RegistroProfissional,
  bool Cancelado,
  DateTime CriadoEm
);

public record ReceituarioMedicoDetailsDto(
  Guid Id,
  int CodEmpresa,
  Guid PacienteId,
  string NomePaciente,
  Guid ProfissionalId,
  string NomeProfissional,
  Guid? AtendimentoId,
  int TipoReceituario,
  DateTime DataEmissao,
  string? Diagnostico,
  bool InformarCid,
  string? Cid,
  string? Observacoes,
  string AssinaturaNome,
  string? RegistroProfissional,
  string? EnderecoProfissional,
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
  int TipoReceituario,
  string? Diagnostico,
  bool InformarCid,
  string? Cid,
  string? Observacoes,
  string AssinaturaNome,
  List<ReceituarioMedicoCreateItemDto> Itens
);

public record ReceituarioMedicoCancelDto(
  string MotivoCancelamento
);
