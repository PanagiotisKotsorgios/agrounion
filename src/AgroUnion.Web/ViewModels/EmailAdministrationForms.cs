using AgroUnion.Application.Contracts;

namespace AgroUnion.Web.ViewModels;

public sealed class BrevoSettingsForm
{
    public string? ApiKey { get; set; }
    public string SenderEmail { get; set; } = "info@agro-union.gr";
    public string SenderName { get; set; } = "AGRO UNION";
    public string? ReplyToEmail { get; set; } = "info@agro-union.gr";
    public bool IsEnabled { get; set; }
    public BrevoSettingsRequest ToRequest() => new(ApiKey, SenderEmail, SenderName, ReplyToEmail, IsEnabled);
}

public sealed class NewsletterSubscriberForm
{
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public sealed class EmailCampaignForm
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Audience { get; set; } = "Newsletter";
    public EmailCampaignRequest ToRequest() => new(Subject, Body, Audience);
}
