namespace SFA.Domain.Entities;

public class UsuarioPerfil
{
  public int UsuarioId { get; set; }
  public int PerfilId  { get; set; }

  public Usuario Usuario { get; set; } = null!;
  public Perfil  Perfil  { get; set; } = null!;
}
