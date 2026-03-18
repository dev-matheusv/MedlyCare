using FluentValidation;
using SFA.Domain.Enums;

namespace SFA.Application.AnexosPaciente;

public class AnexoPacienteUploadValidator : AbstractValidator<AnexoPacienteUploadDto>
{
  public AnexoPacienteUploadValidator()
  {
    RuleFor(x => x.TipoDocumento)
      .Must(x => Enum.IsDefined(typeof(TipoDocumentoPaciente), x))
      .WithMessage("tipo_documento_invalido");

    RuleFor(x => x.Descricao)
      .MaximumLength(1000);

    RuleFor(x => x.DataDocumento)
      .Must(data => !data.HasValue || data.Value.Date <= DateTime.UtcNow.Date)
      .WithMessage("data_documento_invalida");
  }
}
