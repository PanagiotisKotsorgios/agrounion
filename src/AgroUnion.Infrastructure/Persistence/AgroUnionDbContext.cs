using AgroUnion.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgroUnion.Infrastructure.Persistence;

public sealed class AgroUnionDbContext(DbContextOptions<AgroUnionDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<InterestApplication> InterestApplications => Set<InterestApplication>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ProductionDeclaration> ProductionDeclarations => Set<ProductionDeclaration>();
    public DbSet<PurchaseOffer> PurchaseOffers => Set<PurchaseOffer>();
    public DbSet<SellOffer> SellOffers => Set<SellOffer>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<PickupSchedule> PickupSchedules => Set<PickupSchedule>();
    public DbSet<SupplyOrder> SupplyOrders => Set<SupplyOrder>();
    public DbSet<SupplyOrderItem> SupplyOrderItems => Set<SupplyOrderItem>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ProducerCollaborationProfile> ProducerCollaborationProfiles => Set<ProducerCollaborationProfile>();
    public DbSet<PartnerDocument> PartnerDocuments => Set<PartnerDocument>();
    public DbSet<PartnerInvoice> PartnerInvoices => Set<PartnerInvoice>();
    public DbSet<PartnerFinancialEntry> PartnerFinancialEntries => Set<PartnerFinancialEntry>();
    public DbSet<ProducerDeliveryRecord> ProducerDeliveryRecords => Set<ProducerDeliveryRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PlatformRelease> PlatformReleases => Set<PlatformRelease>();
    public DbSet<PlatformReleaseAsset> PlatformReleaseAssets => Set<PlatformReleaseAsset>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<InterestApplication>().HasIndex(x => new { x.Status, x.CreatedAt });
        builder.Entity<InterestApplication>().HasIndex(x => x.Email);
        builder.Entity<ProductionDeclaration>().HasIndex(x => new { x.Product, x.Region, x.Status });
        builder.Entity<PurchaseOffer>().HasIndex(x => new { x.ProducerUserId, x.Status, x.ValidUntil });
        builder.Entity<SellOffer>().HasIndex(x => new { x.BuyerUserId, x.Status, x.ValidUntil });
        builder.Entity<Deal>().HasIndex(x => new { x.Status, x.CreatedAt });
        builder.Entity<Deal>().HasIndex(x => x.FarmerUserId);
        builder.Entity<Deal>().HasIndex(x => x.BuyerCounterpartyUserId);
        builder.Entity<Contract>().HasIndex(x => new { x.UserId, x.Status });
        builder.Entity<Transaction>().HasIndex(x => new { x.UserId, x.TransactionDate });
        builder.Entity<ProducerCollaborationProfile>().HasIndex(x => x.ProducerUserId).IsUnique();
        builder.Entity<PartnerDocument>().HasIndex(x => new { x.UserId, x.IssueDate });
        builder.Entity<PartnerInvoice>().HasIndex(x => new { x.UserId, x.IssueDate, x.Direction });
        builder.Entity<PartnerInvoice>().HasIndex(x => x.InvoiceNumber);
        builder.Entity<PartnerFinancialEntry>().HasIndex(x => new { x.UserId, x.EntryDate });
        builder.Entity<ProducerDeliveryRecord>().HasIndex(x => x.RouteNumber).IsUnique();
        builder.Entity<ProducerDeliveryRecord>().HasIndex(x => new { x.ProducerUserId, x.ScheduledPickupAt });
        builder.Entity<ProducerDeliveryRecord>().HasIndex(x => new { x.ProducerUserId, x.Status, x.PaymentStatus });
        builder.Entity<PriceListItem>().HasIndex(x => new { x.Category, x.ProductName, x.EffectiveFrom });
        builder.Entity<SupplyOrderItem>()
            .HasOne(x => x.SupplyOrder).WithMany(x => x.Items)
            .HasForeignKey(x => x.SupplyOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SupplyOrderItem>().HasIndex(x => new { x.SupplyOrderId, x.ProducerUserId }).IsUnique();
        builder.Entity<PlatformRelease>().HasIndex(x => x.Version).IsUnique();
        builder.Entity<PlatformRelease>().HasIndex(x => new { x.IsPublished, x.PublishedAtUtc });
        builder.Entity<PlatformReleaseAsset>()
            .HasOne(x => x.PlatformRelease).WithMany(x => x.Assets)
            .HasForeignKey(x => x.PlatformReleaseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PlatformReleaseAsset>().HasIndex(x => x.PlatformReleaseId);
    }
}
