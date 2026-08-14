using AgroUnion.Domain.Entities;
using AgroUnion.Domain.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgroUnion.Infrastructure.Persistence;

public sealed class DatabaseSeeder(AgroUnionDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, IConfiguration configuration)
{
    private const string SeedPasswordVersionClaim = "agrounion:seed-password-version";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        foreach (var role in RoleNames.All)
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));

        var passwordVersion = configuration["SeedData:PasswordVersion"];
        var admin = await EnsureUser("admin@agrounion.local", "Διαχειριστής AGRO UNION", "Μεσολόγγι", RoleNames.Admin, configuration["SeedData:AdminPassword"] ?? "Admin!2026Demo", passwordVersion);
        var producer = await EnsureUser("producer@agrounion.local", "Ελαιοπαραγωγός Δημήτρης Νικολάου", "Αιτωλικό", RoleNames.Producer, configuration["SeedData:DemoPassword"] ?? "Demo!2026User", passwordVersion);
        var producer2 = await EnsureUser("producer2@agrounion.local", "Αγρόκτημα Μακρή", "Πεντάλοφος", RoleNames.Producer, configuration["SeedData:DemoPassword"] ?? "Demo!2026User", passwordVersion);
        var trader = await EnsureUser("trader@agrounion.local", "Εμπορική Καρπών Δυτικής Ελλάδας", "Αγρίνιο", RoleNames.Trader, configuration["SeedData:DemoPassword"] ?? "Demo!2026User", passwordVersion);
        var company = await EnsureUser("company@agrounion.local", "Τυποποιητική Αιτωλίας Α.Ε.", "Μεσολόγγι", RoleNames.Company, configuration["SeedData:DemoPassword"] ?? "Demo!2026User", passwordVersion);

        if (await db.ProductionDeclarations.AnyAsync(ct))
        {
            await EnsureProducerDeliveryAsync(producer, admin, ct);
            return;
        }
        var oliveOil = new ProductionDeclaration { ProducerUserId = producer.Id, Product = "Ελαιόλαδο", Quantity = 12800, Unit = "kg", QualityGrade = "Έξτρα παρθένο", Region = "Αιτωλικό", AvailableFrom = new(2026, 10, 15), AvailableTo = new(2027, 2, 28) };
        var olives = new ProductionDeclaration { ProducerUserId = producer2.Id, Product = "Επιτραπέζια ελιά", Quantity = 22400, Unit = "kg", QualityGrade = "Καλαμών Α", Region = "Πεντάλοφος", AvailableFrom = new(2026, 9, 20), AvailableTo = new(2026, 12, 15) };
        db.ProductionDeclarations.AddRange(oliveOil, olives);
        db.Contracts.AddRange(
            ContractFor(producer.Id, PartnerRole.Producer, "AU-2026-0101"), ContractFor(producer2.Id, PartnerRole.Producer, "AU-2026-0102"),
            ContractFor(trader.Id, PartnerRole.Trader, "AU-2026-0201"), ContractFor(company.Id, PartnerRole.Company, "AU-2026-0301"));
        db.PurchaseOffers.Add(new PurchaseOffer { ProducerUserId = producer.Id, Product = "Ελαιόλαδο", BuyPricePerUnit = 6.42m, TargetQuantity = 8000, Region = "Αιτωλικό", ValidUntil = DateTime.UtcNow.AddDays(20), CreatedByUserId = admin.Id });
        db.SellOffers.AddRange(
            new SellOffer { BuyerUserId = trader.Id, Product = "Επιτραπέζια ελιά", SellPricePerUnit = 2.18m, TargetQuantity = 12000, Region = "Πεντάλοφος", ValidUntil = DateTime.UtcNow.AddDays(14), CreatedByUserId = admin.Id },
            new SellOffer { BuyerUserId = company.Id, Product = "Ελαιόλαδο", SellPricePerUnit = 7.14m, TargetQuantity = 8000, Region = "Αιτωλικό", ValidUntil = DateTime.UtcNow.AddDays(20), CreatedByUserId = admin.Id });
        var completedDeal = new Deal { DealType = DealType.Brokerage, ProductionDeclarationId = oliveOil.Id, FarmerUserId = producer.Id, BuyPricePerUnit = 6.10m, BuyQuantity = 4200, BuyerCounterpartyUserId = company.Id, SellPricePerUnit = 6.86m, SellQuantity = 4200, Status = DealStatus.Completed, CreatedAt = DateTime.UtcNow.AddDays(-34), CompletedAt = DateTime.UtcNow.AddDays(-28) };
        db.Deals.Add(completedDeal);
        db.Transactions.AddRange(
            new Transaction { UserId = producer.Id, DealId = completedDeal.Id, Side = TransactionSide.Purchase, Product = "Ελαιόλαδο", Quantity = 4200, UnitPrice = 6.10m, Region = "Αιτωλικό", TransactionDate = DateTime.UtcNow.AddDays(-28) },
            new Transaction { UserId = company.Id, DealId = completedDeal.Id, Side = TransactionSide.Sale, Product = "Ελαιόλαδο", Quantity = 4200, UnitPrice = 6.86m, Region = "Αιτωλικό", TransactionDate = DateTime.UtcNow.AddDays(-28) });
        db.PickupSchedules.Add(new PickupSchedule { DealId = completedDeal.Id, ScheduledDate = DateTime.UtcNow.AddDays(4), TransportDetails = "Φορτηγό ψυγείο · επιβεβαίωση ώρας 24 ώρες πριν." });
        db.SupplyOrders.Add(new SupplyOrder { Title = "Συλλογική παραγγελία λιπάσματος 20-10-10", Product = "Λίπασμα 20-10-10", Description = "Παλέτες 1.000 kg με παράδοση σε κεντρικό σημείο Μεσολογγίου.", DeadlineDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)) });
        db.PriceListItems.AddRange(
            new PriceListItem { PublishedByUserId = company.Id, Category = PriceCategory.Supply, ProductName = "Λίπασμα 20-10-10", Price = 0.68m, Unit = "kg", EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow), EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), VisibleToRoles = $"{RoleNames.Producer},{RoleNames.Trader}" },
            new PriceListItem { PublishedByUserId = admin.Id, Category = PriceCategory.Product, ProductName = "Ελαιόλαδο έξτρα παρθένο", Price = 7.14m, Unit = "kg", EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow), VisibleToRoles = $"{RoleNames.Trader},{RoleNames.Company}" });
        db.InterestApplications.Add(new InterestApplication { Role = PartnerRole.Producer, FullNameOrCompany = "Γεώργιος Παπαδάκης", Region = "Αμφιλοχία", ProductInterest = "Άλλος καρπός", Phone = "+30 690 000 0000", Email = "pending@example.com", Message = "Ενδιαφέρομαι για ένταξη του αμπελώνα μου στο δίκτυο." });
        db.ContactMessages.Add(new ContactMessage { FullName = "Μαρία Κωνσταντίνου", Email = "maria@example.com", Message = "Θα ήθελα πληροφορίες για τις συλλογικές παραγγελίες εφοδίων." });
        db.ProducerCollaborationProfiles.AddRange(
            new ProducerCollaborationProfile
            {
                ProducerUserId = producer.Id, Category = ProducerCategory.Advanced, NextCategory = ProducerCategory.Premium,
                CategoryProgressPercent = 72, UpgradeRequirements = "Συνέπεια ποιότητας σε 2 διαδοχικές παραλαβές και ολοκλήρωση της πιστοποίησης ολοκληρωμένης διαχείρισης.",
                CommissionRate = 2.25m, BonusRate = 1.50m, RelationshipStartDate = new DateOnly(2024, 3, 18),
                AccountManager = "Ελένη Παπαδοπούλου", PaymentTerms = "Εξόφληση εντός 30 ημερών από την έκδοση τιμολογίου.",
                InternalNotes = "Σταθερή ποιότητα και άμεση ανταπόκριση στις παραλαβές.", UpdatedByUserId = admin.Id
            },
            new ProducerCollaborationProfile
            {
                ProducerUserId = producer2.Id, Category = ProducerCategory.Standard, NextCategory = ProducerCategory.Advanced,
                CategoryProgressPercent = 45, UpgradeRequirements = "Πλήρης φάκελος πιστοποιήσεων και συνέπεια ποιότητας στις παραλαβές.",
                CommissionRate = 1.50m, BonusRate = 0.50m, RelationshipStartDate = new DateOnly(2025, 1, 10),
                AccountManager = "Ελένη Παπαδοπούλου", PaymentTerms = "Εξόφληση εντός 45 ημερών.", UpdatedByUserId = admin.Id
            });
        db.PartnerDocuments.AddRange(
            new PartnerDocument { UserId = producer.Id, Type = PartnerDocumentType.Certification, Title = "Πιστοποίηση ποιότητας ελαιολάδου", ReferenceNumber = "CERT-OL-2026-18", IssueDate = new DateOnly(2026, 2, 1), ExpiryDate = new DateOnly(2027, 2, 1), Notes = "Ενεργή πιστοποίηση για την τρέχουσα παραγωγική περίοδο.", IsVisibleToPartner = true, CreatedByUserId = admin.Id },
            new PartnerDocument { UserId = producer.Id, Type = PartnerDocumentType.Statement, Title = "Ετήσια καρτέλα συνεργασίας 2025", ReferenceNumber = "STAT-2025-0101", IssueDate = new DateOnly(2026, 1, 20), Notes = "Συγκεντρωτική κατάσταση παραδόσεων και πληρωμών.", IsVisibleToPartner = true, CreatedByUserId = admin.Id });
        var producerInvoice = new PartnerInvoice { UserId = producer.Id, Direction = PartnerInvoiceDirection.FromProducer, InvoiceNumber = "ΤΠΥ-184/2026", IssueDate = new DateOnly(2026, 7, 8), DueDate = new DateOnly(2026, 8, 7), NetAmount = 25620m, VatAmount = 3329.60m, PaidAmount = 28949.60m, Status = PartnerInvoiceStatus.Paid, Description = "Παράδοση ελαιολάδου παρτίδας AU-LOT-071", CreatedByUserId = admin.Id };
        var agroInvoice = new PartnerInvoice { UserId = producer.Id, Direction = PartnerInvoiceDirection.FromAgroUnion, InvoiceNumber = "AU-EP-2026-044", IssueDate = new DateOnly(2026, 7, 25), DueDate = new DateOnly(2026, 8, 24), NetAmount = 780m, VatAmount = 187.20m, PaidAmount = 0m, Status = PartnerInvoiceStatus.Issued, Description = "Εφόδια και υπηρεσία μεταφοράς", CreatedByUserId = admin.Id };
        db.PartnerInvoices.AddRange(producerInvoice, agroInvoice);
        db.PartnerFinancialEntries.AddRange(
            new PartnerFinancialEntry { UserId = producer.Id, EntryDate = new DateOnly(2026, 7, 12), Type = FinancialEntryType.Income, Category = FinancialEntryCategory.Bonus, Amount = 384.30m, Description = "Bonus ποιότητας παρτίδας AU-LOT-071", ReferenceNumber = "BONUS-071", PartnerInvoiceId = producerInvoice.Id, CreatedByUserId = admin.Id },
            new PartnerFinancialEntry { UserId = producer.Id, EntryDate = new DateOnly(2026, 7, 25), Type = FinancialEntryType.Expense, Category = FinancialEntryCategory.Supplies, Amount = 967.20m, Description = "Εφόδια και υπηρεσία μεταφοράς", ReferenceNumber = agroInvoice.InvoiceNumber, PartnerInvoiceId = agroInvoice.Id, CreatedByUserId = admin.Id },
            new PartnerFinancialEntry { UserId = producer.Id, EntryDate = new DateOnly(2026, 6, 28), Type = FinancialEntryType.Expense, Category = FinancialEntryCategory.Commission, Amount = 576.45m, Description = "Προμήθεια διαχείρισης και εμπορικής διάθεσης", ReferenceNumber = "COM-06/2026", CreatedByUserId = admin.Id });
        await db.SaveChangesAsync(ct);
        await EnsureProducerDeliveryAsync(producer, admin, ct);
    }

    private async Task EnsureProducerDeliveryAsync(ApplicationUser producer, ApplicationUser admin, CancellationToken ct)
    {
        if (await db.ProducerDeliveryRecords.AnyAsync(x => x.ProducerUserId == producer.Id, ct)) return;

        var production = await db.ProductionDeclarations.FirstOrDefaultAsync(x => x.ProducerUserId == producer.Id, ct);
        var deal = await db.Deals.FirstOrDefaultAsync(x => x.FarmerUserId == producer.Id && x.Status == DealStatus.Completed, ct);
        var contract = await db.Contracts.FirstOrDefaultAsync(x => x.UserId == producer.Id && x.Status == ContractStatus.Active, ct);
        if (production is null || deal is null) return;

        var settlement = DeliverySettlementCalculator.Calculate(
            grossWeight: 8230m,
            tareWeight: 3980m,
            rejectedWeight: 50m,
            unitPrice: 6.10m,
            bonusPercent: 1.50m,
            commissionPercent: 2.25m,
            withholdingPercent: 0m,
            vatPercent: 0m,
            transportCost: 240m,
            otherDeductions: 67.20m);

        db.ProducerDeliveryRecords.Add(new ProducerDeliveryRecord
        {
            ProducerUserId = producer.Id,
            ProductionDeclarationId = production.Id,
            ContractId = contract?.Id,
            DealId = deal.Id,
            RouteNumber = "AU-ROUTE-2026-071",
            Status = DeliveryLogisticsStatus.Settled,
            Product = "Ελαιόλαδο",
            Variety = "Κορωνέικη",
            QualityGrade = "Έξτρα παρθένο · οξύτητα 0,31%",
            LotNumber = "AU-LOT-071",
            OriginAddress = "Ελαιοτριβείο Αιτωλικού · Θέση Κάμπος",
            DestinationAddress = "Κέντρο παραλαβής AGRO UNION · Μεσολόγγι",
            FactoryName = "Τυποποιητική Αιτωλίας Α.Ε.",
            ScheduledPickupAt = new DateTime(2026, 7, 4, 7, 30, 0, DateTimeKind.Utc),
            LoadedAt = new DateTime(2026, 7, 4, 7, 42, 0, DateTimeKind.Utc),
            DeliveredAt = new DateTime(2026, 7, 4, 9, 18, 0, DateTimeKind.Utc),
            CarrierName = "AGRO UNION Logistics",
            DriverName = "Νίκος Γεωργίου",
            VehiclePlate = "ΜΕΖ-4281",
            TrailerPlate = "ΜΕΖ-8912",
            GrossWeight = 8230m,
            TareWeight = 3980m,
            NetWeight = settlement.NetWeight,
            RejectedWeight = 50m,
            AcceptedWeight = settlement.AcceptedWeight,
            WeightUnit = "kg",
            WeighedAt = new DateTime(2026, 7, 4, 9, 11, 0, DateTimeKind.Utc),
            WeighbridgeName = "Γεφυροπλάστιγγα Κέντρου Μεσολογγίου 02",
            WeighingSlipNumber = "ΖΥΓ-2026-007184",
            DispatchNoteNumber = "ΔΑ-184/2026",
            DeliveryReceiptNumber = "ΠΑΡ-071/2026",
            AgreementReference = "AU-AGR-2026-071",
            AgreementType = PricingAgreementType.FixedPrice,
            UnitPrice = 6.10m,
            FactoryUnitPrice = 6.86m,
            QualityBonusPercent = 1.50m,
            CommissionPercent = 2.25m,
            WithholdingPercent = 0m,
            VatPercent = 0m,
            TransportCost = 240m,
            OtherDeductions = 67.20m,
            BaseAmount = settlement.BaseAmount,
            BonusAmount = settlement.BonusAmount,
            CommissionAmount = settlement.CommissionAmount,
            WithholdingAmount = settlement.WithholdingAmount,
            VatAmount = settlement.VatAmount,
            NetPayableAmount = settlement.NetPayableAmount,
            PaidAmount = 24153.45m,
            PaymentDueDate = new DateOnly(2026, 8, 7),
            PaymentStatus = DeliveryPaymentStatus.PartiallyPaid,
            AgreementNotes = "Σταθερή τιμή 6,100 €/kg στο αποδεκτό καθαρό βάρος. Bonus ποιότητας 1,50% και προμήθεια διαχείρισης 2,25%.",
            LoadNotes = "Παραλαβή σε ανοξείδωτη δεξαμενή. Απόρριψη 50 kg μετά τον ποιοτικό έλεγχο. Σφραγίδα φορτίου AU-S-071.",
            InternalNotes = "Έχει ελεγχθεί η συμφωνία, η ζύγιση και η αντιστοίχιση με το δελτίο αποστολής.",
            IsVisibleToProducer = true,
            CreatedAt = new DateTime(2026, 7, 4, 9, 22, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 7, 12, 12, 30, 0, DateTimeKind.Utc),
            CreatedByUserId = admin.Id,
            UpdatedByUserId = admin.Id
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task<ApplicationUser> EnsureUser(string email, string name, string region, string role, string password, string? passwordVersion)
    {
        var user = await users.FindByEmailAsync(email);
        var created = user is null;
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullNameOrCompany = name, Region = region };
            var result = await users.CreateAsync(user, password);
            EnsureSucceeded(result);
        }

        if (!string.IsNullOrWhiteSpace(passwordVersion))
        {
            var claims = await users.GetClaimsAsync(user);
            var versionClaim = claims.SingleOrDefault(x => x.Type == SeedPasswordVersionClaim);
            if (!created && versionClaim?.Value != passwordVersion)
            {
                var resetToken = await users.GeneratePasswordResetTokenAsync(user);
                EnsureSucceeded(await users.ResetPasswordAsync(user, resetToken, password));
            }

            if (versionClaim is null)
                EnsureSucceeded(await users.AddClaimAsync(user, new Claim(SeedPasswordVersionClaim, passwordVersion)));
            else if (versionClaim.Value != passwordVersion)
                EnsureSucceeded(await users.ReplaceClaimAsync(user, versionClaim, new Claim(SeedPasswordVersionClaim, passwordVersion)));
        }
        if (!await users.IsInRoleAsync(user, role)) await users.AddToRoleAsync(user, role);
        return user;
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }

    private static Contract ContractFor(string userId, PartnerRole role, string number) => new()
    {
        UserId = userId, ContractNumber = number, PartyRole = role, Subject = role == PartnerRole.Company ? ContractSubject.Supply : ContractSubject.Sale,
        DurationType = DurationType.Indefinite, Status = ContractStatus.Active, StartDate = new DateOnly(2026, 1, 1),
        PricingTerms = "Οι τιμές συμφωνούνται ανά συναλλαγή.", QuantityTerms = "Οι ποσότητες οριστικοποιούνται με αποδοχή προσφοράς.", TerminationTerms = "Έγγραφη προειδοποίηση 30 ημερών."
    };
}
