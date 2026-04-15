namespace SFA.Domain.Entities;

public class Complexidade
{
    public Guid Id { get; set; }
    public int CodEmpresa { get; set; }
    public string Descricao { get; set; } = null!;
    public string Cor { get; set; } = null!;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
