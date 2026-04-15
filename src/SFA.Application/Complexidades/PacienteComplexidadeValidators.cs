using FluentValidation;

namespace SFA.Application.Complexidades;

public class PacienteComplexidadeCreateValidator : AbstractValidator<PacienteComplexidadeCreateDto>
{
    public PacienteComplexidadeCreateValidator()
    {
        RuleFor(x => x.ComplexidadeId)
            .NotEmpty().WithMessage("complexidade_obrigatoria");

        RuleFor(x => x.UsuarioId)
            .NotEmpty().WithMessage("usuario_obrigatorio");
    }
}
