namespace SFA.Application.Usuarios;

public record UsuarioListItemDto(int Id, int CodEmpresa, string Login, string Nome, bool Ativo, DateTime CriadoEm);

public record UsuarioCreateDto(
  string Login,
  string Nome,
  string Password,     // plaintext (será hasheada no banco)
  bool Ativo = true
);

public record UsuarioUpdateDto(
  string Nome,
  string? Password,    // opcional: se enviar, re-hasha
  bool Ativo
);
