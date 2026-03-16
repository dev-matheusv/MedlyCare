namespace SFA.Application.Auth;

public class SmtpOptions
{
  public string Host { get; set; } = null!;
  public int Port { get; set; }
  public string User { get; set; } = null!;
  public string Password { get; set; } = null!;
  public string FromEmail { get; set; } = null!;
  public string FromName { get; set; } = "MedlyCare";
  public bool EnableSsl { get; set; } = true;
  public string ResetBaseUrl { get; set; } = null!;
}
