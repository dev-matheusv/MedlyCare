namespace SFA.Domain.Entities;

public class Empresa
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
