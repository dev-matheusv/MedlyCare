using FluentValidation;

namespace SFA.Application.Receituarios;

public class ReceituarioMedicoItemUpsertValidator : AbstractValidator<ReceituarioMedicoItemUpsertDto>
{
  public ReceituarioMedicoItemUpsertValidator()
  {
    RuleFor(x => x.NomeMedicamento)
      .NotEmpty().WithMessage("nome_medicamento_obrigatorio")
      .MaximumLength(300);

    RuleFor(x => x.FormaFarmaceutica)
      .MaximumLength(100)
      .When(x => x.FormaFarmaceutica is not null);

    RuleFor(x => x.Concentracao)
      .MaximumLength(100)
      .When(x => x.Concentracao is not null);

    RuleFor(x => x.ViaAdministracao)
      .MaximumLength(100)
      .When(x => x.ViaAdministracao is not null);

    RuleFor(x => x.Posologia)
      .MaximumLength(1000)
      .When(x => x.Posologia is not null);

    RuleFor(x => x.Quantidade)
      .NotEmpty().WithMessage("quantidade_obrigatoria")
      .MaximumLength(200);

    RuleFor(x => x.QuantidadeExtenso)
      .MaximumLength(200)
      .When(x => x.QuantidadeExtenso is not null);

    RuleFor(x => x.Orientacoes)
      .MaximumLength(1000)
      .When(x => x.Orientacoes is not null);
  }
}
