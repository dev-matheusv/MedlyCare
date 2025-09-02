namespace SFA.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public int CodEmpresa { get; set; }
    public string Login { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string PasswordHash { get; set; } = null!; // pgcrypto (bcrypt)
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
