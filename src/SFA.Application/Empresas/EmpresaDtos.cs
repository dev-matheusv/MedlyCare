namespace SFA.Application.Empresas;

public record EmpresaCreateDto(string Nome, bool Ativa = true);
public record EmpresaUpdateDto(string Nome, bool Ativa);
public record EmpresaListItemDto(int Id, string Nome, bool Ativa, DateTime CriadoEm);
