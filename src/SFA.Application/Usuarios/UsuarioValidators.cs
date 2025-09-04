using FluentValidation;

namespace SFA.Application.Usuarios;

public class UsuarioCreateValidator : AbstractValidator<UsuarioCreateDto>
{
  public UsuarioCreateValidator()
  {
    RuleFor(x => x.Login).NotEmpty().MaximumLength(120).EmailAddress();
    RuleFor(x => x.Nome).NotEmpty().MaximumLength(120);
    RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
  }
}

public class UsuarioUpdateValidator : AbstractValidator<UsuarioUpdateDto>
{
  public UsuarioUpdateValidator()
  {
    RuleFor(x => x.Nome).NotEmpty().MaximumLength(120);
    RuleFor(x => x.Password)
      .MinimumLength(6).When(x => !string.IsNullOrWhiteSpace(x.Password));
  }
}
