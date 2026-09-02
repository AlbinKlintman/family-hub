using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebApp.Tests.Integration;

public class TestEmailSender : IEmailSender
{
    private readonly ConcurrentBag<SentEmail> _sentEmails = [];

    public IReadOnlyCollection<SentEmail> SentEmails => _sentEmails.ToArray();

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _sentEmails.Add(new SentEmail(email, subject, htmlMessage));
        return Task.CompletedTask;
    }

    public record SentEmail(string To, string Subject, string HtmlMessage);
}
