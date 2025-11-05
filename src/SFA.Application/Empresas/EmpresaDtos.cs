namespace SFA.Application.Empresas;

public record EmpresaCreateDto(string Nome, bool Ativa = true);
public record EmpresaUpdateDto(string Nome, bool Ativa);
public record EmpresaListItemDto(Guid Id, int CodEmpresa, string Nome, bool Ativa, DateTime CriadoEm); // antes: ID int - agora Id Guid e CodEmpresa int
