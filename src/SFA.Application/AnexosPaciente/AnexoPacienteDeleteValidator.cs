using FluentValidation;

namespace SFA.Application.AnexosPaciente;

public class AnexoPacienteDeleteValidator : AbstractValidator<AnexoPacienteDeleteDto>
{
  public AnexoPacienteDeleteValidator()
  {
    RuleFor(x => x.Motivo)
      .NotEmpty()
      .WithMessage("motivo_obrigatorio")
      .MaximumLength(500);
  }
}
