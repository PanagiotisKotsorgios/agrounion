using AgroUnion.Domain.Entities;

namespace AgroUnion.Application.Contracts;

public sealed record UserSummary(string Id, string Name, string Email, string Role, string Region, bool IsActive);
public sealed record ProductionSummary(Guid Id, string Product, decimal Quantity, string Unit, string QualityGrade, string Region, DateOnly AvailableFrom, DateOnly AvailableTo, ProductionStatus Status);
public sealed record VolumeSummary(string Product, string Region, decimal Quantity, string Unit);
public sealed record PurchaseOfferDto(Guid Id, string Product, decimal BuyPricePerUnit, decimal TargetQuantity, string Region, DateTime ValidUntil, OfferStatus Status);
public sealed record SellOfferDto(Guid Id, string Product, decimal SellPricePerUnit, decimal TargetQuantity, decimal? CounterPricePerUnit, decimal? RequestedQuantity, string Region, DateTime ValidUntil, OfferStatus Status);
public sealed record FarmerDealDto(Guid Id, string Product, decimal BuyPricePerUnit, decimal Quantity, DealStatus Status, DateTime CreatedAt);
public sealed record BuyerDealDto(Guid Id, string Product, decimal SellPricePerUnit, decimal Quantity, DealStatus Status, DateTime CreatedAt);
public sealed record AdminDealDto(Guid Id, DealType DealType, string Product, decimal BuyPricePerUnit, decimal SellPricePerUnit, decimal Quantity, decimal MarginPerUnit, decimal TotalMargin, DealStatus Status, DateTime CreatedAt);
public sealed record ContractDto(Guid Id, string ContractNumber, ContractSubject Subject, ContractStatus Status, DateOnly StartDate, DateOnly? EndDate, string PricingTerms, string QuantityTerms);
public sealed record TransactionDto(Guid Id, string Product, TransactionSide Side, decimal Quantity, decimal UnitPrice, decimal TotalValue, DateTime Date, string Region);
public sealed record SupplyOrderDto(Guid Id, string Title, string Product, string Description, DateOnly Deadline, SupplyOrderStatus Status, decimal TotalQuantity, decimal? MyQuantity);
public sealed record PriceItemDto(Guid Id, PriceCategory Category, string ProductName, decimal Price, string Unit, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record PickupDto(Guid Id, Guid DealId, DateTime ScheduledDate, string TransportDetails, PickupStatus Status);
public sealed record NotificationDto(Guid Id, string Title, string Message, bool IsRead, DateTime CreatedAt);
public sealed record AdminProductionDto(Guid Id, string Producer, string Product, decimal Quantity, string Unit, string Quality, string Region, ProductionStatus Status);
public sealed record AdminOfferDto(Guid Id, string Side, string Counterparty, string Product, decimal Price, decimal Quantity, string Region, OfferStatus Status);
public sealed record AdminContractDto(Guid Id, string ContractNumber, string Partner, ContractSubject Subject, ContractStatus Status, DateOnly StartDate);
public sealed record ProducerCollaborationProfileDto(
    string ProducerUserId,
    string ProducerName,
    string Email,
    string Region,
    ProducerCategory Category,
    ProducerCategory? NextCategory,
    int CategoryProgressPercent,
    string UpgradeRequirements,
    decimal CommissionRate,
    decimal BonusRate,
    DateOnly RelationshipStartDate,
    string AccountManager,
    string PaymentTerms,
    string? InternalNotes,
    DateTime? UpdatedAt);
public sealed record PartnerDocumentDto(Guid Id, PartnerDocumentType Type, string Title, string ReferenceNumber, string? FileUrl, DateOnly IssueDate, DateOnly? ExpiryDate, string? Notes, bool IsVisibleToPartner);
public sealed record PartnerInvoiceDto(Guid Id, PartnerInvoiceDirection Direction, string InvoiceNumber, DateOnly IssueDate, DateOnly? DueDate, decimal NetAmount, decimal VatAmount, decimal TotalAmount, decimal PaidAmount, decimal OutstandingAmount, PartnerInvoiceStatus Status, string Description, string? FileUrl);
public sealed record PartnerFileAccessDto(string StorageKey, string DownloadName);
public sealed record AccountPreferenceDto(bool EmailNotifications, bool DeliveryNotifications, bool CompactDashboard, string DateFormat);
public sealed record AccountStatisticDto(string Label, string Value, string Icon);
public sealed record AccountAuditDto(Guid Id, string Action, string Details, DateTime Timestamp);
public sealed record AccountProfileDto(
    string UserId,
    string FullNameOrCompany,
    string Email,
    string? PhoneNumber,
    string Region,
    string Role,
    DateTime CreatedAt,
    bool EmailConfirmed,
    AccountPreferenceDto Preferences,
    IReadOnlyList<AccountStatisticDto> Statistics,
    IReadOnlyList<AccountAuditDto> AuditLog);
public sealed record PartnerFinancialEntryDto(Guid Id, DateOnly EntryDate, FinancialEntryType Type, FinancialEntryCategory Category, decimal Amount, string Description, string? ReferenceNumber, Guid? PartnerInvoiceId);
public sealed record ProducerFinancialSummaryDto(decimal DeclaredProduction, decimal DeliveredProduction, decimal AverageDeliveryPrice, decimal DeliveryRevenue, decimal OtherIncome, decimal Expenses, decimal NetCashFlow, decimal ReceivableFromAgroUnion, decimal PayableToAgroUnion);
public sealed record ProducerDeliveryDto(
    Guid Id,
    string ProducerUserId,
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
    decimal NetWeight,
    decimal RejectedWeight,
    decimal AcceptedWeight,
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
    decimal BaseAmount,
    decimal BonusAmount,
    decimal CommissionAmount,
    decimal WithholdingAmount,
    decimal VatAmount,
    decimal NetPayableAmount,
    decimal FactoryGrossValue,
    decimal PaidAmount,
    decimal OutstandingAmount,
    DateOnly? PaymentDueDate,
    DateTime? PaidAt,
    DeliveryPaymentStatus PaymentStatus,
    string? AgreementNotes,
    string? LoadNotes,
    string? InternalNotes,
    bool IsVisibleToProducer,
    DateTime UpdatedAt);
public sealed record ProducerLogisticsSummaryDto(
    int TotalRoutes,
    int ActiveRoutes,
    int CompletedRoutes,
    decimal GrossCollectedWeight,
    decimal AcceptedWeight,
    decimal RejectedWeight,
    decimal BaseValue,
    decimal Bonuses,
    decimal Commissions,
    decimal Withholdings,
    decimal TransportAndDeductions,
    decimal Vat,
    decimal NetPayable,
    decimal Paid,
    decimal Outstanding);
public sealed record ProducerAdminWorkspaceDto(
    IReadOnlyList<UserSummary> Producers,
    UserSummary? SelectedProducer,
    ProducerCollaborationProfileDto? Profile,
    ProducerFinancialSummaryDto Summary,
    IReadOnlyList<ProductionSummary> Production,
    IReadOnlyList<FarmerDealDto> Deliveries,
    IReadOnlyList<ContractDto> Contracts,
    IReadOnlyList<PartnerDocumentDto> Documents,
    IReadOnlyList<PartnerInvoiceDto> Invoices,
    IReadOnlyList<PartnerFinancialEntryDto> FinancialEntries,
    IReadOnlyList<ProducerDeliveryDto> DeliveryRecords,
    ProducerLogisticsSummaryDto LogisticsSummary);

public sealed record AdminDashboardDto(
    int ActivePartners,
    int PendingApplications,
    decimal AvailableVolume,
    int ActiveContracts,
    decimal TotalMargin,
    IReadOnlyList<InterestApplication> Applications,
    IReadOnlyList<AdminDealDto> Deals,
    IReadOnlyList<UserSummary> Users,
    IReadOnlyList<ContactMessage> Messages,
    IReadOnlyList<PriceItemDto> PriceList,
    IReadOnlyList<SupplyOrderDto> SupplyOrders,
    IReadOnlyList<AdminProductionDto> Production,
    IReadOnlyList<AdminOfferDto> Offers,
    IReadOnlyList<AdminContractDto> Contracts,
    ProducerAdminWorkspaceDto ProducerWorkspace);

public sealed record ProducerDashboardDto(
    IReadOnlyList<ProductionSummary> Declarations,
    IReadOnlyList<PurchaseOfferDto> Offers,
    IReadOnlyList<ContractDto> Contracts,
    IReadOnlyList<TransactionDto> Transactions,
    IReadOnlyList<SupplyOrderDto> SupplyOrders,
    IReadOnlyList<PriceItemDto> PriceList,
    IReadOnlyList<FarmerDealDto> Deals,
    ProducerCollaborationProfileDto Profile,
    ProducerFinancialSummaryDto FinancialSummary,
    IReadOnlyList<PartnerDocumentDto> Documents,
    IReadOnlyList<PartnerInvoiceDto> Invoices,
    IReadOnlyList<PartnerFinancialEntryDto> FinancialEntries,
    IReadOnlyList<ProducerDeliveryDto> DeliveryRecords,
    ProducerLogisticsSummaryDto LogisticsSummary);

public sealed record BuyerDashboardDto(
    IReadOnlyList<VolumeSummary> AvailableVolume,
    IReadOnlyList<SellOfferDto> Offers,
    IReadOnlyList<ContractDto> Contracts,
    IReadOnlyList<TransactionDto> Transactions,
    IReadOnlyList<PriceItemDto> PriceList,
    IReadOnlyList<PickupDto> Pickups,
    IReadOnlyList<BuyerDealDto> Deals,
    bool IsCompany);

public sealed record ApprovalResult(string UserId, string Email, string TemporaryPassword, Guid ContractId);
public sealed record JwtTokenResult(string Token, DateTime ExpiresAt, string Role);
