using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Domain.Entities;

namespace AgroUnion.Tests;

public sealed class ValidationTests
{
    private static InterestApplicationRequest ValidInterest => new(PartnerRole.Producer, "Δοκιμαστικός Παραγωγός", "Αιτωλικό", "Ελαιόλαδο", "+30 690 000 0000", "demo@example.gr", "Μήνυμα", true);

    [Fact]
    public void Valid_interest_application_passes() => Assert.True(new InterestApplicationValidator().Validate(ValidInterest).IsValid);

    [Fact]
    public void Gdpr_consent_is_required()
    {
        var request = ValidInterest with { Consent = false };
        Assert.Contains(new InterestApplicationValidator().Validate(request).Errors, x => x.PropertyName == nameof(request.Consent));
    }

    [Fact]
    public void Honeypot_rejects_bot_submission()
    {
        var request = ValidInterest with { Website = "https://spam.invalid" };
        Assert.Contains(new InterestApplicationValidator().Validate(request).Errors, x => x.PropertyName == nameof(request.Website));
    }

    [Fact]
    public void Production_end_cannot_precede_start()
    {
        var request = new ProductionRequest("Ελιά", 100, "kg", "A", "Μεσολόγγι", new(2026, 10, 2), new(2026, 10, 1));
        Assert.False(new ProductionRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Counter_offer_requires_positive_price_and_quantity()
    {
        var result = new CounterOfferValidator().Validate(new CounterOfferRequest(0, -1));
        Assert.Equal(2, result.Errors.Count);
    }
}
