using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace WebApp.Services;

public class SmtpEmailSender(IOptions<EmailSenderOptions> options) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var settings = options.Value;

        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.SenderAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(settings.Username, settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
