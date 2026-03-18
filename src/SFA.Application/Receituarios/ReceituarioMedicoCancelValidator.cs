using FluentValidation;

namespace SFA.Application.Receituarios;

public class ReceituarioMedicoCancelValidator : AbstractValidator<ReceituarioMedicoCancelDto>
{
  public ReceituarioMedicoCancelValidator()
  {
    RuleFor(x => x.MotivoCancelamento)
      .NotEmpty()
      .WithMessage("motivo_cancelamento_obrigatorio")
      .MaximumLength(500);
  }
}
