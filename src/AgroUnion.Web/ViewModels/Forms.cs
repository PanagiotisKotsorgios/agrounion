using AgroUnion.Application.Contracts;
using AgroUnion.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace AgroUnion.Web.ViewModels;

public sealed class InterestForm
{
    public PartnerRole Role { get; set; }
    public string FullNameOrCompany { get; set; } = "";
    public string Region { get; set; } = "";
    public string ProductInterest { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Message { get; set; } = "";
    public bool Consent { get; set; }
    public string? Website { get; set; }
    public InterestApplicationRequest ToRequest() => new(Role, FullNameOrCompany, Region, ProductInterest, Phone, Email, Message, Consent, Website);
}

public sealed class ContactForm
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Website { get; set; }
    public ContactRequest ToRequest() => new(FullName, Email, Message, Website);
}

public sealed class HomeViewModel
{
    public InterestForm Interest { get; set; } = new();
    public ContactForm Contact { get; set; } = new();
}

public sealed class LoginForm
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class ForgotPasswordForm
{
    public string Email { get; set; } = "";
    public string? Website { get; set; }
}

public sealed class ResetPasswordForm
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

public sealed class ChangePasswordForm
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

public sealed class ProductionForm
{
    public Guid? Id { get; set; }
    public string Product { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public string QualityGrade { get; set; } = "";
    public string Region { get; set; } = "";
    public DateOnly AvailableFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly AvailableTo { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
    public ProductionRequest ToRequest() => new(Product, Quantity, Unit, QualityGrade, Region, AvailableFrom, AvailableTo);
}

public sealed class CounterOfferForm { public Guid OfferId { get; set; } public decimal PricePerUnit { get; set; } public decimal Quantity { get; set; } }
public sealed class SupplyParticipationForm { public Guid OrderId { get; set; } public decimal Quantity { get; set; } }
public sealed class ApplicationUpdateForm { public Guid Id { get; set; } public ApplicationStatus Status { get; set; } public string? Notes { get; set; } }
public sealed class UserActiveForm { public string UserId { get; set; } = ""; public bool Active { get; set; } }
public sealed class UserRoleForm { public string UserId { get; set; } = ""; public string Role { get; set; } = ""; }
public sealed class PriceItemForm
{
    public PriceCategory Category { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public string Unit { get; set; } = "kg";
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? EffectiveTo { get; set; }
    public string VisibleToRoles { get; set; } = $"{RoleNames.Producer},{RoleNames.Trader}";
}
public sealed class DealForm
{
    public DealType DealType { get; set; }
    public Guid? ProductionDeclarationId { get; set; }
    public string FarmerUserId { get; set; } = "";
    public decimal BuyPricePerUnit { get; set; }
    public decimal BuyQuantity { get; set; }
    public string BuyerUserId { get; set; } = "";
    public decimal SellPricePerUnit { get; set; }
    public decimal SellQuantity { get; set; }
}
public sealed class SupplyOrderForm { public string Title { get; set; } = ""; public string Product { get; set; } = ""; public string Description { get; set; } = ""; public DateOnly DeadlineDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)); }
public sealed class PickupForm { public Guid DealId { get; set; } public DateTime ScheduledDate { get; set; } = DateTime.Today.AddDays(7).AddHours(9); public string TransportDetails { get; set; } = ""; }
public abstract class ProducerAdminForm { public string ProducerUserId { get; set; } = ""; }
public sealed class AdminProductionForm : ProducerAdminForm
{
    public Guid? Id { get; set; }
    public string Product { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public string QualityGrade { get; set; } = "";
    public string Region { get; set; } = "";
    public DateOnly AvailableFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly AvailableTo { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
    public ProductionRequest ToRequest() => new(Product, Quantity, Unit, QualityGrade, Region, AvailableFrom, AvailableTo);
}
public sealed class ProducerProfileForm : ProducerAdminForm
{
    public ProducerCategory Category { get; set; }
    public ProducerCategory? NextCategory { get; set; }
    public int CategoryProgressPercent { get; set; }
    public string UpgradeRequirements { get; set; } = "";
    public decimal CommissionRate { get; set; }
    public decimal BonusRate { get; set; }
    public DateOnly RelationshipStartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string AccountManager { get; set; } = "";
    public string PaymentTerms { get; set; } = "";
    public string? InternalNotes { get; set; }
    public ProducerProfileRequest ToRequest() => new(Category, NextCategory, CategoryProgressPercent, UpgradeRequirements, CommissionRate, BonusRate, RelationshipStartDate, AccountManager, PaymentTerms, InternalNotes);
}
public sealed class PartnerDocumentForm : ProducerAdminForm
{
    public PartnerDocumentType Type { get; set; }
    public string Title { get; set; } = "";
    public string ReferenceNumber { get; set; } = "";
    public string? FileUrl { get; set; }
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public bool IsVisibleToPartner { get; set; } = true;
    public PartnerDocumentRequest ToRequest() => new(Type, Title, ReferenceNumber, FileUrl, IssueDate, ExpiryDate, Notes, IsVisibleToPartner);
}
public sealed class PartnerInvoiceForm : ProducerAdminForm
{
    public PartnerInvoiceDirection Direction { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? DueDate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public PartnerInvoiceStatus Status { get; set; } = PartnerInvoiceStatus.Issued;
    public string Description { get; set; } = "";
    public string? FileUrl { get; set; }
    public PartnerInvoiceRequest ToRequest() => new(Direction, InvoiceNumber, IssueDate, DueDate, NetAmount, VatAmount, PaidAmount, Status, Description, FileUrl);
}
public sealed class PartnerFinancialEntryForm : ProducerAdminForm
{
    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public FinancialEntryType Type { get; set; }
    public FinancialEntryCategory Category { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public string? ReferenceNumber { get; set; }
    public Guid? PartnerInvoiceId { get; set; }
    public PartnerFinancialEntryRequest ToRequest() => new(EntryDate, Type, Category, Amount, Description, ReferenceNumber, PartnerInvoiceId);
}
public sealed class ProducerDeliveryForm : ProducerAdminForm
{
    public Guid? Id { get; set; }
    public Guid? ProductionDeclarationId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? DealId { get; set; }
    public string RouteNumber { get; set; } = "";
    public DeliveryLogisticsStatus Status { get; set; } = DeliveryLogisticsStatus.Scheduled;
    public string Product { get; set; } = "";
    public string? Variety { get; set; }
    public string QualityGrade { get; set; } = "";
    public string? LotNumber { get; set; }
    public string OriginAddress { get; set; } = "";
    public string DestinationAddress { get; set; } = "Κέντρο παραλαβής AGRO UNION";
    public DateTime ScheduledPickupAt { get; set; } = DateTime.Today.AddDays(1).AddHours(8);
    public DateTime? LoadedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string CarrierName { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string VehiclePlate { get; set; } = "";
    public string? TrailerPlate { get; set; }
    public decimal GrossWeight { get; set; }
    public decimal TareWeight { get; set; }
    public decimal RejectedWeight { get; set; }
    public string WeightUnit { get; set; } = "kg";
    public DateTime? WeighedAt { get; set; }
    public string? WeighbridgeName { get; set; }
    public string? WeighingSlipNumber { get; set; }
    public string? WeighingSlipUrl { get; set; }
    public string? DispatchNoteNumber { get; set; }
    public string? DispatchNoteUrl { get; set; }
    public string? DeliveryReceiptNumber { get; set; }
    public string? DeliveryReceiptUrl { get; set; }
    public string AgreementReference { get; set; } = "";
    public PricingAgreementType AgreementType { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal QualityBonusPercent { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal WithholdingPercent { get; set; }
    public decimal VatPercent { get; set; }
    public decimal TransportCost { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? PaymentDueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public DeliveryPaymentStatus PaymentStatus { get; set; }
    public string? AgreementNotes { get; set; }
    public string? LoadNotes { get; set; }
    public string? InternalNotes { get; set; }
    public bool IsVisibleToProducer { get; set; } = true;
    public ProducerDeliveryRequest ToRequest() => new(ProductionDeclarationId, ContractId, DealId, RouteNumber, Status, Product, Variety, QualityGrade, LotNumber, OriginAddress, DestinationAddress, ScheduledPickupAt, LoadedAt, DeliveredAt, CarrierName, DriverName, VehiclePlate, TrailerPlate, GrossWeight, TareWeight, RejectedWeight, WeightUnit, WeighedAt, WeighbridgeName, WeighingSlipNumber, WeighingSlipUrl, DispatchNoteNumber, DispatchNoteUrl, DeliveryReceiptNumber, DeliveryReceiptUrl, AgreementReference, AgreementType, UnitPrice, QualityBonusPercent, CommissionPercent, WithholdingPercent, VatPercent, TransportCost, OtherDeductions, PaidAmount, PaymentDueDate, PaidAt, PaymentStatus, AgreementNotes, LoadNotes, InternalNotes, IsVisibleToProducer);
}

public sealed class ReleaseUploadForm
{
    public string Version { get; set; } = "";
    public string Title { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public DateTime PublishedAt { get; set; } = DateTime.Today;
    public bool IsPublished { get; set; } = true;
}

public sealed record ReleaseViewModel(
    Guid Id,
    string Version,
    string Title,
    string ReleaseNotes,
    DateTime PublishedAtUtc,
    bool IsPublished);

public sealed class ReleaseCatalogViewModel
{
    public IReadOnlyList<ReleaseViewModel> Releases { get; init; } = [];
    public bool IsAdmin { get; init; }
    public ReleaseUploadForm Upload { get; init; } = new();
    public int PublishedCount => Releases.Count(x => x.IsPublished);
}
