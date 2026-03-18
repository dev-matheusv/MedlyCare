using FluentValidation;
using SFA.Domain.Enums;

namespace SFA.Application.Atestados;

public class AtestadoCreateValidator : AbstractValidator<AtestadoCreateDto>
{
  public AtestadoCreateValidator()
  {
    RuleFor(x => x.PacienteId)
      .NotEmpty();

    RuleFor(x => x.ProfissionalId)
      .NotEmpty();

    RuleFor(x => x.DataEmissao)
      .NotEmpty();

    RuleFor(x => x.DiasAfastamento)
      .GreaterThan(0);

    RuleFor(x => x.TipoAfastamento)
      .Must(x => x is null || Enum.IsDefined(typeof(TipoAfastamento), x.Value))
      .WithMessage("tipo_afastamento_invalido");

    RuleFor(x => x.DescricaoCurta)
      .MaximumLength(500);

    RuleFor(x => x.Cid)
      .MaximumLength(10);

    RuleFor(x => x.LocalEmissao)
      .MaximumLength(200);

    RuleFor(x => x.Crm)
      .NotEmpty()
      .MaximumLength(20);

    RuleFor(x => x.AssinaturaNome)
      .NotEmpty()
      .MaximumLength(200);

    When(x => x.InformarCid, () =>
    {
      RuleFor(x => x.Cid)
        .NotEmpty()
        .WithMessage("cid_obrigatorio_quando_informado");
    });

    When(x => !x.InformarCid, () =>
    {
      RuleFor(x => x.Cid)
        .Must(string.IsNullOrWhiteSpace)
        .WithMessage("cid_deve_ser_nulo_quando_nao_autorizado");
    });
  }
}
