using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroUnion.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public sealed class InterestApplication : Entity
{
    public PartnerRole Role { get; set; }
    [MaxLength(180)] public string FullNameOrCompany { get; set; } = string.Empty;
    [MaxLength(120)] public string Region { get; set; } = string.Empty;
    [MaxLength(160)] public string ProductInterest { get; set; } = string.Empty;
    [MaxLength(40)] public string Phone { get; set; } = string.Empty;
    [MaxLength(180)] public string Email { get; set; } = string.Empty;
    [MaxLength(3000)] public string Message { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.New;
    [MaxLength(3000)] public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? HandledByUserId { get; set; }
    public DateTime? HandledAt { get; set; }
}

public sealed class ContactMessage : Entity
{
    [MaxLength(180)] public string FullName { get; set; } = string.Empty;
    [MaxLength(180)] public string Email { get; set; } = string.Empty;
    [MaxLength(3000)] public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}

public sealed class Contract : Entity
{
    public string UserId { get; set; } = string.Empty;
    [MaxLength(40)] public string ContractNumber { get; set; } = string.Empty;
    public PartnerRole PartyRole { get; set; }
    public ContractSubject Subject { get; set; }
    public DurationType DurationType { get; set; }
    [MaxLength(1500)] public string PricingTerms { get; set; } = string.Empty;
    [MaxLength(1500)] public string QuantityTerms { get; set; } = string.Empty;
    [MaxLength(1500)] public string TerminationTerms { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    [MaxLength(500)] public string? PdfFilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ProductionDeclaration : Entity
{
    public string ProducerUserId { get; set; } = string.Empty;
    [MaxLength(120)] public string Product { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,3)")] public decimal Quantity { get; set; }
    [MaxLength(30)] public string Unit { get; set; } = "kg";
    [MaxLength(80)] public string QualityGrade { get; set; } = string.Empty;
    [MaxLength(120)] public string Region { get; set; } = string.Empty;
    public DateOnly AvailableFrom { get; set; }
    public DateOnly AvailableTo { get; set; }
    public ProductionStatus Status { get; set; } = ProductionStatus.Available;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PurchaseOffer : Entity
{
    public string ProducerUserId { get; set; } = string.Empty;
    [MaxLength(120)] public string Product { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,4)")] public decimal BuyPricePerUnit { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal TargetQuantity { get; set; }
    [MaxLength(120)] public string Region { get; set; } = string.Empty;
    public DateTime ValidUntil { get; set; }
    public OfferStatus Status { get; set; } = OfferStatus.Active;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SellOffer : Entity
{
    public string BuyerUserId { get; set; } = string.Empty;
    [MaxLength(120)] public string Product { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,4)")] public decimal SellPricePerUnit { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal TargetQuantity { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal? CounterPricePerUnit { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal? RequestedQuantity { get; set; }
    public DateTime? BuyerRespondedAt { get; set; }
    [MaxLength(120)] public string Region { get; set; } = string.Empty;
    public DateTime ValidUntil { get; set; }
    public OfferStatus Status { get; set; } = OfferStatus.Active;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Deal : Entity
{
    public DealType DealType { get; set; }
    public Guid? ProductionDeclarationId { get; set; }
    public string FarmerUserId { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,4)")] public decimal BuyPricePerUnit { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal BuyQuantity { get; set; }
    public string BuyerCounterpartyUserId { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,4)")] public decimal SellPricePerUnit { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal SellQuantity { get; set; }
    [NotMapped] public decimal MarginPerUnit => DealType == DealType.Brokerage ? SellPricePerUnit - BuyPricePerUnit : 0;
    [NotMapped] public decimal TotalMargin => MarginPerUnit * Math.Min(BuyQuantity, SellQuantity);
    public DealStatus Status { get; set; } = DealStatus.Proposed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public sealed class PickupSchedule : Entity
{
    public Guid DealId { get; set; }
    public DateTime ScheduledDate { get; set; }
    [MaxLength(1000)] public string TransportDetails { get; set; } = string.Empty;
    public PickupStatus Status { get; set; } = PickupStatus.Scheduled;
}

public sealed class SupplyOrder : Entity
{
    [MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(160)] public string Product { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    public DateOnly DeadlineDate { get; set; }
    public SupplyOrderStatus Status { get; set; } = SupplyOrderStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<SupplyOrderItem> Items { get; set; } = [];
}

public sealed class SupplyOrderItem : Entity
{
    public Guid SupplyOrderId { get; set; }
    public SupplyOrder? SupplyOrder { get; set; }
    public string ProducerUserId { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,3)")] public decimal Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PriceListItem : Entity
{
    public string PublishedByUserId { get; set; } = string.Empty;
    public PriceCategory Category { get; set; }
    [MaxLength(160)] public string ProductName { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,4)")] public decimal Price { get; set; }
    [MaxLength(30)] public string Unit { get; set; } = "kg";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    [MaxLength(180)] public string VisibleToRoles { get; set; } = string.Empty;
}

public sealed class Transaction : Entity
{
    public string UserId { get; set; } = string.Empty;
    public Guid? RelatedContractId { get; set; }
    public Guid? DealId { get; set; }
    public TransactionSide Side { get; set; }
    [MaxLength(120)] public string Product { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,3)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal UnitPrice { get; set; }
    [NotMapped] public decimal TotalValue => Quantity * UnitPrice;
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    [MaxLength(120)] public string Region { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Notes { get; set; }
}

public sealed class ProducerCollaborationProfile : Entity
{
    public string ProducerUserId { get; set; } = string.Empty;
    public ProducerCategory Category { get; set; } = ProducerCategory.Developing;
    public ProducerCategory? NextCategory { get; set; } = ProducerCategory.Standard;
    public int CategoryProgressPercent { get; set; }
    [MaxLength(2500)] public string UpgradeRequirements { get; set; } = string.Empty;
    [Column(TypeName = "decimal(7,3)")] public decimal CommissionRate { get; set; }
    [Column(TypeName = "decimal(7,3)")] public decimal BonusRate { get; set; }
    public DateOnly RelationshipStartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [MaxLength(180)] public string AccountManager { get; set; } = string.Empty;
    [MaxLength(500)] public string PaymentTerms { get; set; } = string.Empty;
    [MaxLength(3000)] public string? InternalNotes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedByUserId { get; set; } = string.Empty;
}

public sealed class PartnerDocument : Entity
{
    public string UserId { get; set; } = string.Empty;
    public PartnerDocumentType Type { get; set; }
    [MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(80)] public string ReferenceNumber { get; set; } = string.Empty;
    [MaxLength(1000)] public string? FileUrl { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    [MaxLength(1500)] public string? Notes { get; set; }
    public bool IsVisibleToPartner { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
}

public sealed class PartnerInvoice : Entity
{
    public string UserId { get; set; } = string.Empty;
    public PartnerInvoiceDirection Direction { get; set; }
    [MaxLength(80)] public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal NetAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal VatAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PaidAmount { get; set; }
    [NotMapped] public decimal TotalAmount => NetAmount + VatAmount;
    [NotMapped] public decimal OutstandingAmount => Math.Max(0, TotalAmount - PaidAmount);
    public PartnerInvoiceStatus Status { get; set; } = PartnerInvoiceStatus.Issued;
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    [MaxLength(1000)] public string? FileUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
}

public sealed class PartnerFinancialEntry : Entity
{
    public string UserId { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public FinancialEntryType Type { get; set; }
    public FinancialEntryCategory Category { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [MaxLength(1000)] public string Description { get; set; } = string.Empty;
    [MaxLength(80)] public string? ReferenceNumber { get; set; }
    public Guid? PartnerInvoiceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
}

public sealed class ProducerDeliveryRecord : Entity
{
    public string ProducerUserId { get; set; } = string.Empty;
    public Guid? ProductionDeclarationId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? DealId { get; set; }
    [MaxLength(80)] public string RouteNumber { get; set; } = string.Empty;
    public DeliveryLogisticsStatus Status { get; set; } = DeliveryLogisticsStatus.Scheduled;
    [MaxLength(120)] public string Product { get; set; } = string.Empty;
    [MaxLength(120)] public string? Variety { get; set; }
    [MaxLength(120)] public string QualityGrade { get; set; } = string.Empty;
    [MaxLength(80)] public string? LotNumber { get; set; }
    [MaxLength(300)] public string OriginAddress { get; set; } = string.Empty;
    [MaxLength(300)] public string DestinationAddress { get; set; } = string.Empty;
    public DateTime ScheduledPickupAt { get; set; }
    public DateTime? LoadedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    [MaxLength(120)] public string CarrierName { get; set; } = string.Empty;
    [MaxLength(120)] public string DriverName { get; set; } = string.Empty;
    [MaxLength(30)] public string VehiclePlate { get; set; } = string.Empty;
    [MaxLength(30)] public string? TrailerPlate { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal GrossWeight { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal TareWeight { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal NetWeight { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal RejectedWeight { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal AcceptedWeight { get; set; }
    [MaxLength(30)] public string WeightUnit { get; set; } = "kg";
    public DateTime? WeighedAt { get; set; }
    [MaxLength(180)] public string? WeighbridgeName { get; set; }
    [MaxLength(80)] public string? WeighingSlipNumber { get; set; }
    [MaxLength(1000)] public string? WeighingSlipUrl { get; set; }
    [MaxLength(80)] public string? DispatchNoteNumber { get; set; }
    [MaxLength(1000)] public string? DispatchNoteUrl { get; set; }
    [MaxLength(80)] public string? DeliveryReceiptNumber { get; set; }
    [MaxLength(1000)] public string? DeliveryReceiptUrl { get; set; }
    [MaxLength(80)] public string AgreementReference { get; set; } = string.Empty;
    public PricingAgreementType AgreementType { get; set; } = PricingAgreementType.FixedPrice;
    [Column(TypeName = "decimal(18,4)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(7,3)")] public decimal QualityBonusPercent { get; set; }
    [Column(TypeName = "decimal(7,3)")] public decimal CommissionPercent { get; set; }
    [Column(TypeName = "decimal(7,3)")] public decimal WithholdingPercent { get; set; }
    [Column(TypeName = "decimal(7,3)")] public decimal VatPercent { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TransportCost { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal OtherDeductions { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal BaseAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal BonusAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal CommissionAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal WithholdingAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal VatAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal NetPayableAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PaidAmount { get; set; }
    public DateOnly? PaymentDueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public DeliveryPaymentStatus PaymentStatus { get; set; } = DeliveryPaymentStatus.Pending;
    [MaxLength(2000)] public string? AgreementNotes { get; set; }
    [MaxLength(2000)] public string? LoadNotes { get; set; }
    [MaxLength(2000)] public string? InternalNotes { get; set; }
    public bool IsVisibleToProducer { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string UpdatedByUserId { get; set; } = string.Empty;
    [NotMapped] public decimal OutstandingAmount => Math.Max(0, NetPayableAmount - PaidAmount);
}

public sealed class AuditLog : Entity
{
    public string? UserId { get; set; }
    [MaxLength(120)] public string Action { get; set; } = string.Empty;
    [MaxLength(120)] public string EntityName { get; set; } = string.Empty;
    [MaxLength(80)] public string EntityId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [MaxLength(3000)] public string Details { get; set; } = string.Empty;
}

public sealed class Notification : Entity
{
    public string UserId { get; set; } = string.Empty;
    [MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(1000)] public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PlatformRelease : Entity
{
    [MaxLength(40)] public string Version { get; set; } = string.Empty;
    [MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(5000)] public string ReleaseNotes { get; set; } = string.Empty;
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public ICollection<PlatformReleaseAsset> Assets { get; set; } = [];
}

public sealed class PlatformReleaseAsset : Entity
{
    public Guid PlatformReleaseId { get; set; }
    public PlatformRelease? PlatformRelease { get; set; }
    [MaxLength(180)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(180)] public string OriginalFileName { get; set; } = string.Empty;
    [MaxLength(100)] public string TargetPlatform { get; set; } = "Όλες οι πλατφόρμες";
    [MaxLength(260)] public string? StoredFileName { get; set; }
    [MaxLength(150)] public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    [MaxLength(1000)] public string? GitHubDownloadUrl { get; set; }
    public long DownloadCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EmailProviderSetting : Entity
{
    [MaxLength(40)] public string ProviderName { get; set; } = "Brevo";
    [MaxLength(5000)] public string EncryptedApiKey { get; set; } = string.Empty;
    [MaxLength(16)] public string ApiKeyHint { get; set; } = string.Empty;
    [MaxLength(180)] public string SenderEmail { get; set; } = string.Empty;
    [MaxLength(180)] public string SenderName { get; set; } = "AGRO UNION";
    [MaxLength(180)] public string? ReplyToEmail { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(450)] public string UpdatedByUserId { get; set; } = string.Empty;
}

public sealed class NewsletterSubscriber : Entity
{
    [MaxLength(180)] public string Email { get; set; } = string.Empty;
    [MaxLength(180)] public string NormalizedEmail { get; set; } = string.Empty;
    [MaxLength(180)] public string? DisplayName { get; set; }
    [MaxLength(40)] public string Source { get; set; } = "Website";
    public Guid UnsubscribeToken { get; set; } = Guid.NewGuid();
    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UnsubscribedAtUtc { get; set; }
    public DateTime? LastEmailAtUtc { get; set; }
    public int EmailsSent { get; set; }
}

public sealed class EmailCampaign : Entity
{
    [MaxLength(220)] public string Subject { get; set; } = string.Empty;
    [Column(TypeName = "text"), MaxLength(12000)] public string PlainTextBody { get; set; } = string.Empty;
    [MaxLength(30)] public string Audience { get; set; } = "Newsletter";
    [MaxLength(30)] public string Status { get; set; } = "Draft";
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    [Column(TypeName = "text"), MaxLength(3000)] public string? ErrorSummary { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
    [MaxLength(450)] public string CreatedByUserId { get; set; } = string.Empty;
}

public sealed class PartnerProductionListing : Entity
{
    public Guid ProductionDeclarationId { get; set; }
    [MaxLength(450)] public string ProducerUserId { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,3)")] public decimal OfferedQuantity { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal AskingPricePerUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PartnerBuyingRequest : Entity
{
    [MaxLength(450)] public string BuyerUserId { get; set; } = string.Empty;
    [MaxLength(120)] public string Product { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,3)")] public decimal Quantity { get; set; }
    [MaxLength(30)] public string Unit { get; set; } = "kg";
    [Column(TypeName = "decimal(18,4)")] public decimal MaxPricePerUnit { get; set; }
    [MaxLength(120)] public string Region { get; set; } = string.Empty;
    [MaxLength(1000)] public string? QualityRequirements { get; set; }
    public DateTime ValidUntilUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PartnerMarketplaceInquiry : Entity
{
    public Guid? ProductionListingId { get; set; }
    public Guid? BuyingRequestId { get; set; }
    [MaxLength(450)] public string SenderUserId { get; set; } = string.Empty;
    [MaxLength(450)] public string RecipientUserId { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,3)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal OfferedPricePerUnit { get; set; }
    [MaxLength(1500)] public string? Message { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "New";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
