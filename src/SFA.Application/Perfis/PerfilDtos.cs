namespace SFA.Application.Perfis;
public record PerfilDto(int Id, int CodEmpresa, string Nome, bool Ativo, DateTime CriadoEm);
public record PerfilCreateDto(string Nome, bool Ativo = true);
public record PerfilUpdateDto(string Nome, bool Ativo);
