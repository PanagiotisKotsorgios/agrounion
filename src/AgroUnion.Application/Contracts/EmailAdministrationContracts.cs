namespace AgroUnion.Application.Contracts;

public sealed record EmailProviderSettingsDto(
    bool IsConfigured,
    bool IsEnabled,
    string ApiKeyHint,
    string SenderEmail,
    string SenderName,
    string? ReplyToEmail,
    DateTime? UpdatedAtUtc);

public sealed record NewsletterSubscriberDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string Source,
    bool IsActive,
    DateTime SubscribedAtUtc,
    DateTime? LastEmailAtUtc,
    int EmailsSent);

public sealed record EmailCampaignDto(
    Guid Id,
    string Subject,
    string Audience,
    string Status,
    int RecipientCount,
    int SentCount,
    int FailedCount,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc);

public sealed record EmailAdministrationDto(
    EmailProviderSettingsDto Settings,
    IReadOnlyList<NewsletterSubscriberDto> Subscribers,
    IReadOnlyList<EmailCampaignDto> Campaigns,
    int ActiveSubscriberCount,
    int ActivePartnerCount);

public sealed record BrevoSettingsRequest(
    string? ApiKey,
    string SenderEmail,
    string SenderName,
    string? ReplyToEmail,
    bool IsEnabled);

public sealed record EmailCampaignRequest(string Subject, string Body, string Audience);

public sealed record EmailCampaignResult(Guid CampaignId, int RecipientCount, int SentCount, int FailedCount);
