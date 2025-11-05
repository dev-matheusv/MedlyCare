namespace SFA.Domain.Entities;

public class Perfil
{
  public Guid Id { get; set; }
  public int CodEmpresa { get; set; }          // se perfil for por empresa; se quiser global, pode remover
  public string Nome { get; set; } = null!;
  public DateTime CriadoEm { get; set; }
  public bool Ativo { get; set; } = true;

  public ICollection<UsuarioPerfil> UsuariosPerfis { get; set; } = [];
}
