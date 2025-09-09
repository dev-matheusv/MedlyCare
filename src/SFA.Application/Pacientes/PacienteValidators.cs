using FluentValidation;

namespace SFA.Application.Pacientes;

public class PacienteCreateValidator : AbstractValidator<PacienteCreateDto>
{
  public PacienteCreateValidator()
  {
    RuleFor(x => x.Nome).NotEmpty().MaximumLength(120);
    RuleFor(x => x.Documento).NotEmpty().MaximumLength(20);
    RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    RuleFor(x => x.Telefone).MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.Telefone));
  }
}

public class PacienteUpdateValidator : AbstractValidator<PacienteUpdateDto>
{
  public PacienteUpdateValidator()
  {
    RuleFor(x => x.Nome).NotEmpty().MaximumLength(120);
    RuleFor(x => x.Documento).NotEmpty().MaximumLength(20);
    RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    RuleFor(x => x.Telefone).MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.Telefone));
  }
}
