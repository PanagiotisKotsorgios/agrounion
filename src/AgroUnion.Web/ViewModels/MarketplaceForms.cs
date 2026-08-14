using AgroUnion.Application.Contracts;
using System.Globalization;

namespace AgroUnion.Web.ViewModels;

public sealed class PartnerProductionListingForm
{
    public Guid ProductionDeclarationId { get; set; }
    public string OfferedQuantity { get; set; } = string.Empty;
    public string AskingPricePerUnit { get; set; } = string.Empty;
    public PartnerProductionListingRequest ToRequest() => new(ProductionDeclarationId, MarketplaceFormParser.Parse(OfferedQuantity, "ποσότητα"), MarketplaceFormParser.Parse(AskingPricePerUnit, "ζητούμενη τιμή"));
}

public sealed class PartnerBuyingRequestForm
{
    public string Product { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Unit { get; set; } = "kg";
    public string MaxPricePerUnit { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? QualityRequirements { get; set; }
    public DateTime ValidUntilUtc { get; set; } = DateTime.UtcNow.AddDays(30);
    public PartnerBuyingRequestRequest ToRequest() => new(Product, MarketplaceFormParser.Parse(Quantity, "ποσότητα"), Unit, MarketplaceFormParser.Parse(MaxPricePerUnit, "μέγιστη τιμή"), Region, QualityRequirements, ValidUntilUtc);
}

public sealed class PartnerMarketplaceInquiryForm
{
    public Guid? ProductionListingId { get; set; }
    public Guid? BuyingRequestId { get; set; }
    public string? PartnerUserId { get; set; }
    public string Quantity { get; set; } = string.Empty;
    public string OfferedPricePerUnit { get; set; } = string.Empty;
    public string? Message { get; set; }
    public PartnerMarketplaceInquiryRequest ToRequest() => new(ProductionListingId, BuyingRequestId, PartnerUserId, MarketplaceFormParser.Parse(Quantity, "ποσότητα"), MarketplaceFormParser.Parse(OfferedPricePerUnit, "προτεινόμενη τιμή"), Message);
}

internal static class MarketplaceFormParser
{
    public static decimal Parse(string value, string field)
    {
        if (decimal.TryParse(value?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)) return invariant;
        if (decimal.TryParse(value?.Trim(), NumberStyles.Number, CultureInfo.GetCultureInfo("el-GR"), out var greek)) return greek;
        throw new InvalidOperationException($"Συμπληρώστε έγκυρη {field}.");
    }
}
