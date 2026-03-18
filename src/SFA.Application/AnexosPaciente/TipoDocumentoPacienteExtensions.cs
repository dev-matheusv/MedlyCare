using SFA.Domain.Enums;

namespace SFA.Application.AnexosPaciente;

public static class TipoDocumentoPacienteExtensions
{
  public static string ToDescricao(this TipoDocumentoPaciente tipoDocumento)
  {
    return tipoDocumento switch
    {
      TipoDocumentoPaciente.ExameLaboratorial => "Exame laboratorial",
      TipoDocumentoPaciente.ExameImagem => "Exame de imagem",
      TipoDocumentoPaciente.Laudo => "Laudo",
      TipoDocumentoPaciente.ReceitaExterna => "Receita externa",
      TipoDocumentoPaciente.DocumentoPessoal => "Documento pessoal",
      TipoDocumentoPaciente.TermoConsentimento => "Termo de consentimento",
      TipoDocumentoPaciente.Outros => "Outros",
      _ => "Não informado"
    };
  }
}
