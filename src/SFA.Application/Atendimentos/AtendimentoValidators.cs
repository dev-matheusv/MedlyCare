using FluentValidation;

namespace SFA.Application.Atendimentos;

public class AtendimentoCreateDtoValidator : AbstractValidator<AtendimentoCreateDto>
{
  public AtendimentoCreateDtoValidator()
  {
    RuleFor(x => x.PacienteId).NotEmpty();
    RuleFor(x => x.ProfissionalId).NotEmpty();
    RuleFor(x => x.Observacoes).MaximumLength(5000);
  }
}

public class AtendimentoUpdateDtoValidator : AbstractValidator<AtendimentoUpdateDto>
{
  public AtendimentoUpdateDtoValidator()
  {
    RuleFor(x => x.Observacoes).MaximumLength(5000);
  }
}
