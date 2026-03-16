namespace SFA.Domain.Entities;

public class Empresa
{
  public Guid Id { get; set; }

  public int CodEmpresa { get; set; }

  public string RazaoSocial { get; set; } = null!;
  public string Documento { get; set; } = null!; // CNPJ ou CPF
  public string? InscricaoEstadualRg { get; set; }
  public string Email { get; set; } = null!;
  public string? Telefone { get; set; }
  public string? CelularWhatsapp { get; set; }
  public bool UtilizarCelularParaEnvioMensagens { get; set; }

  public string Endereco { get; set; } = null!;
  public int NumeroImovel { get; set; }
  public string Bairro { get; set; } = null!;
  public string Cidade { get; set; } = null!;
  public string Uf { get; set; } = null!;
  public string Cep { get; set; } = null!;
  public string Pais { get; set; } = "Brasil";

  public string ResponsavelClinica { get; set; } = null!;
  public string? PathLogotipo { get; set; }
  public string? Cnae { get; set; }
  public string? RedesSociais { get; set; }

  public bool Ativa { get; set; } = true;
  public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
