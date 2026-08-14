namespace AgroUnion.Domain.Entities;

public enum PartnerRole { Producer, Trader, Company, Other }
public enum ApplicationStatus { New, InReview, Approved, Rejected }
public enum ContractSubject { Sale, Supply }
public enum DurationType { Fixed, Indefinite }
public enum ContractStatus { Draft, Active, Terminated }
public enum ProductionStatus { Available, Reserved, Sold }
public enum OfferStatus { Active, Accepted, Closed }
public enum DealType { Brokerage, Facilitation }
public enum DealStatus { Proposed, BuySideConfirmed, SellSideConfirmed, Completed, Cancelled }
public enum PickupStatus { Scheduled, Completed, Cancelled }
public enum SupplyOrderStatus { Open, Closed }
public enum PriceCategory { Product, Supply }
public enum TransactionSide { Purchase, Sale, Facilitation }
public enum ProducerCategory { Developing, Standard, Advanced, Premium, Strategic }
public enum PartnerDocumentType { ContractAnnex, Certification, TaxDocument, DeliveryNote, Statement, Other }
public enum PartnerInvoiceDirection { FromAgroUnion, FromProducer }
public enum PartnerInvoiceStatus { Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled }
public enum FinancialEntryType { Income, Expense }
public enum FinancialEntryCategory { ProductSale, Supplies, Commission, Bonus, Transport, Services, Adjustment, Other }
public enum DeliveryLogisticsStatus { Scheduled, InTransit, Weighed, Delivered, Settled, Cancelled }
public enum DeliveryPaymentStatus { Pending, PartiallyPaid, Paid, OnHold }
public enum PricingAgreementType { FixedPrice, DailyPrice, Percentage, Mixed }

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Producer = "Producer";
    public const string Trader = "Trader";
    public const string Company = "Company";
    public static readonly string[] All = [Admin, Producer, Trader, Company];

    public static string FromPartnerRole(PartnerRole role) => role switch
    {
        PartnerRole.Producer => Producer,
        PartnerRole.Trader => Trader,
        PartnerRole.Company => Company,
        _ => throw new InvalidOperationException("Ο ρόλος «Άλλο» πρέπει να εξειδικευτεί πριν από την έγκριση.")
    };
}
