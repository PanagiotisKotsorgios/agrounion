using System.Net;
using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Domain.Entities;
using AgroUnion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroUnion.Infrastructure.Services;

public sealed class PartnerMarketplaceService(AgroUnionDbContext db, IEmailSender emailSender) : IPartnerMarketplaceService
{
    private static readonly string[] PartnerRoles = [RoleNames.Producer, RoleNames.Trader, RoleNames.Company];

    public async Task<PartnerMarketplaceDto> GetMarketplaceAsync(string currentUserId, string? role, string? region, string? product, string? search, CancellationToken ct = default)
    {
        var currentRole = await GetRoleAsync(currentUserId, allowAdmin: true, ct);
        var users = await db.Users.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        var names = users.ToDictionary(x => x.Id, x => x.FullNameOrCompany);
        var userRoles = await (from ur in db.UserRoles.AsNoTracking()
                               join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                               where PartnerRoles.Contains(r.Name!)
                               select new { ur.UserId, Role = r.Name! }).ToListAsync(ct);
        var rolesByUser = userRoles.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.First().Role);

        var declarations = await db.ProductionDeclarations.AsNoTracking().ToListAsync(ct);
        var buyingRows = await db.PartnerBuyingRequests.AsNoTracking().ToListAsync(ct);
        var productsByUser = declarations.GroupBy(x => x.ProducerUserId)
            .ToDictionary(x => x.Key, x => x.Select(p => p.Product).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p).ToList());
        foreach (var group in buyingRows.GroupBy(x => x.BuyerUserId))
        {
            if (!productsByUser.TryGetValue(group.Key, out var values)) productsByUser[group.Key] = values = [];
            foreach (var value in group.Select(x => x.Product).Distinct(StringComparer.OrdinalIgnoreCase))
                if (!values.Contains(value, StringComparer.OrdinalIgnoreCase)) values.Add(value);
            values.Sort(StringComparer.Create(new System.Globalization.CultureInfo("el-GR"), true));
        }

        var normalizedRole = Clean(role);
        var normalizedRegion = Clean(region);
        var normalizedProduct = Clean(product);
        var normalizedSearch = Clean(search);
        var partners = users.Where(x => rolesByUser.ContainsKey(x.Id) && x.Id != currentUserId)
            .Select(x => new PartnerDirectoryDto(x.Id, x.FullNameOrCompany, rolesByUser[x.Id], x.Region, productsByUser.GetValueOrDefault(x.Id, [])))
            .Where(x => normalizedRole is null || x.Role.Equals(normalizedRole, StringComparison.OrdinalIgnoreCase))
            .Where(x => normalizedRegion is null || x.Region.Contains(normalizedRegion, StringComparison.OrdinalIgnoreCase))
            .Where(x => normalizedProduct is null || x.Products.Any(p => p.Contains(normalizedProduct, StringComparison.OrdinalIgnoreCase)))
            .Where(x => normalizedSearch is null || x.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) || x.Region.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) || x.Products.Any(p => p.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Role).ThenBy(x => x.Region).ThenBy(x => x.Name).ToList();

        var committed = await CommittedQuantitiesAsync(ct);
        var productionById = declarations.ToDictionary(x => x.Id);
        var listings = new List<PartnerProductionListingDto>();
        foreach (var listing in await db.PartnerProductionListings.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.AskingPricePerUnit).ToListAsync(ct))
        {
            if (!productionById.TryGetValue(listing.ProductionDeclarationId, out var production) || production.Status != ProductionStatus.Available) continue;
            if (!names.TryGetValue(listing.ProducerUserId, out var producerName)) continue;
            var uncommitted = Math.Max(0, production.Quantity - committed.GetValueOrDefault(production.Id));
            var available = Math.Min(listing.OfferedQuantity, uncommitted);
            if (available <= 0) continue;
            var dto = new PartnerProductionListingDto(listing.Id, listing.ProducerUserId, producerName, production.Product, production.QualityGrade, production.Region, available, production.Unit, listing.AskingPricePerUnit, listing.ProducerUserId == currentUserId);
            if (!Matches(dto.Product, dto.Region, producerName, normalizedProduct, normalizedRegion, normalizedSearch)) continue;
            listings.Add(dto);
        }

        var requests = buyingRows.Where(x => x.IsActive && x.ValidUntilUtc >= DateTime.UtcNow && names.ContainsKey(x.BuyerUserId))
            .Select(x => new PartnerBuyingRequestDto(x.Id, x.BuyerUserId, names[x.BuyerUserId], rolesByUser.GetValueOrDefault(x.BuyerUserId, RoleNames.Company), x.Product, x.Quantity, x.Unit, x.MaxPricePerUnit, x.Region, x.QualityRequirements, x.ValidUntilUtc, x.BuyerUserId == currentUserId))
            .Where(x => Matches(x.Product, x.Region, x.BuyerName, normalizedProduct, normalizedRegion, normalizedSearch))
            .OrderByDescending(x => x.MaxPricePerUnit).ThenBy(x => x.ValidUntilUtc).ToList();

        var ownOptions = currentRole == RoleNames.Producer
            ? declarations.Where(x => x.ProducerUserId == currentUserId && x.Status == ProductionStatus.Available)
                .Select(x => new MarketplaceProductionOptionDto(x.Id, x.Product, x.QualityGrade, x.Region, Math.Max(0, x.Quantity - committed.GetValueOrDefault(x.Id)), x.Unit))
                .Where(x => x.UncommittedQuantity > 0).OrderBy(x => x.Product).ToList()
            : [];

        var inquiryRows = await db.PartnerMarketplaceInquiries.AsNoTracking()
            .Where(x => x.SenderUserId == currentUserId || x.RecipientUserId == currentUserId)
            .OrderByDescending(x => x.CreatedAtUtc).Take(30).ToListAsync(ct);
        var listingContexts = listings.ToDictionary(x => x.Id, x => $"{x.Product} · {x.Region}");
        var requestContexts = requests.ToDictionary(x => x.Id, x => $"{x.Product} · {x.Region}");
        var inquiries = inquiryRows.Select(x => new PartnerMarketplaceInquiryDto(
            x.Id,
            names.GetValueOrDefault(x.SenderUserId == currentUserId ? x.RecipientUserId : x.SenderUserId, "Συνεργάτης δικτύου"),
            x.SenderUserId == currentUserId ? "Sent" : "Received",
            x.ProductionListingId is { } listingId ? listingContexts.GetValueOrDefault(listingId, "Παραγωγή δικτύου") : x.BuyingRequestId is { } requestId ? requestContexts.GetValueOrDefault(requestId, "Ζήτηση αγοράς") : "Αγορά δικτύου",
            x.Quantity, x.OfferedPricePerUnit, x.Status, x.CreatedAtUtc)).ToList();

        var regionOptions = users.Where(x => rolesByUser.ContainsKey(x.Id)).Select(x => x.Region)
            .Concat(declarations.Select(x => x.Region)).Concat(buyingRows.Select(x => x.Region))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var productOptions = declarations.Select(x => x.Product).Concat(buyingRows.Select(x => x.Product))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        return new PartnerMarketplaceDto(currentUserId, currentRole, partners, listings, requests, ownOptions, inquiries, regionOptions, productOptions, normalizedRole, normalizedRegion, normalizedProduct, normalizedSearch);
    }

    public async Task SaveProductionListingAsync(string producerUserId, PartnerProductionListingRequest request, CancellationToken ct = default)
    {
        await EnsureRoleAsync(producerUserId, [RoleNames.Producer], ct);
        var production = await db.ProductionDeclarations.SingleOrDefaultAsync(x => x.Id == request.ProductionDeclarationId && x.ProducerUserId == producerUserId, ct)
            ?? throw new KeyNotFoundException("Η δήλωση παραγωγής δεν βρέθηκε.");
        if (production.Status != ProductionStatus.Available) throw new InvalidOperationException("Μόνο διαθέσιμη παραγωγή μπορεί να δημοσιευτεί στο δίκτυο.");
        var committed = await db.Deals.Where(x => x.ProductionDeclarationId == production.Id && x.Status != DealStatus.Cancelled).SumAsync(x => (decimal?)x.BuyQuantity, ct) ?? 0;
        var uncommitted = Math.Max(0, production.Quantity - committed);
        if (request.OfferedQuantity <= 0 || request.OfferedQuantity > uncommitted)
            throw new InvalidOperationException($"Μπορείτε να διαθέσετε έως {uncommitted:N3} {production.Unit}. Η δεσμευμένη ποσότητα δεν επιτρέπεται να δημοσιευτεί.");
        if (request.AskingPricePerUnit <= 0) throw new InvalidOperationException("Η ζητούμενη τιμή πρέπει να είναι θετική.");

        var listing = await db.PartnerProductionListings.SingleOrDefaultAsync(x => x.ProductionDeclarationId == production.Id, ct);
        if (listing is null)
        {
            listing = new PartnerProductionListing { ProductionDeclarationId = production.Id, ProducerUserId = producerUserId, CreatedAtUtc = DateTime.UtcNow };
            db.PartnerProductionListings.Add(listing);
        }
        listing.OfferedQuantity = request.OfferedQuantity;
        listing.AskingPricePerUnit = request.AskingPricePerUnit;
        listing.IsActive = true;
        listing.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetProductionListingActiveAsync(string producerUserId, Guid id, bool active, CancellationToken ct = default)
    {
        var listing = await db.PartnerProductionListings.SingleOrDefaultAsync(x => x.Id == id && x.ProducerUserId == producerUserId, ct)
            ?? throw new KeyNotFoundException("Η προσφορά παραγωγής δεν βρέθηκε.");
        if (active)
        {
            var production = await db.ProductionDeclarations.SingleAsync(x => x.Id == listing.ProductionDeclarationId, ct);
            var committed = await db.Deals.Where(x => x.ProductionDeclarationId == production.Id && x.Status != DealStatus.Cancelled).SumAsync(x => (decimal?)x.BuyQuantity, ct) ?? 0;
            if (listing.OfferedQuantity > Math.Max(0, production.Quantity - committed)) throw new InvalidOperationException("Η προσφορά υπερβαίνει πλέον τη μη δεσμευμένη παραγωγή. Ενημερώστε την ποσότητα.");
        }
        listing.IsActive = active;
        listing.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> CreateBuyingRequestAsync(string buyerUserId, PartnerBuyingRequestRequest request, CancellationToken ct = default)
    {
        await EnsureRoleAsync(buyerUserId, [RoleNames.Trader, RoleNames.Company], ct);
        if (string.IsNullOrWhiteSpace(request.Product) || string.IsNullOrWhiteSpace(request.Region)) throw new InvalidOperationException("Συμπληρώστε προϊόν και περιοχή παραλαβής.");
        if (request.Quantity <= 0 || request.MaxPricePerUnit <= 0) throw new InvalidOperationException("Ποσότητα και μέγιστη τιμή πρέπει να είναι θετικές.");
        if (request.ValidUntilUtc <= DateTime.UtcNow || request.ValidUntilUtc > DateTime.UtcNow.AddDays(180)) throw new InvalidOperationException("Η ισχύς πρέπει να είναι μελλοντική και έως 180 ημέρες.");
        var item = new PartnerBuyingRequest
        {
            BuyerUserId = buyerUserId,
            Product = request.Product.Trim()[..Math.Min(request.Product.Trim().Length, 120)],
            Quantity = request.Quantity,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "kg" : request.Unit.Trim()[..Math.Min(request.Unit.Trim().Length, 30)],
            MaxPricePerUnit = request.MaxPricePerUnit,
            Region = request.Region.Trim()[..Math.Min(request.Region.Trim().Length, 120)],
            QualityRequirements = Truncate(request.QualityRequirements, 1000),
            ValidUntilUtc = request.ValidUntilUtc,
            IsActive = true
        };
        db.PartnerBuyingRequests.Add(item);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task SetBuyingRequestActiveAsync(string buyerUserId, Guid id, bool active, CancellationToken ct = default)
    {
        var item = await db.PartnerBuyingRequests.SingleOrDefaultAsync(x => x.Id == id && x.BuyerUserId == buyerUserId, ct)
            ?? throw new KeyNotFoundException("Η ζήτηση αγοράς δεν βρέθηκε.");
        if (active && item.ValidUntilUtc <= DateTime.UtcNow) throw new InvalidOperationException("Η ζήτηση έχει λήξει και δεν μπορεί να ενεργοποιηθεί.");
        item.IsActive = active;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> SendInquiryAsync(string senderUserId, PartnerMarketplaceInquiryRequest request, CancellationToken ct = default)
    {
        await GetRoleAsync(senderUserId, allowAdmin: false, ct);
        var targetCount = (request.ProductionListingId is not null ? 1 : 0) + (request.BuyingRequestId is not null ? 1 : 0) + (!string.IsNullOrWhiteSpace(request.PartnerUserId) ? 1 : 0);
        if (targetCount != 1) throw new InvalidOperationException("Επιλέξτε μία καταχώριση ή έναν συνεργάτη.");
        if (request.Quantity <= 0 || request.OfferedPricePerUnit <= 0) throw new InvalidOperationException("Ποσότητα και προτεινόμενη τιμή πρέπει να είναι θετικές.");

        string recipientId;
        string context;
        if (request.ProductionListingId is { } listingId)
        {
            var listing = await db.PartnerProductionListings.SingleOrDefaultAsync(x => x.Id == listingId && x.IsActive, ct) ?? throw new KeyNotFoundException("Η προσφορά παραγωγής δεν είναι πλέον ενεργή.");
            var production = await db.ProductionDeclarations.SingleAsync(x => x.Id == listing.ProductionDeclarationId, ct);
            var committed = await db.Deals.Where(x => x.ProductionDeclarationId == production.Id && x.Status != DealStatus.Cancelled).SumAsync(x => (decimal?)x.BuyQuantity, ct) ?? 0;
            var available = Math.Min(listing.OfferedQuantity, Math.Max(0, production.Quantity - committed));
            if (request.Quantity > available) throw new InvalidOperationException($"Η διαθέσιμη μη δεσμευμένη ποσότητα είναι {available:N3} {production.Unit}.");
            recipientId = listing.ProducerUserId;
            context = $"{production.Product} · {production.Region}";
        }
        else if (request.BuyingRequestId is not null)
        {
            var buying = await db.PartnerBuyingRequests.SingleOrDefaultAsync(x => x.Id == request.BuyingRequestId && x.IsActive && x.ValidUntilUtc >= DateTime.UtcNow, ct) ?? throw new KeyNotFoundException("Η ζήτηση αγοράς δεν είναι πλέον ενεργή.");
            if (request.Quantity > buying.Quantity) throw new InvalidOperationException($"Η ζητούμενη ποσότητα είναι έως {buying.Quantity:N3} {buying.Unit}.");
            recipientId = buying.BuyerUserId;
            context = $"{buying.Product} · {buying.Region}";
        }
        else
        {
            recipientId = request.PartnerUserId!;
            var isPartner = await (from ur in db.UserRoles.AsNoTracking() join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id where ur.UserId == recipientId && PartnerRoles.Contains(r.Name!) select ur.UserId).AnyAsync(ct);
            if (!isPartner) throw new InvalidOperationException("Ο επιλεγμένος λογαριασμός δεν είναι συνεργάτης του δικτύου.");
            context = "Γενική εμπορική επαφή";
        }
        if (recipientId == senderUserId) throw new InvalidOperationException("Δεν μπορείτε να στείλετε ενδιαφέρον στη δική σας καταχώριση.");
        var senderUser = await db.Users.SingleAsync(x => x.Id == senderUserId && x.IsActive, ct);
        var recipient = await db.Users.SingleOrDefaultAsync(x => x.Id == recipientId && x.IsActive, ct) ?? throw new InvalidOperationException("Ο συνεργάτης δεν είναι πλέον ενεργός.");

        var inquiry = new PartnerMarketplaceInquiry
        {
            ProductionListingId = request.ProductionListingId,
            BuyingRequestId = request.BuyingRequestId,
            SenderUserId = senderUserId,
            RecipientUserId = recipientId,
            Quantity = request.Quantity,
            OfferedPricePerUnit = request.OfferedPricePerUnit,
            Message = Truncate(request.Message, 1500)
        };
        db.PartnerMarketplaceInquiries.Add(inquiry);
        db.Notifications.Add(new Notification
        {
            UserId = recipientId,
            Title = "Νέο ενδιαφέρον στην Αγορά Δικτύου",
            Message = $"Ο συνεργάτης {senderUser.FullNameOrCompany} έστειλε πρόταση για {context}: {request.Quantity:N3} μονάδες στα {request.OfferedPricePerUnit:N4} € ανά μονάδα."
        });
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(recipient.Email))
        {
            var safeSender = WebUtility.HtmlEncode(senderUser.FullNameOrCompany);
            var safeContext = WebUtility.HtmlEncode(context);
            await emailSender.SendAsync(recipient.Email, "Νέο ενδιαφέρον στην Αγορά Δικτύου", $"<p>Ο συνεργάτης <strong>{safeSender}</strong> έστειλε νέα πρόταση για <strong>{safeContext}</strong>.</p><p>Ποσότητα: <strong>{request.Quantity:N3}</strong><br>Προτεινόμενη τιμή: <strong>{request.OfferedPricePerUnit:N4} € / μονάδα</strong></p><p>Συνδεθείτε στο Portal → Αγορά Δικτύου για τις λεπτομέρειες.</p>", ct);
        }
        return inquiry.Id;
    }

    private async Task<Dictionary<Guid, decimal>> CommittedQuantitiesAsync(CancellationToken ct) => await db.Deals.AsNoTracking()
        .Where(x => x.ProductionDeclarationId != null && x.Status != DealStatus.Cancelled)
        .GroupBy(x => x.ProductionDeclarationId!.Value)
        .Select(x => new { ProductionId = x.Key, Quantity = x.Sum(d => d.BuyQuantity) })
        .ToDictionaryAsync(x => x.ProductionId, x => x.Quantity, ct);

    private async Task EnsureRoleAsync(string userId, string[] allowed, CancellationToken ct)
    {
        var role = await GetRoleAsync(userId, allowAdmin: false, ct);
        if (!allowed.Contains(role)) throw new UnauthorizedAccessException("Ο ρόλος σας δεν επιτρέπει αυτή την ενέργεια.");
    }

    private async Task<string> GetRoleAsync(string userId, bool allowAdmin, CancellationToken ct)
    {
        var role = await (from ur in db.UserRoles.AsNoTracking() join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id where ur.UserId == userId select r.Name).FirstOrDefaultAsync(ct);
        if (role is null || (!PartnerRoles.Contains(role) && !(allowAdmin && role == RoleNames.Admin))) throw new UnauthorizedAccessException("Η Αγορά Δικτύου είναι διαθέσιμη μόνο σε ενεργούς συνεργάτες της AGRO UNION.");
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.IsActive, ct)) throw new UnauthorizedAccessException("Ο λογαριασμός σας δεν είναι ενεργός.");
        return role;
    }

    private static bool Matches(string product, string region, string name, string? productFilter, string? regionFilter, string? search) =>
        (productFilter is null || product.Contains(productFilter, StringComparison.OrdinalIgnoreCase)) &&
        (regionFilter is null || region.Contains(regionFilter, StringComparison.OrdinalIgnoreCase)) &&
        (search is null || product.Contains(search, StringComparison.OrdinalIgnoreCase) || region.Contains(search, StringComparison.OrdinalIgnoreCase) || name.Contains(search, StringComparison.OrdinalIgnoreCase));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? Truncate(string? value, int length)
    {
        var cleaned = Clean(value);
        return cleaned is null ? null : cleaned[..Math.Min(cleaned.Length, length)];
    }
}
