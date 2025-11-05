namespace SFA.Domain.Entities;

public class UsuarioPerfil
{
  public Guid UsuarioId { get; set; }
  public Guid PerfilId  { get; set; }

  public Usuario Usuario { get; set; } = null!;
  public Perfil  Perfil  { get; set; } = null!;
}
