using FluentValidation;

namespace SFA.Application.Atestados;

public class AtestadoCancelValidator : AbstractValidator<AtestadoCancelDto>
{
  public AtestadoCancelValidator()
  {
    RuleFor(x => x.MotivoCancelamento)
      .NotEmpty()
      .WithMessage("motivo_cancelamento_obrigatorio")
      .MaximumLength(500);
  }
}
