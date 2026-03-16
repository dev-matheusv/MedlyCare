namespace SFA.Application.Auth;

public interface IEmailService
{
  Task SendAsync(string to, string subject, string html);
}
