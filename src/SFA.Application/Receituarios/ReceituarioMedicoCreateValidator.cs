using FluentValidation;
using SFA.Domain.Enums;

namespace SFA.Application.Receituarios;

public class ReceituarioMedicoCreateValidator : AbstractValidator<ReceituarioMedicoCreateDto>
{
  public ReceituarioMedicoCreateValidator()
  {
    RuleFor(x => x.PacienteId)
      .NotEmpty();

    RuleFor(x => x.ProfissionalId)
      .NotEmpty();

    RuleFor(x => x.TipoReceituario)
      .Must(v => Enum.IsDefined(typeof(TipoReceituario), v))
      .WithMessage("tipo_receituario_invalido");

    RuleFor(x => x.Diagnostico)
      .MaximumLength(1000)
      .When(x => x.Diagnostico is not null);

    RuleFor(x => x.Cid)
      .MaximumLength(10)
      .When(x => x.Cid is not null);

    RuleFor(x => x.Observacoes)
      .MaximumLength(2000)
      .When(x => x.Observacoes is not null);

    RuleFor(x => x.AssinaturaNome)
      .NotEmpty().WithMessage("assinatura_nome_obrigatorio")
      .MaximumLength(200);

    RuleFor(x => x.Itens)
      .NotEmpty()
      .WithMessage("receituario_deve_conter_ao_menos_um_item");

    RuleForEach(x => x.Itens).ChildRules(item =>
    {
      item.RuleFor(i => i.NomeMedicamento)
        .NotEmpty()
        .MaximumLength(300);

      item.RuleFor(i => i.FormaFarmaceutica)
        .MaximumLength(100)
        .When(i => i.FormaFarmaceutica is not null);

      item.RuleFor(i => i.Concentracao)
        .MaximumLength(100)
        .When(i => i.Concentracao is not null);

      item.RuleFor(i => i.ViaAdministracao)
        .MaximumLength(100)
        .When(i => i.ViaAdministracao is not null);

      item.RuleFor(i => i.Posologia)
        .MaximumLength(1000)
        .When(i => i.Posologia is not null);

      item.RuleFor(i => i.Quantidade)
        .NotEmpty().WithMessage("quantidade_obrigatoria")
        .MaximumLength(200);

      item.RuleFor(i => i.QuantidadeExtenso)
        .MaximumLength(200)
        .When(i => i.QuantidadeExtenso is not null);

      item.RuleFor(i => i.Orientacoes)
        .MaximumLength(1000)
        .When(i => i.Orientacoes is not null);
    });
  }
}
