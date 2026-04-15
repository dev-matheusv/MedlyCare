namespace SFA.Domain.Entities;

public class ReceituarioMedicoItem
{
  public Guid Id { get; set; }

  public Guid ReceituarioMedicoId { get; set; }
  public ReceituarioMedico ReceituarioMedico { get; set; } = null!;

  public string NomeMedicamento { get; set; } = null!;
  public string? FormaFarmaceutica { get; set; }
  public string? Concentracao { get; set; }
  public string? ViaAdministracao { get; set; }
  public string? Posologia { get; set; }
  public string Quantidade { get; set; } = null!;
  public string? QuantidadeExtenso { get; set; }
  public string? Orientacoes { get; set; }
}
