using System.Globalization;
using System.Text;
using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Domain.Entities;
using AgroUnion.Domain.Services;
using AgroUnion.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgroUnion.Infrastructure.Services;

public sealed class AgroUnionService(
    AgroUnionDbContext db,
    UserManager<ApplicationUser> users,
    IEmailSender emailSender,
    IConfiguration configuration,
    IValidator<InterestApplicationRequest> interestValidator,
    IValidator<ContactRequest> contactValidator,
    IValidator<ProductionRequest> productionValidator,
    IValidator<CounterOfferRequest> counterValidator) : IAgroUnionService
{
    public async Task<Guid> SubmitInterestAsync(InterestApplicationRequest request, CancellationToken ct = default)
    {
        await interestValidator.ValidateAndThrowAsync(request, ct);
        var item = new InterestApplication
        {
            Role = request.Role, FullNameOrCompany = request.FullNameOrCompany.Trim(), Region = request.Region.Trim(),
            ProductInterest = request.ProductInterest.Trim(), Phone = request.Phone.Trim(), Email = request.Email.Trim().ToLowerInvariant(), Message = request.Message.Trim()
        };
        db.InterestApplications.Add(item);
        await db.SaveChangesAsync(ct);
        var adminEmail = configuration["Notifications:AdminEmail"] ?? "info@agro-union.gr";
        await emailSender.SendAsync(adminEmail, "Νέα αίτηση συνεργασίας", $"Νέα αίτηση από <strong>{item.FullNameOrCompany}</strong> ({item.Email}).", ct);
        await emailSender.SendAsync(item.Email, "Λάβαμε την αίτησή σας", "Σας ευχαριστούμε. Η ομάδα της AGRO UNION θα επικοινωνήσει σύντομα μαζί σας.", ct);
        return item.Id;
    }

    public async Task<Guid> SubmitContactAsync(ContactRequest request, CancellationToken ct = default)
    {
        await contactValidator.ValidateAndThrowAsync(request, ct);
        var item = new ContactMessage { FullName = request.FullName.Trim(), Email = request.Email.Trim().ToLowerInvariant(), Message = request.Message.Trim() };
        db.ContactMessages.Add(item);
        await db.SaveChangesAsync(ct);
        await emailSender.SendAsync(configuration["Notifications:AdminEmail"] ?? "info@agro-union.gr", "Νέο μήνυμα επικοινωνίας", $"Μήνυμα από <strong>{item.FullName}</strong> ({item.Email}).", ct);
        return item.Id;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(string? producerUserId = null, CancellationToken ct = default)
    {
        var userRows = await users.Users.OrderBy(x => x.FullNameOrCompany).ToListAsync(ct);
        var summaries = new List<UserSummary>();
        foreach (var user in userRows)
        {
            var role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "—";
            summaries.Add(new(user.Id, user.FullNameOrCompany, user.Email ?? "", role, user.Region, user.IsActive));
        }
        var deals = await db.Deals.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var declarations = await db.ProductionDeclarations.ToDictionaryAsync(x => x.Id, ct);
        var dealDtos = deals.Select(x => new AdminDealDto(x.Id, x.DealType, x.ProductionDeclarationId is { } pid && declarations.TryGetValue(pid, out var p) ? p.Product : "—", x.BuyPricePerUnit, x.SellPricePerUnit, Math.Min(x.BuyQuantity, x.SellQuantity), x.MarginPerUnit, x.TotalMargin, x.Status, x.CreatedAt)).ToList();
        var userNames = userRows.ToDictionary(x => x.Id, x => x.FullNameOrCompany);
        var productionDtos = declarations.Values.OrderByDescending(x => x.CreatedAt).Select(x => new AdminProductionDto(x.Id, userNames.GetValueOrDefault(x.ProducerUserId, "—"), x.Product, x.Quantity, x.Unit, x.QualityGrade, x.Region, x.Status)).ToList();
        var purchaseRows = await db.PurchaseOffers.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var sellRows = await db.SellOffers.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var offerDtos = purchaseRows.Select(x => new AdminOfferDto(x.Id, "Αγορά", userNames.GetValueOrDefault(x.ProducerUserId, "—"), x.Product, x.BuyPricePerUnit, x.TargetQuantity, x.Region, x.Status))
            .Concat(sellRows.Select(x => new AdminOfferDto(x.Id, "Πώληση", userNames.GetValueOrDefault(x.BuyerUserId, "—"), x.Product, x.SellPricePerUnit, x.TargetQuantity, x.Region, x.Status))).ToList();
        var contractRows = await db.Contracts.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var contractDtos = contractRows.Select(x => new AdminContractDto(x.Id, x.ContractNumber, userNames.GetValueOrDefault(x.UserId, "—"), x.Subject, x.Status, x.StartDate)).ToList();
        var producerSummaries = summaries.Where(x => x.Role == RoleNames.Producer).ToList();
        var selectedProducer = producerSummaries.FirstOrDefault(x => x.Id == producerUserId) ?? producerSummaries.FirstOrDefault();
        var producerWorkspace = selectedProducer is null
            ? EmptyProducerWorkspace(producerSummaries)
            : await BuildProducerWorkspaceAsync(selectedProducer, producerSummaries, true, ct);
        return new AdminDashboardDto(
            userRows.Count(x => x.IsActive) - 1,
            await db.InterestApplications.CountAsync(x => x.Status == ApplicationStatus.New || x.Status == ApplicationStatus.InReview, ct),
            await db.ProductionDeclarations.Where(x => x.Status == ProductionStatus.Available).SumAsync(x => (decimal?)x.Quantity, ct) ?? 0,
            await db.Contracts.CountAsync(x => x.Status == ContractStatus.Active, ct),
            dealDtos.Where(x => x.Status != DealStatus.Cancelled).Sum(x => x.TotalMargin),
            await db.InterestApplications.OrderByDescending(x => x.CreatedAt).Take(30).ToListAsync(ct),
            dealDtos, summaries,
            await db.ContactMessages.OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync(ct),
            await VisiblePriceItems(RoleNames.Admin, ct),
            await SupplyOrdersFor(null, ct), productionDtos, offerDtos, contractDtos, producerWorkspace);
    }

    public async Task<ProducerDashboardDto> GetProducerDashboardAsync(string userId, CancellationToken ct = default)
    {
        var declarations = await db.ProductionDeclarations.Where(x => x.ProducerUserId == userId).OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProductionSummary(x.Id, x.Product, x.Quantity, x.Unit, x.QualityGrade, x.Region, x.AvailableFrom, x.AvailableTo, x.Status)).ToListAsync(ct);
        var offers = await db.PurchaseOffers.Where(x => x.ProducerUserId == userId && x.ValidUntil >= DateTime.UtcNow && x.Status == OfferStatus.Active)
            .Select(x => new PurchaseOfferDto(x.Id, x.Product, x.BuyPricePerUnit, x.TargetQuantity, x.Region, x.ValidUntil, x.Status)).ToListAsync(ct);
        var deals = await (from d in db.Deals where d.FarmerUserId == userId join p in db.ProductionDeclarations on d.ProductionDeclarationId equals p.Id into ps from p in ps.DefaultIfEmpty()
            select new FarmerDealDto(d.Id, p == null ? "—" : p.Product, d.BuyPricePerUnit, d.BuyQuantity, d.Status, d.CreatedAt)).ToListAsync(ct);
        var user = await users.FindByIdAsync(userId) ?? throw new KeyNotFoundException("Ο λογαριασμός δεν βρέθηκε.");
        var summary = new UserSummary(user.Id, user.FullNameOrCompany, user.Email ?? string.Empty, RoleNames.Producer, user.Region, user.IsActive);
        var workspace = await BuildProducerWorkspaceAsync(summary, [], false, ct);
        return new(declarations, offers, workspace.Contracts, await TransactionsFor(userId, ct), await SupplyOrdersFor(userId, ct), await VisiblePriceItems(RoleNames.Producer, ct), deals,
            workspace.Profile!, workspace.Summary, workspace.Documents, workspace.Invoices, workspace.FinancialEntries, workspace.DeliveryRecords, workspace.LogisticsSummary);
    }

    public async Task<BuyerDashboardDto> GetBuyerDashboardAsync(string userId, bool isCompany, CancellationToken ct = default)
    {
        var volumes = await db.ProductionDeclarations.Where(x => x.Status == ProductionStatus.Available)
            .GroupBy(x => new { x.Product, x.Region, x.Unit }).Select(g => new VolumeSummary(g.Key.Product, g.Key.Region, g.Sum(x => x.Quantity), g.Key.Unit)).ToListAsync(ct);
        var offers = await db.SellOffers.Where(x => x.BuyerUserId == userId && x.ValidUntil >= DateTime.UtcNow && x.Status != OfferStatus.Closed)
            .Select(x => new SellOfferDto(x.Id, x.Product, x.SellPricePerUnit, x.TargetQuantity, x.CounterPricePerUnit, x.RequestedQuantity, x.Region, x.ValidUntil, x.Status)).ToListAsync(ct);
        var deals = await (from d in db.Deals where d.BuyerCounterpartyUserId == userId join p in db.ProductionDeclarations on d.ProductionDeclarationId equals p.Id into ps from p in ps.DefaultIfEmpty()
            select new BuyerDealDto(d.Id, p == null ? "—" : p.Product, d.SellPricePerUnit, d.SellQuantity, d.Status, d.CreatedAt)).ToListAsync(ct);
        var dealIds = deals.Select(x => x.Id).ToArray();
        var pickups = await db.PickupSchedules.Where(x => dealIds.Contains(x.DealId)).Select(x => new PickupDto(x.Id, x.DealId, x.ScheduledDate, x.TransportDetails, x.Status)).ToListAsync(ct);
        return new(volumes, offers, await ContractsFor(userId, ct), await TransactionsFor(userId, ct), await VisiblePriceItems(isCompany ? RoleNames.Company : RoleNames.Trader, ct), pickups, deals, isCompany);
    }

    public async Task<ApprovalResult> ApproveApplicationAsync(Guid id, string adminUserId, CancellationToken ct = default)
    {
        var application = await db.InterestApplications.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Η αίτηση δεν βρέθηκε.");
        if (application.Status == ApplicationStatus.Approved) throw new InvalidOperationException("Η αίτηση έχει ήδη εγκριθεί.");
        var existing = await users.FindByEmailAsync(application.Email);
        if (existing is not null) throw new InvalidOperationException("Υπάρχει ήδη λογαριασμός με αυτό το email.");
        var role = RoleNames.FromPartnerRole(application.Role);
        var user = new ApplicationUser { UserName = application.Email, Email = application.Email, EmailConfirmed = true, FullNameOrCompany = application.FullNameOrCompany, Region = application.Region };
        var password = $"Au!{Guid.NewGuid():N}"[..14];
        var create = await users.CreateAsync(user, password);
        if (!create.Succeeded) throw new InvalidOperationException(string.Join(" ", create.Errors.Select(x => x.Description)));
        await users.AddToRoleAsync(user, role);
        var contract = new Contract
        {
            UserId = user.Id, ContractNumber = $"AU-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999)}", PartyRole = application.Role,
            Subject = application.Role == PartnerRole.Company ? ContractSubject.Supply : ContractSubject.Sale, DurationType = DurationType.Indefinite,
            PricingTerms = "Οι τιμές συμφωνούνται ανά συναλλαγή και επιβεβαιώνονται εγγράφως.", QuantityTerms = "Οι ποσότητες οριστικοποιούνται ανά προσφορά.",
            TerminationTerms = "Δυνατότητα καταγγελίας με έγγραφη προειδοποίηση 30 ημερών.", StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        db.Contracts.Add(contract);
        application.Status = ApplicationStatus.Approved; application.HandledByUserId = adminUserId; application.HandledAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { UserId = adminUserId, Action = "Approve", EntityName = nameof(InterestApplication), EntityId = id.ToString(), Details = $"Δημιουργήθηκε χρήστης {user.Email} και σύμβαση {contract.ContractNumber}." });
        await db.SaveChangesAsync(ct);
        await emailSender.SendAsync(user.Email!, "Πρόσκληση στο Portal της AGRO UNION", $"Ο λογαριασμός σας ενεργοποιήθηκε. Προσωρινός κωδικός: <strong>{password}</strong>. Συνδεθείτε και αλλάξτε τον άμεσα.", ct);
        return new ApprovalResult(user.Id, user.Email!, password, contract.Id);
    }

    public async Task UpdateApplicationAsync(Guid id, ApplicationStatus status, string? notes, string adminUserId, CancellationToken ct = default)
    {
        var item = await db.InterestApplications.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Η αίτηση δεν βρέθηκε.");
        item.Status = status; item.InternalNotes = notes?.Trim(); item.HandledByUserId = adminUserId; item.HandledAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { UserId = adminUserId, Action = "StatusChange", EntityName = nameof(InterestApplication), EntityId = id.ToString(), Details = status.ToString() });
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> SaveProductionAsync(string producerUserId, Guid? id, ProductionRequest request, CancellationToken ct = default)
    {
        await productionValidator.ValidateAndThrowAsync(request, ct);
        var item = id is null ? new ProductionDeclaration { ProducerUserId = producerUserId } :
            await db.ProductionDeclarations.SingleOrDefaultAsync(x => x.Id == id && x.ProducerUserId == producerUserId, ct) ?? throw new KeyNotFoundException("Η δήλωση δεν βρέθηκε.");
        item.Product = request.Product.Trim(); item.Quantity = request.Quantity; item.Unit = request.Unit.Trim(); item.QualityGrade = request.QualityGrade.Trim(); item.Region = request.Region.Trim(); item.AvailableFrom = request.AvailableFrom; item.AvailableTo = request.AvailableTo; item.UpdatedAt = DateTime.UtcNow;
        if (id is null) db.ProductionDeclarations.Add(item);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task DeleteProductionAsync(string producerUserId, Guid id, CancellationToken ct = default)
    {
        var item = await db.ProductionDeclarations.SingleOrDefaultAsync(x => x.Id == id && x.ProducerUserId == producerUserId, ct) ?? throw new KeyNotFoundException("Η δήλωση δεν βρέθηκε.");
        if (item.Status != ProductionStatus.Available) throw new InvalidOperationException("Μόνο διαθέσιμες δηλώσεις μπορούν να διαγραφούν.");
        db.Remove(item); await db.SaveChangesAsync(ct);
    }

    public async Task SubmitCounterOfferAsync(string buyerUserId, Guid offerId, CounterOfferRequest request, CancellationToken ct = default)
    {
        await counterValidator.ValidateAndThrowAsync(request, ct);
        var offer = await db.SellOffers.SingleOrDefaultAsync(x => x.Id == offerId && x.BuyerUserId == buyerUserId && x.Status == OfferStatus.Active, ct) ?? throw new KeyNotFoundException("Η προσφορά δεν βρέθηκε.");
        offer.CounterPricePerUnit = request.PricePerUnit; offer.RequestedQuantity = request.Quantity; offer.BuyerRespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task JoinSupplyOrderAsync(string producerUserId, Guid orderId, SupplyParticipationRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0) throw new ValidationException("Η ποσότητα πρέπει να είναι θετική.");
        var order = await db.SupplyOrders.SingleOrDefaultAsync(x => x.Id == orderId && x.Status == SupplyOrderStatus.Open && x.DeadlineDate >= DateOnly.FromDateTime(DateTime.UtcNow), ct) ?? throw new KeyNotFoundException("Η παραγγελία δεν είναι διαθέσιμη.");
        var item = await db.SupplyOrderItems.SingleOrDefaultAsync(x => x.SupplyOrderId == orderId && x.ProducerUserId == producerUserId, ct);
        if (item is null) db.SupplyOrderItems.Add(new SupplyOrderItem { SupplyOrderId = order.Id, ProducerUserId = producerUserId, Quantity = request.Quantity });
        else item.Quantity = request.Quantity;
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> CreatePriceItemAsync(string publisherUserId, PriceListRequest request, CancellationToken ct = default)
    {
        if (request.Price <= 0 || string.IsNullOrWhiteSpace(request.ProductName)) throw new ValidationException("Συμπληρώστε έγκυρο προϊόν και τιμή.");
        var item = new PriceListItem { PublishedByUserId = publisherUserId, Category = request.Category, ProductName = request.ProductName.Trim(), Price = request.Price, Unit = request.Unit.Trim(), EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, VisibleToRoles = request.VisibleToRoles };
        db.PriceListItems.Add(item); await db.SaveChangesAsync(ct); return item.Id;
    }

    public async Task<Guid> CreateDealAsync(string adminUserId, DealRequest request, CancellationToken ct = default)
    {
        _ = DealRules.CalculateTotalMargin(request.DealType, request.BuyPricePerUnit, request.SellPricePerUnit, request.BuyQuantity, request.SellQuantity);
        var item = new Deal { DealType = request.DealType, ProductionDeclarationId = request.ProductionDeclarationId, FarmerUserId = request.FarmerUserId, BuyPricePerUnit = request.BuyPricePerUnit, BuyQuantity = request.BuyQuantity, BuyerCounterpartyUserId = request.BuyerUserId, SellPricePerUnit = request.SellPricePerUnit, SellQuantity = request.SellQuantity };
        db.Deals.Add(item); db.AuditLogs.Add(new AuditLog { UserId = adminUserId, Action = "Create", EntityName = nameof(Deal), EntityId = item.Id.ToString(), Details = $"{item.DealType}, margin {item.TotalMargin:N2}" });
        await db.SaveChangesAsync(ct); return item.Id;
    }

    public async Task ConfirmDealSideAsync(string userId, Guid dealId, bool buySide, bool isAdmin, CancellationToken ct = default)
    {
        var deal = await db.Deals.SingleOrDefaultAsync(x => x.Id == dealId, ct) ?? throw new KeyNotFoundException("Η συμφωνία δεν βρέθηκε.");
        if (!isAdmin && (buySide ? deal.FarmerUserId != userId : deal.BuyerCounterpartyUserId != userId)) throw new UnauthorizedAccessException("Δεν έχετε πρόσβαση σε αυτή τη συμφωνία.");
        deal.Status = buySide ? DealRules.ConfirmBuySide(deal.Status) : DealRules.ConfirmSellSide(deal.Status);
        if (deal.Status == DealStatus.Completed)
        {
            deal.CompletedAt = DateTime.UtcNow;
            var production = deal.ProductionDeclarationId is { } id ? await db.ProductionDeclarations.SingleOrDefaultAsync(x => x.Id == id, ct) : null;
            if (production is not null) production.Status = ProductionStatus.Sold;
            var product = production?.Product ?? "Αγροτικό προϊόν"; var region = production?.Region ?? "—";
            db.PickupSchedules.Add(new PickupSchedule { DealId = deal.Id, ScheduledDate = DateTime.UtcNow.AddDays(7), TransportDetails = "Η ώρα και το όχημα θα επιβεβαιωθούν από την ομάδα συντονισμού." });
            db.Transactions.AddRange(
                new Transaction { UserId = deal.FarmerUserId, DealId = deal.Id, Side = TransactionSide.Purchase, Product = product, Quantity = deal.BuyQuantity, UnitPrice = deal.BuyPricePerUnit, Region = region },
                new Transaction { UserId = deal.BuyerCounterpartyUserId, DealId = deal.Id, Side = TransactionSide.Sale, Product = product, Quantity = deal.SellQuantity, UnitPrice = deal.SellPricePerUnit, Region = region });
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> CreateSupplyOrderAsync(SupplyOrderRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Product) || request.DeadlineDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ValidationException("Συμπληρώστε τίτλο, προϊόν και μελλοντική προθεσμία.");
        var order = new SupplyOrder { Title = request.Title.Trim(), Product = request.Product.Trim(), Description = request.Description.Trim(), DeadlineDate = request.DeadlineDate };
        db.SupplyOrders.Add(order); await db.SaveChangesAsync(ct); return order.Id;
    }

    public async Task CloseSupplyOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await db.SupplyOrders.SingleOrDefaultAsync(x => x.Id == orderId, ct) ?? throw new KeyNotFoundException("Η συλλογική παραγγελία δεν βρέθηκε.");
        order.Status = SupplyOrderStatus.Closed; await db.SaveChangesAsync(ct);
    }

    public async Task ActivateContractAsync(Guid contractId, CancellationToken ct = default)
    {
        var contract = await db.Contracts.SingleOrDefaultAsync(x => x.Id == contractId, ct) ?? throw new KeyNotFoundException("Η σύμβαση δεν βρέθηκε.");
        if (contract.Status != ContractStatus.Draft) throw new InvalidOperationException("Μόνο προσχέδια μπορούν να ενεργοποιηθούν.");
        contract.Status = ContractStatus.Active;
        db.Notifications.Add(new Notification { UserId = contract.UserId, Title = "Η σύμβασή σας ενεργοποιήθηκε", Message = $"Η σύμβαση {contract.ContractNumber} είναι πλέον ενεργή." });
        await db.SaveChangesAsync(ct);
    }

    public async Task<string> ResetPasswordAsync(string userId, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new KeyNotFoundException("Ο χρήστης δεν βρέθηκε.");
        var password = $"Au!{Guid.NewGuid():N}"[..14];
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var result = await users.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
        await emailSender.SendAsync(user.Email!, "Νέος προσωρινός κωδικός", $"Ο νέος προσωρινός κωδικός σας είναι <strong>{password}</strong>.", ct);
        return password;
    }

    public async Task ChangeUserRoleAsync(string userId, string role, CancellationToken ct = default)
    {
        if (!new[] { RoleNames.Producer, RoleNames.Trader, RoleNames.Company }.Contains(role)) throw new ValidationException("Μη έγκυρος ρόλος συνεργάτη.");
        var user = await users.FindByIdAsync(userId) ?? throw new KeyNotFoundException("Ο χρήστης δεν βρέθηκε.");
        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current.Where(x => x != RoleNames.Admin));
        await users.AddToRoleAsync(user, role);
    }

    public async Task DeletePriceItemAsync(string requesterUserId, bool isAdmin, Guid id, CancellationToken ct = default)
    {
        var item = await db.PriceListItems.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Η τιμή δεν βρέθηκε.");
        if (!isAdmin && item.PublishedByUserId != requesterUserId) throw new UnauthorizedAccessException("Δεν μπορείτε να διαγράψετε αυτή την τιμή.");
        db.PriceListItems.Remove(item); await db.SaveChangesAsync(ct);
    }

    public async Task MarkContactReadAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.ContactMessages.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Το μήνυμα δεν βρέθηκε.");
        item.IsRead = true; await db.SaveChangesAsync(ct);
    }

    public async Task SchedulePickupAsync(string buyerUserId, Guid dealId, PickupRequest request, CancellationToken ct = default)
    {
        var deal = await db.Deals.SingleOrDefaultAsync(x => x.Id == dealId && x.BuyerCounterpartyUserId == buyerUserId && x.Status == DealStatus.Completed, ct) ?? throw new KeyNotFoundException("Δεν υπάρχει ολοκληρωμένη συμφωνία για αυτή την παραλαβή.");
        var pickup = await db.PickupSchedules.SingleOrDefaultAsync(x => x.DealId == dealId, ct);
        if (pickup is null) db.PickupSchedules.Add(new PickupSchedule { DealId = deal.Id, ScheduledDate = request.ScheduledDate, TransportDetails = request.TransportDetails.Trim() });
        else { pickup.ScheduledDate = request.ScheduledDate; pickup.TransportDetails = request.TransportDetails.Trim(); pickup.Status = PickupStatus.Scheduled; }
        await db.SaveChangesAsync(ct);
    }

    public async Task SetUserActiveAsync(string userId, bool active, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new KeyNotFoundException("Ο χρήστης δεν βρέθηκε.");
        user.IsActive = active; user.LockoutEnd = active ? null : DateTimeOffset.MaxValue; await users.UpdateAsync(user);
    }

    public async Task DeletePersonalDataAsync(string userId, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new KeyNotFoundException("Ο χρήστης δεν βρέθηκε.");
        var oldEmail = user.Email;
        user.FullNameOrCompany = "Διαγραμμένος χρήστης"; user.Region = "—"; user.PhoneNumber = null; user.Email = $"deleted-{Guid.NewGuid():N}@invalid.local"; user.UserName = user.Email; user.IsActive = false;
        await users.UpdateAsync(user);
        var messages = await db.ContactMessages.Where(x => x.Email == oldEmail).ToListAsync(ct); db.ContactMessages.RemoveRange(messages);
        await db.SaveChangesAsync(ct);
    }

    public async Task<string> ExportTransactionsCsvAsync(string userId, bool isAdmin, CancellationToken ct = default)
    {
        var query = db.Transactions.AsNoTracking(); if (!isAdmin) query = query.Where(x => x.UserId == userId);
        var rows = await query.OrderByDescending(x => x.TransactionDate).ToListAsync(ct);
        var sb = new StringBuilder("Ημερομηνία,Προϊόν,Σκέλος,Ποσότητα,Τιμή,Σύνολο,Περιοχή\r\n");
        foreach (var x in rows) sb.AppendLine($"{x.TransactionDate:yyyy-MM-dd},{Csv(x.Product)},{x.Side},{x.Quantity.ToString(CultureInfo.InvariantCulture)},{x.UnitPrice.ToString(CultureInfo.InvariantCulture)},{x.TotalValue.ToString(CultureInfo.InvariantCulture)},{Csv(x.Region)}");
        return sb.ToString();
    }

    public async Task<string> ExportMarginCsvAsync(CancellationToken ct = default)
    {
        var deals = await db.Deals.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var sb = new StringBuilder("Ημερομηνία,Τύπος,Τιμή αγοράς,Τιμή πώλησης,Ποσότητα,Περιθώριο\r\n");
        foreach (var x in deals) sb.AppendLine($"{x.CreatedAt:yyyy-MM-dd},{x.DealType},{x.BuyPricePerUnit.ToString(CultureInfo.InvariantCulture)},{x.SellPricePerUnit.ToString(CultureInfo.InvariantCulture)},{Math.Min(x.BuyQuantity, x.SellQuantity).ToString(CultureInfo.InvariantCulture)},{x.TotalMargin.ToString(CultureInfo.InvariantCulture)}");
        return sb.ToString();
    }

    public async Task SaveProducerProfileAsync(string producerUserId, ProducerProfileRequest request, string adminUserId, CancellationToken ct = default)
    {
        await EnsureProducerAsync(producerUserId);
        if (request.CategoryProgressPercent is < 0 or > 100) throw new ValidationException("Η πρόοδος κατηγορίας πρέπει να είναι από 0 έως 100%.");
        if (request.CommissionRate is < 0 or > 100 || request.BonusRate is < 0 or > 100) throw new ValidationException("Τα ποσοστά προμήθειας και bonus πρέπει να είναι από 0 έως 100%.");
        if (request.RelationshipStartDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))) throw new ValidationException("Η ημερομηνία έναρξης συνεργασίας δεν μπορεί να είναι μελλοντική.");
        var profile = await db.ProducerCollaborationProfiles.SingleOrDefaultAsync(x => x.ProducerUserId == producerUserId, ct);
        if (profile is null)
        {
            profile = new ProducerCollaborationProfile { ProducerUserId = producerUserId };
            db.ProducerCollaborationProfiles.Add(profile);
        }
        profile.Category = request.Category;
        profile.NextCategory = request.Category switch
        {
            ProducerCategory.Developing => ProducerCategory.Standard,
            ProducerCategory.Standard => ProducerCategory.Advanced,
            ProducerCategory.Advanced => ProducerCategory.Premium,
            ProducerCategory.Premium => ProducerCategory.Strategic,
            _ => null
        };
        profile.CategoryProgressPercent = request.CategoryProgressPercent;
        profile.UpgradeRequirements = request.UpgradeRequirements.Trim();
        profile.CommissionRate = request.CommissionRate;
        profile.BonusRate = request.BonusRate;
        profile.RelationshipStartDate = request.RelationshipStartDate;
        profile.AccountManager = request.AccountManager.Trim();
        profile.PaymentTerms = request.PaymentTerms.Trim();
        profile.InternalNotes = CleanOptional(request.InternalNotes);
        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByUserId = adminUserId;
        AddAudit(adminUserId, "Update", nameof(ProducerCollaborationProfile), profile.Id, $"Producer {producerUserId}, category {profile.Category}");
        db.Notifications.Add(new Notification { UserId = producerUserId, Title = "Ενημέρωση όρων συνεργασίας", Message = "Ο ατομικός φάκελος συνεργασίας και η κατηγορία σας ενημερώθηκαν." });
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> AddPartnerDocumentAsync(string producerUserId, PartnerDocumentRequest request, string adminUserId, CancellationToken ct = default)
    {
        await EnsureProducerAsync(producerUserId);
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.ReferenceNumber)) throw new ValidationException("Συμπληρώστε τίτλο και αριθμό αναφοράς εγγράφου.");
        if (request.ExpiryDate < request.IssueDate) throw new ValidationException("Η λήξη του εγγράφου δεν μπορεί να προηγείται της έκδοσης.");
        ValidateFileUrl(request.FileUrl);
        var item = new PartnerDocument
        {
            UserId = producerUserId, Type = request.Type, Title = request.Title.Trim(), ReferenceNumber = request.ReferenceNumber.Trim(),
            FileUrl = CleanOptional(request.FileUrl), IssueDate = request.IssueDate, ExpiryDate = request.ExpiryDate,
            Notes = CleanOptional(request.Notes), IsVisibleToPartner = request.IsVisibleToPartner, CreatedByUserId = adminUserId
        };
        db.PartnerDocuments.Add(item);
        AddAudit(adminUserId, "Create", nameof(PartnerDocument), item.Id, $"Producer {producerUserId}, {item.ReferenceNumber}");
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task DeletePartnerDocumentAsync(Guid id, string adminUserId, CancellationToken ct = default)
    {
        var item = await db.PartnerDocuments.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Το έγγραφο δεν βρέθηκε.");
        db.PartnerDocuments.Remove(item);
        AddAudit(adminUserId, "Delete", nameof(PartnerDocument), item.Id, item.ReferenceNumber);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> AddPartnerInvoiceAsync(string producerUserId, PartnerInvoiceRequest request, string adminUserId, CancellationToken ct = default)
    {
        await EnsureProducerAsync(producerUserId);
        if (string.IsNullOrWhiteSpace(request.InvoiceNumber) || request.NetAmount < 0 || request.VatAmount < 0 || request.PaidAmount < 0) throw new ValidationException("Συμπληρώστε έγκυρο αριθμό και μη αρνητικά ποσά τιμολογίου.");
        if (request.DueDate < request.IssueDate) throw new ValidationException("Η ημερομηνία λήξης δεν μπορεί να προηγείται της έκδοσης.");
        if (request.PaidAmount > request.NetAmount + request.VatAmount) throw new ValidationException("Το εξοφλημένο ποσό δεν μπορεί να υπερβαίνει τη συνολική αξία.");
        ValidateFileUrl(request.FileUrl);
        var item = new PartnerInvoice
        {
            UserId = producerUserId, Direction = request.Direction, InvoiceNumber = request.InvoiceNumber.Trim(), IssueDate = request.IssueDate,
            DueDate = request.DueDate, NetAmount = request.NetAmount, VatAmount = request.VatAmount, PaidAmount = request.PaidAmount,
            Status = request.Status, Description = request.Description.Trim(), FileUrl = CleanOptional(request.FileUrl), CreatedByUserId = adminUserId
        };
        db.PartnerInvoices.Add(item);
        AddAudit(adminUserId, "Create", nameof(PartnerInvoice), item.Id, $"Producer {producerUserId}, {item.InvoiceNumber}, {item.TotalAmount:N2}");
        db.Notifications.Add(new Notification { UserId = producerUserId, Title = "Νέο τιμολόγιο στον φάκελό σας", Message = $"Καταχωρίστηκε το τιμολόγιο {item.InvoiceNumber}." });
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task DeletePartnerInvoiceAsync(Guid id, string adminUserId, CancellationToken ct = default)
    {
        var item = await db.PartnerInvoices.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Το τιμολόγιο δεν βρέθηκε.");
        db.PartnerInvoices.Remove(item);
        AddAudit(adminUserId, "Delete", nameof(PartnerInvoice), item.Id, item.InvoiceNumber);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> AddPartnerFinancialEntryAsync(string producerUserId, PartnerFinancialEntryRequest request, string adminUserId, CancellationToken ct = default)
    {
        await EnsureProducerAsync(producerUserId);
        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Description)) throw new ValidationException("Συμπληρώστε θετικό ποσό και περιγραφή οικονομικής κίνησης.");
        if (request.PartnerInvoiceId is { } invoiceId && !await db.PartnerInvoices.AnyAsync(x => x.Id == invoiceId && x.UserId == producerUserId, ct)) throw new ValidationException("Το συνδεδεμένο τιμολόγιο δεν ανήκει στον συγκεκριμένο παραγωγό.");
        var item = new PartnerFinancialEntry
        {
            UserId = producerUserId, EntryDate = request.EntryDate, Type = request.Type, Category = request.Category,
            Amount = request.Amount, Description = request.Description.Trim(), ReferenceNumber = CleanOptional(request.ReferenceNumber),
            PartnerInvoiceId = request.PartnerInvoiceId, CreatedByUserId = adminUserId
        };
        db.PartnerFinancialEntries.Add(item);
        AddAudit(adminUserId, "Create", nameof(PartnerFinancialEntry), item.Id, $"Producer {producerUserId}, {item.Type}, {item.Amount:N2}");
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task DeletePartnerFinancialEntryAsync(Guid id, string adminUserId, CancellationToken ct = default)
    {
        var item = await db.PartnerFinancialEntries.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Η οικονομική κίνηση δεν βρέθηκε.");
        db.PartnerFinancialEntries.Remove(item);
        AddAudit(adminUserId, "Delete", nameof(PartnerFinancialEntry), item.Id, item.Description);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> SaveProducerDeliveryAsync(string producerUserId, Guid? id, ProducerDeliveryRequest request, string adminUserId, CancellationToken ct = default)
    {
        await EnsureProducerAsync(producerUserId);
        if (string.IsNullOrWhiteSpace(request.RouteNumber) || string.IsNullOrWhiteSpace(request.Product) ||
            string.IsNullOrWhiteSpace(request.OriginAddress) || string.IsNullOrWhiteSpace(request.DestinationAddress) || string.IsNullOrWhiteSpace(request.FactoryName) ||
            string.IsNullOrWhiteSpace(request.AgreementReference))
            throw new ValidationException("Συμπληρώστε αριθμό δρομολογίου, προϊόν, αφετηρία, εργοστάσιο, προορισμό και αναφορά συμφωνίας.");
        if (request.ScheduledPickupAt == default) throw new ValidationException("Συμπληρώστε την προγραμματισμένη ημερομηνία παραλαβής.");
        if (request.LoadedAt < request.ScheduledPickupAt.AddDays(-1)) throw new ValidationException("Η φόρτωση δεν μπορεί να προηγείται σημαντικά του προγραμματισμένου δρομολογίου.");
        if (request.DeliveredAt < request.LoadedAt || request.DeliveredAt < request.ScheduledPickupAt.AddDays(-1)) throw new ValidationException("Η παράδοση δεν μπορεί να προηγείται της φόρτωσης ή του δρομολογίου.");
        if (request.PaidAt is not null && request.PaidAmount <= 0) throw new ValidationException("Η ημερομηνία πληρωμής απαιτεί θετικό εξοφλημένο ποσό.");
        if (request.FactoryUnitPrice < 0) throw new ValidationException("Η τιμή εργοστασίου δεν μπορεί να είναι αρνητική.");
        if (request.Status is DeliveryLogisticsStatus.Weighed or DeliveryLogisticsStatus.Delivered or DeliveryLogisticsStatus.Settled && request.GrossWeight <= 0)
            throw new ValidationException("Για ζυγισμένο ή ολοκληρωμένο φορτίο απαιτείται μικτό βάρος.");
        ValidateFileUrl(request.WeighingSlipUrl);
        ValidateFileUrl(request.DispatchNoteUrl);
        ValidateFileUrl(request.DeliveryReceiptUrl);
        if (request.ProductionDeclarationId is { } productionId && !await db.ProductionDeclarations.AnyAsync(x => x.Id == productionId && x.ProducerUserId == producerUserId, ct))
            throw new ValidationException("Η επιλεγμένη δήλωση παραγωγής δεν ανήκει στον παραγωγό.");
        if (request.ContractId is { } contractId && !await db.Contracts.AnyAsync(x => x.Id == contractId && x.UserId == producerUserId, ct))
            throw new ValidationException("Η επιλεγμένη σύμβαση δεν ανήκει στον παραγωγό.");
        if (request.DealId is { } dealId && !await db.Deals.AnyAsync(x => x.Id == dealId && x.FarmerUserId == producerUserId, ct))
            throw new ValidationException("Η επιλεγμένη συμφωνία δεν ανήκει στον παραγωγό.");
        if (await db.ProducerDeliveryRecords.AnyAsync(x => x.RouteNumber == request.RouteNumber.Trim() && x.Id != id, ct))
            throw new ValidationException("Υπάρχει ήδη δρομολόγιο με αυτόν τον αριθμό.");

        DeliverySettlement settlement;
        try
        {
            settlement = DeliverySettlementCalculator.Calculate(request.GrossWeight, request.TareWeight, request.RejectedWeight, request.UnitPrice,
                request.QualityBonusPercent, request.CommissionPercent, request.WithholdingPercent, request.VatPercent, request.TransportCost, request.OtherDeductions);
        }
        catch (ArgumentOutOfRangeException ex) { throw new ValidationException(ex.Message); }
        if (request.PaidAmount < 0 || request.PaidAmount > settlement.NetPayableAmount)
            throw new ValidationException("Το εξοφλημένο ποσό πρέπει να είναι από μηδέν έως το καθαρό πληρωτέο.");
        if (request.PaymentStatus == DeliveryPaymentStatus.Paid && request.PaidAmount != settlement.NetPayableAmount)
            throw new ValidationException("Για κατάσταση «Εξοφλημένο» το πληρωμένο ποσό πρέπει να ισούται με το καθαρό πληρωτέο.");

        var item = id is { } deliveryId
            ? await db.ProducerDeliveryRecords.SingleOrDefaultAsync(x => x.Id == deliveryId && x.ProducerUserId == producerUserId, ct) ?? throw new KeyNotFoundException("Το δρομολόγιο δεν βρέθηκε.")
            : new ProducerDeliveryRecord { ProducerUserId = producerUserId, CreatedByUserId = adminUserId };
        item.ProductionDeclarationId = request.ProductionDeclarationId;
        item.ContractId = request.ContractId;
        item.DealId = request.DealId;
        item.RouteNumber = request.RouteNumber.Trim();
        item.Status = request.Status;
        item.Product = request.Product.Trim();
        item.Variety = CleanOptional(request.Variety);
        item.QualityGrade = request.QualityGrade.Trim();
        item.LotNumber = CleanOptional(request.LotNumber);
        item.OriginAddress = request.OriginAddress.Trim();
        item.DestinationAddress = request.DestinationAddress.Trim();
        item.FactoryName = request.FactoryName.Trim();
        item.ScheduledPickupAt = request.ScheduledPickupAt;
        item.LoadedAt = request.LoadedAt;
        item.DeliveredAt = request.DeliveredAt;
        item.CarrierName = request.CarrierName.Trim();
        item.DriverName = request.DriverName.Trim();
        item.VehiclePlate = request.VehiclePlate.Trim().ToUpperInvariant();
        item.TrailerPlate = CleanOptional(request.TrailerPlate)?.ToUpperInvariant();
        item.GrossWeight = request.GrossWeight;
        item.TareWeight = request.TareWeight;
        item.NetWeight = settlement.NetWeight;
        item.RejectedWeight = request.RejectedWeight;
        item.AcceptedWeight = settlement.AcceptedWeight;
        item.WeightUnit = string.IsNullOrWhiteSpace(request.WeightUnit) ? "kg" : request.WeightUnit.Trim();
        item.WeighedAt = request.WeighedAt;
        item.WeighbridgeName = CleanOptional(request.WeighbridgeName);
        item.WeighingSlipNumber = CleanOptional(request.WeighingSlipNumber);
        item.WeighingSlipUrl = CleanOptional(request.WeighingSlipUrl);
        item.DispatchNoteNumber = CleanOptional(request.DispatchNoteNumber);
        item.DispatchNoteUrl = CleanOptional(request.DispatchNoteUrl);
        item.DeliveryReceiptNumber = CleanOptional(request.DeliveryReceiptNumber);
        item.DeliveryReceiptUrl = CleanOptional(request.DeliveryReceiptUrl);
        item.AgreementReference = request.AgreementReference.Trim();
        item.AgreementType = request.AgreementType;
        item.UnitPrice = request.UnitPrice;
        item.FactoryUnitPrice = request.FactoryUnitPrice;
        item.QualityBonusPercent = request.QualityBonusPercent;
        item.CommissionPercent = request.CommissionPercent;
        item.WithholdingPercent = request.WithholdingPercent;
        item.VatPercent = request.VatPercent;
        item.TransportCost = request.TransportCost;
        item.OtherDeductions = request.OtherDeductions;
        item.BaseAmount = settlement.BaseAmount;
        item.BonusAmount = settlement.BonusAmount;
        item.CommissionAmount = settlement.CommissionAmount;
        item.WithholdingAmount = settlement.WithholdingAmount;
        item.VatAmount = settlement.VatAmount;
        item.NetPayableAmount = settlement.NetPayableAmount;
        item.PaidAmount = request.PaidAmount;
        item.PaymentDueDate = request.PaymentDueDate;
        item.PaidAt = request.PaidAt;
        item.PaymentStatus = request.PaymentStatus;
        item.AgreementNotes = CleanOptional(request.AgreementNotes);
        item.LoadNotes = CleanOptional(request.LoadNotes);
        item.InternalNotes = CleanOptional(request.InternalNotes);
        item.IsVisibleToProducer = request.IsVisibleToProducer;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByUserId = adminUserId;
        if (id is null) db.ProducerDeliveryRecords.Add(item);
        AddAudit(adminUserId, id is null ? "Create" : "Update", nameof(ProducerDeliveryRecord), item.Id, $"Producer {producerUserId}, route {item.RouteNumber}, factory {item.FactoryName}, accepted {item.AcceptedWeight:N3} {item.WeightUnit}, producer payable {item.NetPayableAmount:N2}, factory value {item.FactoryGrossValue:N2}");
        db.Notifications.Add(new Notification { UserId = producerUserId, Title = id is null ? "Νέο δρομολόγιο στον φάκελό σας" : "Ενημέρωση δρομολογίου", Message = $"Το δρομολόγιο {item.RouteNumber} ενημερώθηκε με κατάσταση {item.Status}." });
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task DeleteProducerDeliveryAsync(Guid id, string adminUserId, CancellationToken ct = default)
    {
        var item = await db.ProducerDeliveryRecords.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Το δρομολόγιο δεν βρέθηκε.");
        db.ProducerDeliveryRecords.Remove(item);
        AddAudit(adminUserId, "Delete", nameof(ProducerDeliveryRecord), item.Id, item.RouteNumber);
        await db.SaveChangesAsync(ct);
    }

    private async Task<ProducerAdminWorkspaceDto> BuildProducerWorkspaceAsync(UserSummary producer, IReadOnlyList<UserSummary> producers, bool includeAdminOnly, CancellationToken ct)
    {
        var profile = await db.ProducerCollaborationProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.ProducerUserId == producer.Id, ct);
        var profileDto = new ProducerCollaborationProfileDto(
            producer.Id, producer.Name, producer.Email, producer.Region,
            profile?.Category ?? ProducerCategory.Developing, profile?.NextCategory ?? ProducerCategory.Standard,
            profile?.CategoryProgressPercent ?? 0, profile?.UpgradeRequirements ?? "Η ομάδα συνεργασίας δεν έχει ακόμη ορίσει τα επόμενα κριτήρια.",
            profile?.CommissionRate ?? 0, profile?.BonusRate ?? 0,
            profile?.RelationshipStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow), profile?.AccountManager ?? "Ομάδα Παραγωγών",
            profile?.PaymentTerms ?? "Οι όροι πληρωμής ορίζονται ανά συμφωνία.", includeAdminOnly ? profile?.InternalNotes : null, profile?.UpdatedAt);
        var production = await db.ProductionDeclarations.AsNoTracking().Where(x => x.ProducerUserId == producer.Id).OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProductionSummary(x.Id, x.Product, x.Quantity, x.Unit, x.QualityGrade, x.Region, x.AvailableFrom, x.AvailableTo, x.Status)).ToListAsync(ct);
        var deliveries = await (from d in db.Deals.AsNoTracking() where d.FarmerUserId == producer.Id
            join p in db.ProductionDeclarations.AsNoTracking() on d.ProductionDeclarationId equals p.Id into ps from p in ps.DefaultIfEmpty()
            orderby d.CreatedAt descending select new FarmerDealDto(d.Id, p == null ? "—" : p.Product, d.BuyPricePerUnit, d.BuyQuantity, d.Status, d.CreatedAt)).ToListAsync(ct);
        var documentQuery = db.PartnerDocuments.AsNoTracking().Where(x => x.UserId == producer.Id);
        if (!includeAdminOnly) documentQuery = documentQuery.Where(x => x.IsVisibleToPartner);
        var documents = await documentQuery.OrderByDescending(x => x.IssueDate)
            .Select(x => new PartnerDocumentDto(x.Id, x.Type, x.Title, x.ReferenceNumber, x.FileUrl, x.IssueDate, x.ExpiryDate, x.Notes, x.IsVisibleToPartner)).ToListAsync(ct);
        var invoiceRows = await db.PartnerInvoices.AsNoTracking().Where(x => x.UserId == producer.Id).OrderByDescending(x => x.IssueDate).ToListAsync(ct);
        var invoices = invoiceRows.Select(x => new PartnerInvoiceDto(x.Id, x.Direction, x.InvoiceNumber, x.IssueDate, x.DueDate, x.NetAmount, x.VatAmount, x.TotalAmount, x.PaidAmount, x.OutstandingAmount, x.Status, x.Description, x.FileUrl)).ToList();
        var entries = await db.PartnerFinancialEntries.AsNoTracking().Where(x => x.UserId == producer.Id).OrderByDescending(x => x.EntryDate)
            .Select(x => new PartnerFinancialEntryDto(x.Id, x.EntryDate, x.Type, x.Category, x.Amount, x.Description, x.ReferenceNumber, x.PartnerInvoiceId)).ToListAsync(ct);
        var deliveryQuery = db.ProducerDeliveryRecords.AsNoTracking().Where(x => x.ProducerUserId == producer.Id);
        if (!includeAdminOnly) deliveryQuery = deliveryQuery.Where(x => x.IsVisibleToProducer);
        var deliveryRows = await deliveryQuery.OrderByDescending(x => x.ScheduledPickupAt).ToListAsync(ct);
        var deliveryRecords = deliveryRows.Select(ToDeliveryDto).ToList();
        var logistics = LogisticsSummary(deliveryRecords);
        var detailedSettlements = deliveryRecords.Where(x => x.Status != DeliveryLogisticsStatus.Cancelled && x.AcceptedWeight > 0).ToList();
        var completed = deliveries.Where(x => x.Status == DealStatus.Completed).ToList();
        var delivered = detailedSettlements.Count > 0 ? detailedSettlements.Sum(x => x.AcceptedWeight) : completed.Sum(x => x.Quantity);
        var deliveryRevenue = detailedSettlements.Count > 0 ? detailedSettlements.Sum(x => x.BaseAmount) : completed.Sum(x => x.BuyPricePerUnit * x.Quantity);
        var otherIncome = entries.Where(x => x.Type == FinancialEntryType.Income && (detailedSettlements.Count == 0 || x.Category != FinancialEntryCategory.Bonus)).Sum(x => x.Amount);
        var expenses = entries.Where(x => x.Type == FinancialEntryType.Expense && (detailedSettlements.Count == 0 || x.Category != FinancialEntryCategory.Commission)).Sum(x => x.Amount);
        var settlementCashFlow = detailedSettlements.Count > 0 ? detailedSettlements.Sum(x => x.NetPayableAmount) : deliveryRevenue;
        var financial = new ProducerFinancialSummaryDto(
            production.Sum(x => x.Quantity), delivered, delivered == 0 ? 0 : deliveryRevenue / delivered, deliveryRevenue,
            otherIncome, expenses, settlementCashFlow + otherIncome - expenses,
            invoices.Where(x => x.Direction == PartnerInvoiceDirection.FromProducer && x.Status != PartnerInvoiceStatus.Cancelled).Sum(x => x.OutstandingAmount),
            invoices.Where(x => x.Direction == PartnerInvoiceDirection.FromAgroUnion && x.Status != PartnerInvoiceStatus.Cancelled).Sum(x => x.OutstandingAmount));
        return new ProducerAdminWorkspaceDto(producers, producer, profileDto, financial, production, deliveries, await ContractsFor(producer.Id, ct), documents, invoices, entries, deliveryRecords, logistics);
    }

    private static ProducerAdminWorkspaceDto EmptyProducerWorkspace(IReadOnlyList<UserSummary> producers) =>
        new(producers, null, null, new(0, 0, 0, 0, 0, 0, 0, 0, 0), [], [], [], [], [], [], [], new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    private static ProducerDeliveryDto ToDeliveryDto(ProducerDeliveryRecord x) => new(
        x.Id, x.ProducerUserId, x.ProductionDeclarationId, x.ContractId, x.DealId, x.RouteNumber, x.Status, x.Product, x.Variety, x.QualityGrade,
        x.LotNumber, x.OriginAddress, x.DestinationAddress, x.FactoryName, x.ScheduledPickupAt, x.LoadedAt, x.DeliveredAt, x.CarrierName, x.DriverName,
        x.VehiclePlate, x.TrailerPlate, x.GrossWeight, x.TareWeight, x.NetWeight, x.RejectedWeight, x.AcceptedWeight, x.WeightUnit, x.WeighedAt,
        x.WeighbridgeName, x.WeighingSlipNumber, x.WeighingSlipUrl, x.DispatchNoteNumber, x.DispatchNoteUrl, x.DeliveryReceiptNumber,
        x.DeliveryReceiptUrl, x.AgreementReference, x.AgreementType, x.UnitPrice, x.FactoryUnitPrice, x.QualityBonusPercent, x.CommissionPercent, x.WithholdingPercent,
        x.VatPercent, x.TransportCost, x.OtherDeductions, x.BaseAmount, x.BonusAmount, x.CommissionAmount, x.WithholdingAmount, x.VatAmount,
        x.NetPayableAmount, x.FactoryGrossValue, x.PaidAmount, x.OutstandingAmount, x.PaymentDueDate, x.PaidAt, x.PaymentStatus, x.AgreementNotes, x.LoadNotes,
        x.InternalNotes, x.IsVisibleToProducer, x.UpdatedAt);

    private static ProducerLogisticsSummaryDto LogisticsSummary(IReadOnlyList<ProducerDeliveryDto> deliveries)
    {
        var active = deliveries.Where(x => x.Status != DeliveryLogisticsStatus.Cancelled).ToList();
        return new(
            active.Count,
            active.Count(x => x.Status is DeliveryLogisticsStatus.Scheduled or DeliveryLogisticsStatus.InTransit),
            active.Count(x => x.Status is DeliveryLogisticsStatus.Delivered or DeliveryLogisticsStatus.Settled),
            active.Sum(x => x.GrossWeight), active.Sum(x => x.AcceptedWeight), active.Sum(x => x.RejectedWeight), active.Sum(x => x.BaseAmount),
            active.Sum(x => x.BonusAmount), active.Sum(x => x.CommissionAmount), active.Sum(x => x.WithholdingAmount),
            active.Sum(x => x.TransportCost + x.OtherDeductions), active.Sum(x => x.VatAmount), active.Sum(x => x.NetPayableAmount),
            active.Sum(x => x.PaidAmount), active.Sum(x => x.OutstandingAmount));
    }

    private async Task EnsureProducerAsync(string userId)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new KeyNotFoundException("Ο παραγωγός δεν βρέθηκε.");
        if (!await users.IsInRoleAsync(user, RoleNames.Producer)) throw new ValidationException("Ο επιλεγμένος λογαριασμός δεν ανήκει σε παραγωγό.");
    }

    private void AddAudit(string adminUserId, string action, string entityName, Guid entityId, string details) =>
        db.AuditLogs.Add(new AuditLog { UserId = adminUserId, Action = action, EntityName = entityName, EntityId = entityId.ToString(), Details = details });

    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateFileUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!Uri.TryCreate(value.Trim(), UriKind.RelativeOrAbsolute, out var uri) || (uri.IsAbsoluteUri && uri.Scheme is not ("http" or "https")))
            throw new ValidationException("Ο σύνδεσμος αρχείου πρέπει να είναι έγκυρο URL http/https ή σχετική διαδρομή.");
    }

    private async Task<IReadOnlyList<ContractDto>> ContractsFor(string userId, CancellationToken ct) => await db.Contracts.Where(x => x.UserId == userId)
        .Select(x => new ContractDto(x.Id, x.ContractNumber, x.Subject, x.Status, x.StartDate, x.EndDate, x.PricingTerms, x.QuantityTerms)).ToListAsync(ct);

    private async Task<IReadOnlyList<TransactionDto>> TransactionsFor(string userId, CancellationToken ct) => await db.Transactions.Where(x => x.UserId == userId).OrderByDescending(x => x.TransactionDate)
        .Select(x => new TransactionDto(x.Id, x.Product, x.Side, x.Quantity, x.UnitPrice, x.Quantity * x.UnitPrice, x.TransactionDate, x.Region)).ToListAsync(ct);

    private async Task<IReadOnlyList<PriceItemDto>> VisiblePriceItems(string role, CancellationToken ct) => await db.PriceListItems
        .Where(x => role == RoleNames.Admin || x.VisibleToRoles.Contains(role))
        .OrderBy(x => x.ProductName).Select(x => new PriceItemDto(x.Id, x.Category, x.ProductName, x.Price, x.Unit, x.EffectiveFrom, x.EffectiveTo)).ToListAsync(ct);

    private async Task<IReadOnlyList<SupplyOrderDto>> SupplyOrdersFor(string? producerId, CancellationToken ct) => await db.SupplyOrders.Include(x => x.Items).OrderByDescending(x => x.CreatedAt)
        .Select(x => new SupplyOrderDto(x.Id, x.Title, x.Product, x.Description, x.DeadlineDate, x.Status, x.Items.Sum(i => i.Quantity), producerId == null ? null : x.Items.Where(i => i.ProducerUserId == producerId).Select(i => (decimal?)i.Quantity).FirstOrDefault())).ToListAsync(ct);

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"").Replace("=", "'") }\"";
}
