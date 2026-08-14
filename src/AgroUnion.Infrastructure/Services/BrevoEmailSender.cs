using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgroUnion.Application.Services;
using AgroUnion.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgroUnion.Infrastructure.Services;

internal static class BrevoSecretProtection
{
    public const string Purpose = "AgroUnion.Brevo.ApiKey.v1";
}

public sealed class BrevoEmailSender(
    AgroUnionDbContext db,
    IDataProtectionProvider dataProtection,
    IHttpClientFactory clients,
    ILogger<BrevoEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var settings = await db.EmailProviderSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProviderName == "Brevo", cancellationToken);

        if (settings is null || !settings.IsEnabled || string.IsNullOrWhiteSpace(settings.EncryptedApiKey) || string.IsNullOrWhiteSpace(settings.SenderEmail))
        {
            logger.LogWarning("Email delivery skipped because Brevo has not been enabled by an administrator. Recipient: {Recipient}", to);
            return;
        }

        string apiKey;
        try
        {
            apiKey = dataProtection.CreateProtector(BrevoSecretProtection.Purpose).Unprotect(settings.EncryptedApiKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The stored Brevo API key could not be decrypted.");
            throw new InvalidOperationException("Το αποθηκευμένο κλειδί Brevo δεν μπορεί να αποκρυπτογραφηθεί. Αποθηκεύστε νέο κλειδί από τις ρυθμίσεις email.", ex);
        }

        var payload = new Dictionary<string, object?>
        {
            ["sender"] = new { name = settings.SenderName, email = settings.SenderEmail },
            ["to"] = new[] { new { email = to } },
            ["subject"] = subject,
            ["htmlContent"] = WrapEmail(subject, htmlBody)
        };
        if (!string.IsNullOrWhiteSpace(settings.ReplyToEmail))
            payload["replyTo"] = new { email = settings.ReplyToEmail };

        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("api-key", apiKey);
        request.Headers.TryAddWithoutValidation("accept", "application/json");

        var response = await clients.CreateClient("Brevo").SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var safeDetails = responseBody.Length > 1200 ? responseBody[..1200] : responseBody;
        logger.LogError("Brevo rejected an email to {Recipient}. Status {Status}. Response: {Response}", to, response.StatusCode, safeDetails);
        throw new HttpRequestException($"Η Brevo απέρριψε την αποστολή ({(int)response.StatusCode} {response.StatusCode}). {ExtractMessage(safeDetails)}");
    }

    private static string WrapEmail(string subject, string htmlBody)
    {
        var safeSubject = WebUtility.HtmlEncode(subject);
        return $$"""
            <!doctype html>
            <html lang="el"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;background:#f3f1e9;font-family:Arial,sans-serif;color:#203027">
              <div style="max-width:680px;margin:0 auto;padding:30px 18px">
                <div style="background:#173d2c;padding:22px 28px;color:#fff;border-radius:12px 12px 0 0">
                  <strong style="font-size:20px;letter-spacing:.04em">AGRO UNION</strong>
                  <span style="display:block;margin-top:5px;color:#d7c68d;font-size:12px;letter-spacing:.12em">ΔΙΚΤΥΟ ΣΥΝΕΡΓΑΣΙΑΣ</span>
                </div>
                <div style="background:#fff;padding:32px 28px;border:1px solid #e2ded1;border-top:0;border-radius:0 0 12px 12px;line-height:1.65">
                  <h1 style="font-size:23px;line-height:1.3;margin:0 0 22px;color:#173d2c">{{safeSubject}}</h1>
                  {{htmlBody}}
                  <div style="margin-top:30px;padding-top:20px;border-top:1px solid #ece8dd;color:#67746c;font-size:12px">
                    AGRO UNION · info@agro-union.gr · 26310 28971
                  </div>
                </div>
              </div>
            </body></html>
            """;
    }

    private static string ExtractMessage(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("message", out var message)) return message.GetString() ?? string.Empty;
        }
        catch (JsonException) { }
        return string.Empty;
    }
}
