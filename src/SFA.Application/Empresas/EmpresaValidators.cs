using FluentValidation;

namespace SFA.Application.Empresas;

public class EmpresaCreateValidator : AbstractValidator<EmpresaCreateDto>
{
    public EmpresaCreateValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Documento).NotEmpty().MaximumLength(20);
        RuleFor(x => x.InscricaoEstadualRg).MaximumLength(30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Telefone).MaximumLength(20);
        RuleFor(x => x.CelularWhatsapp).MaximumLength(20);
        RuleFor(x => x.Endereco).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NumeroImovel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Bairro).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Uf).NotEmpty().Length(2);
        RuleFor(x => x.Cep).NotEmpty().MaximumLength(12);
        RuleFor(x => x.Pais).NotEmpty().MaximumLength(60);
        RuleFor(x => x.ResponsavelClinica).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PathLogotipo).MaximumLength(500);
        RuleFor(x => x.Cnae).MaximumLength(20);
        RuleFor(x => x.RedesSociais).MaximumLength(500);
    }
}

public class EmpresaUpdateValidator : AbstractValidator<EmpresaUpdateDto>
{
    public EmpresaUpdateValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Documento).NotEmpty().MaximumLength(20);
        RuleFor(x => x.InscricaoEstadualRg).MaximumLength(30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Telefone).MaximumLength(20);
        RuleFor(x => x.CelularWhatsapp).MaximumLength(20);
        RuleFor(x => x.Endereco).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NumeroImovel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Bairro).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Uf).NotEmpty().Length(2);
        RuleFor(x => x.Cep).NotEmpty().MaximumLength(12);
        RuleFor(x => x.Pais).NotEmpty().MaximumLength(60);
        RuleFor(x => x.ResponsavelClinica).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PathLogotipo).MaximumLength(500);
        RuleFor(x => x.Cnae).MaximumLength(20);
        RuleFor(x => x.RedesSociais).MaximumLength(500);
    }
}
