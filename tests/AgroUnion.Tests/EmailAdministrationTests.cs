using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Infrastructure;
using AgroUnion.Infrastructure.Persistence;
using AgroUnion.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace AgroUnion.Tests;

public sealed class EmailAdministrationTests
{
    [Fact]
    public async Task NewsletterSubscription_DeduplicatesAndReactivatesAddress()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEmailAdministrationService>();
        var db = scope.ServiceProvider.GetRequiredService<AgroUnionDbContext>();

        await service.SubscribeAsync("  Member@Example.gr ", "Πρώτη εγγραφή");
        var subscriber = await db.NewsletterSubscribers.SingleAsync();
        await service.SetSubscriberActiveAsync(subscriber.Id, false);
        await service.SubscribeAsync("member@example.gr", "Νέα επωνυμία");

        var rows = await db.NewsletterSubscribers.ToListAsync();
        Assert.Single(rows);
        Assert.True(rows[0].IsActive);
        Assert.Null(rows[0].UnsubscribedAtUtc);
        Assert.Equal("Νέα επωνυμία", rows[0].DisplayName);
    }

    [Fact]
    public async Task BrevoSettings_EncryptApiKeyAndExposeOnlyHint()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEmailAdministrationService>();
        var db = scope.ServiceProvider.GetRequiredService<AgroUnionDbContext>();
        const string apiKey = "xkeysib-production-key-1234567890";

        await service.SaveSettingsAsync(new BrevoSettingsRequest(apiKey, "info@agro-union.gr", "AGRO UNION", "info@agro-union.gr", true), "admin-id");

        var stored = await db.EmailProviderSettings.SingleAsync();
        var dashboard = await service.GetDashboardAsync();
        Assert.DoesNotContain(apiKey, stored.EncryptedApiKey, StringComparison.Ordinal);
        Assert.Equal("7890", dashboard.Settings.ApiKeyHint);
        Assert.True(dashboard.Settings.IsConfigured);
        Assert.True(dashboard.Settings.IsEnabled);
    }

    [Fact]
    public async Task BrevoSender_UsesOfficialEndpointAndApiKeyHeader()
    {
        var options = new DbContextOptionsBuilder<AgroUnionDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AgroUnionDbContext(options);
        var protection = new EphemeralDataProtectionProvider();
        const string apiKey = "xkeysib-request-test-1234567890";
        db.EmailProviderSettings.Add(new()
        {
            EncryptedApiKey = protection.CreateProtector("AgroUnion.Brevo.ApiKey.v1").Protect(apiKey),
            ApiKeyHint = "7890",
            SenderEmail = "info@agro-union.gr",
            SenderName = "AGRO UNION",
            IsEnabled = true,
            UpdatedByUserId = "admin"
        });
        await db.SaveChangesAsync();
        var handler = new RecordingHandler();
        var sender = new BrevoEmailSender(db, protection, new FixedHttpClientFactory(handler), NullLogger<BrevoEmailSender>.Instance);

        await sender.SendAsync("member@example.gr", "Θέμα δοκιμής", "<p>Μήνυμα</p>");

        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.RequestUri);
        Assert.Equal(apiKey, handler.ApiKey);
        Assert.Contains("member@example.gr", handler.Payload, StringComparison.Ordinal);
        Assert.Contains("AGRO UNION", handler.Payload, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DatabaseProvider"] = "InMemory"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private sealed class FixedHttpClientFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.brevo.com/v3/") };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string RequestUri { get; private set; } = string.Empty;
        public string ApiKey { get; private set; } = string.Empty;
        public string Payload { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString() ?? string.Empty;
            ApiKey = request.Headers.GetValues("api-key").Single();
            Payload = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{\"messageId\":\"test\"}") };
        }
    }
}
