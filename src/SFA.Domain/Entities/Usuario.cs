namespace SFA.Domain.Entities;

public class Usuario
{
  public Guid Id { get; set; }
  public int CodEmpresa { get; set; }
  public string Login { get; set; } = null!;
  public string Nome { get; set; } = null!;
  public string Email { get; set; } = null!;
  public string? Telefone { get; set; }
  public string? CelularWhatsapp { get; set; }
  public string? Crm { get; set; }
  public string PasswordHash { get; set; } = null!;
  public bool Ativo { get; set; } = true;
  public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

  public ICollection<UsuarioPerfil> UsuariosPerfis { get; set; } = [];
  public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
}
