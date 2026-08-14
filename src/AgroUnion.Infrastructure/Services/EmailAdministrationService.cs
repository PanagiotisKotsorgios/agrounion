using System.Net;
using System.Net.Mail;
using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Domain.Entities;
using AgroUnion.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgroUnion.Infrastructure.Services;

public sealed class EmailAdministrationService(
    AgroUnionDbContext db,
    IDataProtectionProvider dataProtection,
    IEmailSender sender,
    IHttpContextAccessor httpContextAccessor,
    ILogger<EmailAdministrationService> logger) : IEmailAdministrationService
{
    private static readonly string[] PartnerRoles = [RoleNames.Producer, RoleNames.Trader, RoleNames.Company];

    public async Task SubscribeAsync(string email, string? displayName = null, string source = "Website", CancellationToken ct = default)
    {
        var validEmail = ValidateEmail(email, "email εγγραφής");
        var normalized = Normalize(validEmail);
        var item = await db.NewsletterSubscribers.SingleOrDefaultAsync(x => x.NormalizedEmail == normalized, ct);
        if (item is null)
        {
            db.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Email = validEmail,
                NormalizedEmail = normalized,
                DisplayName = Clean(displayName, 180),
                Source = Clean(source, 40) ?? "Website"
            });
        }
        else
        {
            item.Email = validEmail;
            item.DisplayName = Clean(displayName, 180) ?? item.DisplayName;
            item.Source = Clean(source, 40) ?? item.Source;
            item.IsActive = true;
            item.UnsubscribedAtUtc = null;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> UnsubscribeAsync(Guid token, CancellationToken ct = default)
    {
        var item = await db.NewsletterSubscribers.SingleOrDefaultAsync(x => x.UnsubscribeToken == token, ct);
        if (item is null) return false;
        item.IsActive = false;
        item.UnsubscribedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<EmailAdministrationDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var setting = await db.EmailProviderSettings.AsNoTracking().SingleOrDefaultAsync(x => x.ProviderName == "Brevo", ct);
        var subscribers = await db.NewsletterSubscribers.AsNoTracking().OrderByDescending(x => x.SubscribedAtUtc).Take(300)
            .Select(x => new NewsletterSubscriberDto(x.Id, x.Email, x.DisplayName, x.Source, x.IsActive, x.SubscribedAtUtc, x.LastEmailAtUtc, x.EmailsSent))
            .ToListAsync(ct);
        var campaigns = await db.EmailCampaigns.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(30)
            .Select(x => new EmailCampaignDto(x.Id, x.Subject, x.Audience, x.Status, x.RecipientCount, x.SentCount, x.FailedCount, x.CreatedAtUtc, x.SentAtUtc))
            .ToListAsync(ct);

        var partnerRoleIds = await db.Roles.Where(x => PartnerRoles.Contains(x.Name!)).Select(x => x.Id).ToListAsync(ct);
        var partnerUserIds = db.UserRoles.Where(x => partnerRoleIds.Contains(x.RoleId)).Select(x => x.UserId);
        var activePartners = await db.Users.CountAsync(x => x.IsActive && x.Email != null && partnerUserIds.Contains(x.Id), ct);

        return new EmailAdministrationDto(
            new EmailProviderSettingsDto(
                setting is not null && !string.IsNullOrWhiteSpace(setting.EncryptedApiKey),
                setting?.IsEnabled == true,
                setting?.ApiKeyHint ?? string.Empty,
                setting?.SenderEmail ?? "info@agro-union.gr",
                setting?.SenderName ?? "AGRO UNION",
                setting?.ReplyToEmail ?? "info@agro-union.gr",
                setting?.UpdatedAtUtc),
            subscribers,
            campaigns,
            await db.NewsletterSubscribers.CountAsync(x => x.IsActive, ct),
            activePartners);
    }

    public async Task SaveSettingsAsync(BrevoSettingsRequest request, string adminUserId, CancellationToken ct = default)
    {
        var item = await db.EmailProviderSettings.SingleOrDefaultAsync(x => x.ProviderName == "Brevo", ct);
        if (item is null)
        {
            item = new EmailProviderSetting();
            db.EmailProviderSettings.Add(item);
        }

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            var apiKey = request.ApiKey.Trim();
            if (apiKey.Length < 20) throw new InvalidOperationException("Το API key της Brevo δεν φαίνεται έγκυρο.");
            item.EncryptedApiKey = dataProtection.CreateProtector(BrevoSecretProtection.Purpose).Protect(apiKey);
            item.ApiKeyHint = apiKey.Length <= 4 ? apiKey : apiKey[^4..];
        }
        if (request.IsEnabled && string.IsNullOrWhiteSpace(item.EncryptedApiKey))
            throw new InvalidOperationException("Προσθέστε API key πριν ενεργοποιήσετε τις αποστολές.");

        item.SenderEmail = ValidateEmail(request.SenderEmail, "email αποστολέα");
        item.SenderName = string.IsNullOrWhiteSpace(request.SenderName) ? "AGRO UNION" : request.SenderName.Trim()[..Math.Min(request.SenderName.Trim().Length, 180)];
        item.ReplyToEmail = string.IsNullOrWhiteSpace(request.ReplyToEmail) ? null : ValidateEmail(request.ReplyToEmail, "email απάντησης");
        item.IsEnabled = request.IsEnabled;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.UpdatedByUserId = adminUserId;
        await db.SaveChangesAsync(ct);
    }

    public async Task SendTestAsync(string recipientEmail, CancellationToken ct = default)
    {
        var recipient = ValidateEmail(recipientEmail, "email δοκιμής");
        await EnsureProviderReadyAsync(ct);
        await sender.SendAsync(recipient, "Δοκιμή σύνδεσης Brevo", "<p>Η σύνδεση της πλατφόρμας AGRO UNION με τη Brevo λειτουργεί κανονικά.</p><p>Οι ειδοποιήσεις λογαριασμού και οι ενημερώσεις του δικτύου μπορούν πλέον να αποστέλλονται από το κεντρικό σύστημα.</p>", ct);
    }

    public Task AddSubscriberAsync(string email, string? displayName, CancellationToken ct = default) =>
        SubscribeAsync(email, displayName, "Admin", ct);

    public async Task SetSubscriberActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        var item = await db.NewsletterSubscribers.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Ο συνδρομητής δεν βρέθηκε.");
        item.IsActive = active;
        item.UnsubscribedAtUtc = active ? null : DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<EmailCampaignResult> SendCampaignAsync(EmailCampaignRequest request, string adminUserId, CancellationToken ct = default)
    {
        var subject = request.Subject?.Trim() ?? string.Empty;
        var body = request.Body?.Trim() ?? string.Empty;
        var audience = request.Audience?.Trim() ?? string.Empty;
        if (subject.Length is < 3 or > 220) throw new InvalidOperationException("Το θέμα πρέπει να έχει από 3 έως 220 χαρακτήρες.");
        if (body.Length is < 10 or > 12000) throw new InvalidOperationException("Το μήνυμα πρέπει να έχει από 10 έως 12.000 χαρακτήρες.");
        if (audience is not ("Newsletter" or "Partners" or "All")) throw new InvalidOperationException("Επιλέξτε έγκυρο κοινό αποστολής.");
        await EnsureProviderReadyAsync(ct);

        var newsletter = audience is "Newsletter" or "All"
            ? await db.NewsletterSubscribers.Where(x => x.IsActive).ToListAsync(ct)
            : [];
        var recipients = new Dictionary<string, CampaignRecipient>(StringComparer.OrdinalIgnoreCase);
        foreach (var subscriber in newsletter)
            recipients[subscriber.NormalizedEmail] = new CampaignRecipient(subscriber.Email, subscriber.DisplayName, subscriber);

        if (audience is "Partners" or "All")
        {
            var partnerRoleIds = await db.Roles.Where(x => PartnerRoles.Contains(x.Name!)).Select(x => x.Id).ToListAsync(ct);
            var partnerUserIds = db.UserRoles.Where(x => partnerRoleIds.Contains(x.RoleId)).Select(x => x.UserId);
            var partners = await db.Users.Where(x => x.IsActive && x.Email != null && partnerUserIds.Contains(x.Id))
                .Select(x => new { x.Email, x.FullNameOrCompany }).ToListAsync(ct);
            foreach (var partner in partners)
            {
                var normalized = Normalize(partner.Email!);
                if (!recipients.ContainsKey(normalized)) recipients[normalized] = new CampaignRecipient(partner.Email!, partner.FullNameOrCompany, null);
            }
        }
        if (recipients.Count == 0) throw new InvalidOperationException("Δεν υπάρχουν ενεργοί παραλήπτες για το επιλεγμένο κοινό.");
        if (recipients.Count > 1000) throw new InvalidOperationException("Η αποστολή περιορίζεται σε 1.000 παραλήπτες ανά καμπάνια.");

        var campaign = new EmailCampaign
        {
            Subject = subject,
            PlainTextBody = body,
            Audience = audience,
            Status = "Sending",
            RecipientCount = recipients.Count,
            CreatedByUserId = adminUserId
        };
        db.EmailCampaigns.Add(campaign);
        await db.SaveChangesAsync(ct);

        var sent = 0;
        var failures = new List<string>();
        foreach (var recipient in recipients.Values)
        {
            try
            {
                var html = BuildCampaignHtml(body, recipient);
                await sender.SendAsync(recipient.Email, subject, html, ct);
                sent++;
                if (recipient.Subscriber is not null)
                {
                    recipient.Subscriber.LastEmailAtUtc = DateTime.UtcNow;
                    recipient.Subscriber.EmailsSent++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Campaign {CampaignId} failed for {Recipient}", campaign.Id, recipient.Email);
                if (failures.Count < 8) failures.Add($"{recipient.Email}: {ex.Message}");
            }
        }

        campaign.SentCount = sent;
        campaign.FailedCount = recipients.Count - sent;
        campaign.Status = campaign.FailedCount == 0 ? "Completed" : sent == 0 ? "Failed" : "Partial";
        campaign.ErrorSummary = failures.Count == 0 ? null : string.Join(Environment.NewLine, failures);
        campaign.SentAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new EmailCampaignResult(campaign.Id, recipients.Count, sent, campaign.FailedCount);
    }

    private async Task EnsureProviderReadyAsync(CancellationToken ct)
    {
        var ready = await db.EmailProviderSettings.AnyAsync(x => x.ProviderName == "Brevo" && x.IsEnabled && x.EncryptedApiKey != "" && x.SenderEmail != "", ct);
        if (!ready) throw new InvalidOperationException("Η Brevo δεν είναι ακόμη ενεργοποιημένη. Αποθηκεύστε πρώτα το API key και τα στοιχεία αποστολέα.");
    }

    private string BuildCampaignHtml(string body, CampaignRecipient recipient)
    {
        var safeBody = WebUtility.HtmlEncode(body).Replace("\r\n", "<br>").Replace("\n", "<br>");
        var greeting = string.IsNullOrWhiteSpace(recipient.Name) ? "" : $"<p>Αγαπητέ/ή {WebUtility.HtmlEncode(recipient.Name)},</p>";
        var unsubscribe = string.Empty;
        if (recipient.Subscriber is not null)
        {
            var request = httpContextAccessor.HttpContext?.Request;
            var path = $"/newsletter/unsubscribe/{recipient.Subscriber.UnsubscribeToken:D}";
            var url = request is null ? path : $"{request.Scheme}://{request.Host}{request.PathBase}{path}";
            unsubscribe = $"<p style=\"margin-top:28px;font-size:12px;color:#67746c\">Δεν επιθυμείτε άλλες ενημερώσεις; <a href=\"{WebUtility.HtmlEncode(url)}\">Διαγραφή από τη λίστα</a>.</p>";
        }
        return $"{greeting}<div>{safeBody}</div>{unsubscribe}";
    }

    private static string ValidateEmail(string? value, string label)
    {
        if (!MailAddress.TryCreate(value?.Trim(), out var address)) throw new InvalidOperationException($"Συμπληρώστε έγκυρο {label}.");
        return address.Address;
    }

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();
    private static string? Clean(string? value, int maxLength)
    {
        var cleaned = value?.Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) return null;
        return cleaned[..Math.Min(cleaned.Length, maxLength)];
    }

    private sealed record CampaignRecipient(string Email, string? Name, NewsletterSubscriber? Subscriber);
}
