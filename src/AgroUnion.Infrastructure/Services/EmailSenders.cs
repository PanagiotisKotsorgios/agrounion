using AgroUnion.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace AgroUnion.Infrastructure.Services;

public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("DEV EMAIL προς {Recipient}: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("Smtp");
        using var message = new MailMessage(section["From"] ?? throw new InvalidOperationException("Λείπει το Smtp:From."), to, subject, htmlBody) { IsBodyHtml = true };
        using var client = new SmtpClient(section["Host"], section.GetValue("Port", 587)) { EnableSsl = section.GetValue("UseTls", true) };
        var username = section["Username"];
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, section["Password"]);
        await client.SendMailAsync(message, cancellationToken);
    }
}
