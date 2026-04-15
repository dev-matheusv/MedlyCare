using FluentValidation;

namespace SFA.Application.Complexidades;

public class ComplexidadeCreateValidator : AbstractValidator<ComplexidadeCreateDto>
{
    public ComplexidadeCreateValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("descricao_obrigatoria")
            .MaximumLength(300);

        RuleFor(x => x.Cor)
            .NotEmpty().WithMessage("cor_obrigatoria")
            .MaximumLength(20);
    }
}

public class ComplexidadeUpdateValidator : AbstractValidator<ComplexidadeUpdateDto>
{
    public ComplexidadeUpdateValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("descricao_obrigatoria")
            .MaximumLength(300);

        RuleFor(x => x.Cor)
            .NotEmpty().WithMessage("cor_obrigatoria")
            .MaximumLength(20);
    }
}
