namespace SFA.Application.Complexidades;

public record ComplexidadeListItemDto(
    Guid Id,
    string Descricao,
    string Cor,
    bool Ativo
);

public record ComplexidadeDto(
    Guid Id,
    int CodEmpresa,
    string Descricao,
    string Cor,
    bool Ativo,
    DateTime CriadoEm,
    DateTime AtualizadoEm
);

public record ComplexidadeCreateDto(
    string Descricao,
    string Cor
);

public record ComplexidadeUpdateDto(
    string Descricao,
    string Cor,
    bool Ativo
);
