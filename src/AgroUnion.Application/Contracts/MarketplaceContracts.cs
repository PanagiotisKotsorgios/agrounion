namespace AgroUnion.Application.Contracts;

public sealed record PartnerDirectoryDto(string UserId, string Name, string Role, string Region, IReadOnlyList<string> Products);

public sealed record MarketplaceProductionOptionDto(Guid ProductionDeclarationId, string Product, string QualityGrade, string Region, decimal UncommittedQuantity, string Unit);

public sealed record PartnerProductionListingDto(
    Guid Id,
    string ProducerUserId,
    string ProducerName,
    string Product,
    string QualityGrade,
    string Region,
    decimal AvailableQuantity,
    string Unit,
    decimal AskingPricePerUnit,
    bool IsMine);

public sealed record PartnerBuyingRequestDto(
    Guid Id,
    string BuyerUserId,
    string BuyerName,
    string BuyerRole,
    string Product,
    decimal Quantity,
    string Unit,
    decimal MaxPricePerUnit,
    string Region,
    string? QualityRequirements,
    DateTime ValidUntilUtc,
    bool IsMine);

public sealed record PartnerMarketplaceInquiryDto(
    Guid Id,
    string CounterpartyName,
    string Direction,
    string Context,
    decimal Quantity,
    decimal OfferedPricePerUnit,
    string Status,
    DateTime CreatedAtUtc);

public sealed record PartnerMarketplaceDto(
    string CurrentUserId,
    string CurrentRole,
    IReadOnlyList<PartnerDirectoryDto> Partners,
    IReadOnlyList<PartnerProductionListingDto> ProductionListings,
    IReadOnlyList<PartnerBuyingRequestDto> BuyingRequests,
    IReadOnlyList<MarketplaceProductionOptionDto> OwnProductionOptions,
    IReadOnlyList<PartnerMarketplaceInquiryDto> Inquiries,
    IReadOnlyList<string> Regions,
    IReadOnlyList<string> Products,
    string? SelectedRole,
    string? SelectedRegion,
    string? SelectedProduct,
    string? Search);

public sealed record PartnerProductionListingRequest(Guid ProductionDeclarationId, decimal OfferedQuantity, decimal AskingPricePerUnit);
public sealed record PartnerBuyingRequestRequest(string Product, decimal Quantity, string Unit, decimal MaxPricePerUnit, string Region, string? QualityRequirements, DateTime ValidUntilUtc);
public sealed record PartnerMarketplaceInquiryRequest(Guid? ProductionListingId, Guid? BuyingRequestId, string? PartnerUserId, decimal Quantity, decimal OfferedPricePerUnit, string? Message);
