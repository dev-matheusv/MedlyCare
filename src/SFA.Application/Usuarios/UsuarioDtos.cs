namespace SFA.Application.Usuarios;

public record UsuarioListItemDto(
  Guid Id,
  int CodEmpresa,
  string Login,
  string Nome,
  string Email,
  string? Telefone,
  string? CelularWhatsapp,
  string? Crm,
  bool Ativo,
  DateTime CriadoEm
);

public record UsuarioCreateDto(
  string Login,
  string Nome,
  string Email,
  string? Telefone,
  string? CelularWhatsapp,
  string? Crm,
  string Password,
  bool Ativo = true
);

public record UsuarioUpdateDto(
  string Nome,
  string Email,
  string? Telefone,
  string? CelularWhatsapp,
  string? Crm,
  string? Password,
  bool Ativo
);
