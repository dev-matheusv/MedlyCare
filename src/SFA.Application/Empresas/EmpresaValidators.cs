using FluentValidation;

namespace SFA.Application.Empresas;

public class EmpresaCreateValidator : AbstractValidator<EmpresaCreateDto>
{
  public EmpresaCreateValidator()
  {
    RuleFor(x => x.Nome).NotEmpty().MaximumLength(120);
  }
}
public class EmpresaUpdateValidator : AbstractValidator<EmpresaUpdateDto>
{
  public EmpresaUpdateValidator()
  {
    RuleFor(x => x.Nome).NotEmpty().MaximumLength(120);
  }
}

