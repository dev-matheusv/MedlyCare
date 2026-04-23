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

    // DiasAfastamento >= 0; se 0, HoraInicio e HoraFim devem ser informadas
    RuleFor(x => x.DiasAfastamento)
      .GreaterThanOrEqualTo(0);

    RuleFor(x => x)
      .Must(x => x.DiasAfastamento > 0 || (x.HoraInicio.HasValue && x.HoraFim.HasValue))
      .WithName("DiasAfastamento")
      .WithMessage("dias_afastamento_obrigatorio_ou_informar_horario");

    // Se HoraInicio ou HoraFim forem informadas, ambas são obrigatórias e HoraFim > HoraInicio
    When(x => x.HoraInicio.HasValue || x.HoraFim.HasValue, () =>
    {
      RuleFor(x => x.HoraInicio)
        .NotNull()
        .WithMessage("hora_inicio_obrigatoria_quando_horario_informado");

      RuleFor(x => x.HoraFim)
        .NotNull()
        .WithMessage("hora_fim_obrigatoria_quando_horario_informado");

      RuleFor(x => x)
        .Must(x => !x.HoraInicio.HasValue || !x.HoraFim.HasValue || x.HoraFim > x.HoraInicio)
        .WithName("HoraFim")
        .WithMessage("hora_fim_deve_ser_maior_que_hora_inicio");
    });

    RuleFor(x => x.TipoAfastamento)
      .Must(x => x is null || Enum.IsDefined(typeof(TipoAfastamento), x.Value))
      .WithMessage("tipo_afastamento_invalido");

    RuleFor(x => x.DescricaoCurta)
      .MaximumLength(500);

    RuleFor(x => x.Cid)
      .MaximumLength(10);

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
