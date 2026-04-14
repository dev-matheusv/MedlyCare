using FluentValidation;

namespace SFA.Application.Complexidades;

public class ComplexidadePacienteCreateValidator : AbstractValidator<ComplexidadePacienteCreateDto>
{
    private static readonly string[] NiveisValidos = ["baixa", "media", "alta"];

    public ComplexidadePacienteCreateValidator()
    {
        RuleFor(x => x.ProfissionalId)
            .NotEmpty().WithMessage("profissional_obrigatorio");

        RuleFor(x => x.Nivel)
            .NotEmpty().WithMessage("nivel_obrigatorio")
            .Must(n => NiveisValidos.Contains(n?.ToLowerInvariant()))
            .WithMessage("nivel_invalido. Valores aceitos: baixa, media, alta");

        RuleFor(x => x.Cor)
            .NotEmpty().WithMessage("cor_obrigatoria")
            .MaximumLength(20).WithMessage("cor_muito_longa");

        RuleFor(x => x.Observacoes)
            .MaximumLength(500).WithMessage("observacoes_muito_longa")
            .When(x => x.Observacoes is not null);
    }
}
