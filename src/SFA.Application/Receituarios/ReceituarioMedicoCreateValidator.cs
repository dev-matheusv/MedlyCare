using FluentValidation;

namespace SFA.Application.Receituarios;

public class ReceituarioMedicoCreateValidator : AbstractValidator<ReceituarioMedicoCreateDto>
{
  public ReceituarioMedicoCreateValidator()
  {
    RuleFor(x => x.PacienteId)
      .NotEmpty();

    RuleFor(x => x.ProfissionalId)
      .NotEmpty();

    RuleFor(x => x.DataEmissao)
      .NotEmpty();

    RuleFor(x => x.Observacoes)
      .MaximumLength(1000);

    RuleFor(x => x.Itens)
      .NotEmpty()
      .WithMessage("receituario_deve_conter_ao_menos_um_item");

    RuleForEach(x => x.Itens).ChildRules(item =>
    {
      item.RuleFor(i => i.NomeMedicamento)
        .NotEmpty()
        .MaximumLength(200);

      item.RuleFor(i => i.FormaFarmaceutica)
        .MaximumLength(100);

      item.RuleFor(i => i.Concentracao)
        .MaximumLength(100);

      item.RuleFor(i => i.ViaAdministracao)
        .MaximumLength(100);

      item.RuleFor(i => i.Posologia)
        .MaximumLength(500);

      item.RuleFor(i => i.Orientacoes)
        .MaximumLength(500);
    });
  }
}
