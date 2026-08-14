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
    public DbSet<UserPortalPreference> UserPortalPreferences => Set<UserPortalPreference>();
    public DbSet<PlatformRelease> PlatformReleases => Set<PlatformRelease>();
    public DbSet<PlatformReleaseAsset> PlatformReleaseAssets => Set<PlatformReleaseAsset>();
    public DbSet<EmailProviderSetting> EmailProviderSettings => Set<EmailProviderSetting>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<EmailCampaign> EmailCampaigns => Set<EmailCampaign>();
    public DbSet<PartnerProductionListing> PartnerProductionListings => Set<PartnerProductionListing>();
    public DbSet<PartnerBuyingRequest> PartnerBuyingRequests => Set<PartnerBuyingRequest>();
    public DbSet<PartnerMarketplaceInquiry> PartnerMarketplaceInquiries => Set<PartnerMarketplaceInquiry>();

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
        builder.Entity<UserPortalPreference>().HasIndex(x => x.UserId).IsUnique();
        builder.Entity<UserPortalPreference>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
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
        builder.Entity<EmailProviderSetting>().HasIndex(x => x.ProviderName).IsUnique();
        builder.Entity<NewsletterSubscriber>().HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Entity<NewsletterSubscriber>().HasIndex(x => x.UnsubscribeToken).IsUnique();
        builder.Entity<NewsletterSubscriber>().HasIndex(x => new { x.IsActive, x.SubscribedAtUtc });
        builder.Entity<EmailCampaign>().HasIndex(x => x.CreatedAtUtc);
        builder.Entity<PartnerProductionListing>().HasIndex(x => x.ProductionDeclarationId).IsUnique();
        builder.Entity<PartnerProductionListing>().HasIndex(x => new { x.IsActive, x.UpdatedAtUtc });
        builder.Entity<PartnerProductionListing>().HasOne<ProductionDeclaration>().WithMany().HasForeignKey(x => x.ProductionDeclarationId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PartnerProductionListing>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ProducerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PartnerBuyingRequest>().HasIndex(x => new { x.IsActive, x.ValidUntilUtc });
        builder.Entity<PartnerBuyingRequest>().HasIndex(x => new { x.Product, x.Region });
        builder.Entity<PartnerBuyingRequest>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.BuyerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PartnerMarketplaceInquiry>().HasIndex(x => new { x.RecipientUserId, x.Status, x.CreatedAtUtc });
        builder.Entity<PartnerMarketplaceInquiry>().HasIndex(x => x.SenderUserId);
        builder.Entity<PartnerMarketplaceInquiry>().HasOne<PartnerProductionListing>().WithMany().HasForeignKey(x => x.ProductionListingId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PartnerMarketplaceInquiry>().HasOne<PartnerBuyingRequest>().WithMany().HasForeignKey(x => x.BuyingRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PartnerMarketplaceInquiry>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PartnerMarketplaceInquiry>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
