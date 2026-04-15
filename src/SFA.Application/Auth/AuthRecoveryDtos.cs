using System.ComponentModel.DataAnnotations;

namespace SFA.Application.Auth;

public record RecuperarAcessoRequest(
  int? CodEmpresa,
  [Required, EmailAddress, MaxLength(200)] string Email
);

public record RedefinirSenhaRequest(
  [Required, MaxLength(200)] string Token,
  [Required, MinLength(6), MaxLength(200)] string NovaSenha
);
