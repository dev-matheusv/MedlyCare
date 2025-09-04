using FluentValidation;

namespace SFA.Application.Perfis;
public class PerfilCreateValidator : AbstractValidator<PerfilCreateDto>
{
  public PerfilCreateValidator() => RuleFor(x => x.Nome).NotEmpty().MaximumLength(80);
}
public class PerfilUpdateValidator : AbstractValidator<PerfilUpdateDto>
{
  public PerfilUpdateValidator() => RuleFor(x => x.Nome).NotEmpty().MaximumLength(80);
}
