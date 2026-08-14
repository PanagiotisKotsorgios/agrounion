using AgroUnion.Domain.Entities;

namespace AgroUnion.Application.Contracts;

public sealed record InterestApplicationRequest(
    PartnerRole Role,
    string FullNameOrCompany,
    string Region,
    string ProductInterest,
    string Phone,
    string Email,
    string Message,
    bool Consent,
    string? Website = null);

public sealed record ContactRequest(string FullName, string Email, string Message, string? Website = null);

public sealed record ProductionRequest(
    string Product,
    decimal Quantity,
    string Unit,
    string QualityGrade,
    string Region,
    DateOnly AvailableFrom,
    DateOnly AvailableTo);

public sealed record CounterOfferRequest(decimal PricePerUnit, decimal Quantity);
public sealed record SupplyParticipationRequest(decimal Quantity);
public sealed record PriceListRequest(PriceCategory Category, string ProductName, decimal Price, string Unit, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string VisibleToRoles);
public sealed record DealRequest(DealType DealType, Guid? ProductionDeclarationId, string FarmerUserId, decimal BuyPricePerUnit, decimal BuyQuantity, string BuyerUserId, decimal SellPricePerUnit, decimal SellQuantity);
public sealed record SupplyOrderRequest(string Title, string Product, string Description, DateOnly DeadlineDate);
public sealed record PickupRequest(DateTime ScheduledDate, string TransportDetails);
public sealed record ProducerProfileRequest(ProducerCategory Category, ProducerCategory? NextCategory, int CategoryProgressPercent, string UpgradeRequirements, decimal CommissionRate, decimal BonusRate, DateOnly RelationshipStartDate, string AccountManager, string PaymentTerms, string? InternalNotes);
public sealed record PartnerDocumentRequest(PartnerDocumentType Type, string Title, string ReferenceNumber, string? FileUrl, DateOnly IssueDate, DateOnly? ExpiryDate, string? Notes, bool IsVisibleToPartner);
public sealed record PartnerInvoiceRequest(PartnerInvoiceDirection Direction, string InvoiceNumber, DateOnly IssueDate, DateOnly? DueDate, decimal NetAmount, decimal VatAmount, decimal PaidAmount, PartnerInvoiceStatus Status, string Description, string? FileUrl);
public sealed record PartnerFinancialEntryRequest(DateOnly EntryDate, FinancialEntryType Type, FinancialEntryCategory Category, decimal Amount, string Description, string? ReferenceNumber, Guid? PartnerInvoiceId);
public sealed record ProducerDeliveryRequest(
    Guid? ProductionDeclarationId,
    Guid? ContractId,
    Guid? DealId,
    string RouteNumber,
    DeliveryLogisticsStatus Status,
    string Product,
    string? Variety,
    string QualityGrade,
    string? LotNumber,
    string OriginAddress,
    string DestinationAddress,
    string FactoryName,
    DateTime ScheduledPickupAt,
    DateTime? LoadedAt,
    DateTime? DeliveredAt,
    string CarrierName,
    string DriverName,
    string VehiclePlate,
    string? TrailerPlate,
    decimal GrossWeight,
    decimal TareWeight,
    decimal RejectedWeight,
    string WeightUnit,
    DateTime? WeighedAt,
    string? WeighbridgeName,
    string? WeighingSlipNumber,
    string? WeighingSlipUrl,
    string? DispatchNoteNumber,
    string? DispatchNoteUrl,
    string? DeliveryReceiptNumber,
    string? DeliveryReceiptUrl,
    string AgreementReference,
    PricingAgreementType AgreementType,
    decimal UnitPrice,
    decimal FactoryUnitPrice,
    decimal QualityBonusPercent,
    decimal CommissionPercent,
    decimal WithholdingPercent,
    decimal VatPercent,
    decimal TransportCost,
    decimal OtherDeductions,
    decimal PaidAmount,
    DateOnly? PaymentDueDate,
    DateTime? PaidAt,
    DeliveryPaymentStatus PaymentStatus,
    string? AgreementNotes,
    string? LoadNotes,
    string? InternalNotes,
    bool IsVisibleToProducer);

public sealed record LoginRequest(string Email, string Password, bool RememberMe);
public sealed record JwtLoginRequest(string Email, string Password);
