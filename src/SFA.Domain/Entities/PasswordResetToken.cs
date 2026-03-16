namespace SFA.Domain.Entities;

public class PasswordResetToken
{
  public Guid Id { get; set; }
  public Guid UsuarioId { get; set; }
  public string Token { get; set; } = null!;
  public DateTime ExpiraEm { get; set; }
  public DateTime? UsadoEm { get; set; }
  public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

  public Usuario Usuario { get; set; } = null!;
}
