using FluentValidation;

namespace SFA.Application.Agendamentos;

public class AgendamentoCreateValidator : AbstractValidator<AgendamentoCreateDto>
{
  public AgendamentoCreateValidator()
  {
    RuleFor(x => x.PacienteId).GreaterThan(Guid.Empty);
    RuleFor(x => x.ProfissionalId).GreaterThan(Guid.Empty);
    RuleFor(x => x.InicioUtc).NotEmpty();
    RuleFor(x => x.FimUtc).NotEmpty()
      .Must((dto, fim) => fim > dto.InicioUtc)
      .WithMessage("FimUtc deve ser maior que InicioUtc");
    RuleFor(x => x)
      .Must(x => (x.FimUtc - x.InicioUtc).TotalMinutes >= 10)
      .WithMessage("Duração mínima de 10 minutos");
  }
}

public class AgendamentoUpdateValidator : AbstractValidator<AgendamentoUpdateDto>
{
  private static readonly string[] Allowed = ["agendado", "confirmado", "cancelado"];

  public AgendamentoUpdateValidator()
  {
    RuleFor(x => x.InicioUtc).NotEmpty();
    RuleFor(x => x.FimUtc).NotEmpty()
      .Must((dto, fim) => fim > dto.InicioUtc)
      .WithMessage("FimUtc deve ser maior que InicioUtc");
    RuleFor(x => x)
      .Must(x => (x.FimUtc - x.InicioUtc).TotalMinutes >= 10)
      .WithMessage("Duração mínima de 10 minutos");
    RuleFor(x => x.Status)
      .NotEmpty()
      .Must(s => Allowed.Contains(s))
      .WithMessage("Status inválido (use: agendado|confirmado|cancelado)");
  }
}
