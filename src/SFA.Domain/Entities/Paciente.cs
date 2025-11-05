namespace SFA.Domain.Entities;

public class Paciente
{
  public Guid Id { get; set; }
  public int CodEmpresa { get; set; }

  public string Nome { get; set; } = null!;
  public string Documento { get; set; } = null!;        // CPF/ID (texto)
  public DateOnly? DataNascimento { get; set; } // era DateTime?
  public string? Telefone { get; set; }
  public string? Email { get; set; }

  public bool Ativo { get; set; } = true;
  public DateTime CriadoEm { get; set; }
  // Soft delete
  public bool IsDeleted { get; set; }
  public DateTimeOffset? DeletedAt { get; set; }
  public Guid? DeletedBy { get; set; }              // antes: int?
  public string? DeletedReason { get; set; }
}
