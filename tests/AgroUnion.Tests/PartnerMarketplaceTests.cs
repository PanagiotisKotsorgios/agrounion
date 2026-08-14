using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Domain.Entities;
using AgroUnion.Infrastructure.Persistence;
using AgroUnion.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgroUnion.Tests;

public sealed class PartnerMarketplaceTests
{
    [Fact]
    public async Task Marketplace_ExposesOnlyUncommittedProductionToActivePartners()
    {
        await using var db = CreateDb();
        var production = await SeedNetworkAsync(db);
        var email = new RecordingEmailSender();
        var service = new PartnerMarketplaceService(db, email);

        var blocked = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveProductionListingAsync("producer", new(production.Id, 30.001m, 6.25m)));
        Assert.Contains("δεσμευμένη", blocked.Message, StringComparison.OrdinalIgnoreCase);

        await service.SaveProductionListingAsync("producer", new(production.Id, 30m, 6.25m));
        var view = await service.GetMarketplaceAsync("trader", null, null, "Ελαιόλαδο", null);

        var listing = Assert.Single(view.ProductionListings);
        Assert.Equal(30m, listing.AvailableQuantity);
        Assert.Equal("Παραγωγός Δικτύου", listing.ProducerName);
        Assert.DoesNotContain(view.Partners, x => x.Name == "Εξωτερικός χρήστης");
        Assert.DoesNotContain(view.Partners, x => x.Name == "Ανενεργός συνεργάτης");

        await service.SendInquiryAsync("trader", new(listing.Id, null, null, 10m, 6.30m, "Ενδιαφέρομαι για άμεση παραλαβή."));
        Assert.Single(await db.PartnerMarketplaceInquiries.ToListAsync());
        Assert.Single(await db.Notifications.Where(x => x.UserId == "producer").ToListAsync());
        Assert.Equal("producer@example.gr", email.Recipient);
    }

    private static AgroUnionDbContext CreateDb() => new(new DbContextOptionsBuilder<AgroUnionDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<ProductionDeclaration> SeedNetworkAsync(AgroUnionDbContext db)
    {
        var producerRole = new IdentityRole(RoleNames.Producer) { Id = "role-producer" };
        var traderRole = new IdentityRole(RoleNames.Trader) { Id = "role-trader" };
        db.Roles.AddRange(producerRole, traderRole);
        db.Users.AddRange(
            new ApplicationUser { Id = "producer", UserName = "producer@example.gr", Email = "producer@example.gr", FullNameOrCompany = "Παραγωγός Δικτύου", Region = "Αιτωλικό", IsActive = true },
            new ApplicationUser { Id = "trader", UserName = "trader@example.gr", Email = "trader@example.gr", FullNameOrCompany = "Έμπορος Δικτύου", Region = "Αγρίνιο", IsActive = true },
            new ApplicationUser { Id = "outsider", UserName = "outside@example.gr", Email = "outside@example.gr", FullNameOrCompany = "Εξωτερικός χρήστης", Region = "Πάτρα", IsActive = true },
            new ApplicationUser { Id = "inactive", UserName = "inactive@example.gr", Email = "inactive@example.gr", FullNameOrCompany = "Ανενεργός συνεργάτης", Region = "Ναύπακτος", IsActive = false });
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = "producer", RoleId = producerRole.Id },
            new IdentityUserRole<string> { UserId = "trader", RoleId = traderRole.Id },
            new IdentityUserRole<string> { UserId = "inactive", RoleId = producerRole.Id });
        var production = new ProductionDeclaration { ProducerUserId = "producer", Product = "Ελαιόλαδο", Quantity = 100m, Unit = "kg", QualityGrade = "Extra παρθένο", Region = "Αιτωλικό", AvailableFrom = DateOnly.FromDateTime(DateTime.Today), AvailableTo = DateOnly.FromDateTime(DateTime.Today.AddMonths(2)) };
        db.ProductionDeclarations.Add(production);
        db.Deals.Add(new Deal { ProductionDeclarationId = production.Id, FarmerUserId = "producer", BuyerCounterpartyUserId = "trader", BuyQuantity = 70m, SellQuantity = 70m, BuyPricePerUnit = 6m, SellPricePerUnit = 6.4m, Status = DealStatus.BuySideConfirmed });
        await db.SaveChangesAsync();
        return production;
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public string? Recipient { get; private set; }
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            Recipient = to;
            return Task.CompletedTask;
        }
    }
}
