namespace SFA.Domain.Entities;

public class ComplexidadePaciente
{
    public Guid Id { get; set; }
    public int CodEmpresa { get; set; }

    public Guid PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public Guid ProfissionalId { get; set; }
    public Usuario Profissional { get; set; } = null!;

    // "baixa", "media", "alta"
    public string Nivel { get; set; } = null!;

    // Cor associada ao nível (ex: "#22c55e"), definida pelo usuário no cadastro
    public string Cor { get; set; } = null!;

    public string? Observacoes { get; set; }

    public DateTime CriadoEm { get; set; }
    public Guid CriadoPorUsuarioId { get; set; }
}
