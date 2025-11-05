namespace SFA.Application.Perfis;
public record PerfilDto(Guid Id, int CodEmpresa, string Nome, bool Ativo, DateTime CriadoEm);
public record PerfilCreateDto(string Nome, bool Ativo = true);
public record PerfilUpdateDto(string Nome, bool Ativo);
