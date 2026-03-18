namespace SFA.Application.Empresas;

public record EmpresaCreateDto(
  string RazaoSocial,
  string Documento,
  string? InscricaoEstadualRg,
  string Email,
  string? Telefone,
  string? CelularWhatsapp,
  bool UtilizarCelularParaEnvioMensagens,
  string Endereco,
  int NumeroImovel,
  string Bairro,
  string Cidade,
  string Uf,
  string Cep,
  string Pais,
  string ResponsavelClinica,
  string? PathLogotipo,
  string? Cnae,
  string? RedesSociais,

  string NomeUsuarioAdmin,
  string LoginUsuarioAdmin,
  string EmailUsuarioAdmin,
  string? TelefoneUsuarioAdmin,
  string? CelularWhatsappUsuarioAdmin,
  string SenhaUsuarioAdmin,

  bool Ativa = true
);

public record EmpresaUpdateDto(
  string RazaoSocial,
  string Documento,
  string? InscricaoEstadualRg,
  string Email,
  string? Telefone,
  string? CelularWhatsapp,
  bool UtilizarCelularParaEnvioMensagens,
  string Endereco,
  int NumeroImovel,
  string Bairro,
  string Cidade,
  string Uf,
  string Cep,
  string Pais,
  string ResponsavelClinica,
  string? PathLogotipo,
  string? Cnae,
  string? RedesSociais,
  bool Ativa
);

public record EmpresaListItemDto(
  Guid Id,
  int CodEmpresa,
  string RazaoSocial,
  string Documento,
  string Email,
  string? Telefone,
  string? CelularWhatsapp,
  bool UtilizarCelularParaEnvioMensagens,
  string Cidade,
  string Uf,
  string ResponsavelClinica,
  bool Ativa,
  DateTime CriadoEm
);

public record EmpresaDetailsDto(
  Guid Id,
  int CodEmpresa,
  string RazaoSocial,
  string Documento,
  string? InscricaoEstadualRg,
  string Email,
  string? Telefone,
  string? CelularWhatsapp,
  bool UtilizarCelularParaEnvioMensagens,
  string Endereco,
  int NumeroImovel,
  string Bairro,
  string Cidade,
  string Uf,
  string Cep,
  string Pais,
  string ResponsavelClinica,
  string? PathLogotipo,
  string? Cnae,
  string? RedesSociais,
  bool Ativa,
  DateTime CriadoEm
);
