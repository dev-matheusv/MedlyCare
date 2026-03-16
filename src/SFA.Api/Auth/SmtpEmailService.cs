using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SFA.Application.Auth;

namespace SFA.Api.Auth;

public class SmtpEmailService : IEmailService
{
  private readonly SmtpOptions _options;

  public SmtpEmailService(IOptions<SmtpOptions> options)
  {
    _options = options.Value;
  }

  public async Task SendAsync(string to, string subject, string html)
  {
    using var message = new MailMessage();
    message.From = new MailAddress(_options.FromEmail, _options.FromName);
    message.Subject = subject;
    message.Body = html;
    message.IsBodyHtml = true;

    message.To.Add(to);

    using var client = new SmtpClient(_options.Host, _options.Port);
    client.Credentials = new NetworkCredential(_options.User, _options.Password);
    client.EnableSsl = _options.EnableSsl;

    await client.SendMailAsync(message);
  }
}
