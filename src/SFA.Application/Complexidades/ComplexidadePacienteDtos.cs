namespace SFA.Application.Complexidades;

public record ComplexidadePacienteDto(
    Guid Id,
    Guid PacienteId,
    Guid ProfissionalId,
    string Nivel,
    string Cor,
    string? Observacoes,
    DateTime CriadoEm,
    Guid CriadoPorUsuarioId
);

public record ComplexidadePacienteCreateDto(
    Guid ProfissionalId,
    string Nivel,
    string Cor,
    string? Observacoes
);
