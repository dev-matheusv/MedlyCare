namespace SFA.Domain.Entities;

public class PacienteComplexidade
{
    public Guid Id { get; set; }
    public int CodEmpresa { get; set; }

    public Guid PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public Guid ComplexidadeId { get; set; }
    public Complexidade Complexidade { get; set; } = null!;

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public Guid? AtendimentoId { get; set; }
    public Atendimento? Atendimento { get; set; }

    public DateTime Data { get; set; }
}
