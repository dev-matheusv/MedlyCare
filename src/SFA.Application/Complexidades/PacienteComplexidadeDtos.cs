namespace SFA.Application.Complexidades;

public record PacienteComplexidadeDto(
    Guid Id,
    Guid PacienteId,
    Guid ComplexidadeId,
    string ComplexidadeDescricao,
    string ComplexidadeCor,
    Guid UsuarioId,
    Guid? AtendimentoId,
    DateTime Data
);

public record PacienteComplexidadeCreateDto(
    Guid ComplexidadeId,
    Guid UsuarioId,
    Guid? AtendimentoId,
    DateTime? Data
);
