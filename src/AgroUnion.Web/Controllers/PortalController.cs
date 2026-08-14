using System.Security.Claims;
using System.Text;
using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Domain.Entities;
using AgroUnion.Web.ViewModels;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroUnion.Web.Controllers;

[Authorize, Route("portal")]
public sealed class PortalController(IAgroUnionService service, IEmailAdministrationService emailAdministration, IPartnerMarketplaceService marketplace, ILogger<PortalController> logger) : Controller
{
    private static readonly HashSet<string> ProducerPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "overview", "collaboration", "finances", "production", "logistics", "invoices", "documents", "offers"
    };
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet("")]
    public async Task<IActionResult> Index(string? producerId, CancellationToken ct)
    {
        if (User.IsInRole(RoleNames.Admin)) return View("Dashboard", await service.GetAdminDashboardAsync(producerId, ct));
        if (User.IsInRole(RoleNames.Producer))
        {
            ViewData["ProducerPage"] = "overview";
            return View("Dashboard", await service.GetProducerDashboardAsync(UserId, ct));
        }
        if (User.IsInRole(RoleNames.Trader)) return View("Dashboard", await service.GetBuyerDashboardAsync(UserId, false, ct));
        if (User.IsInRole(RoleNames.Company)) return View("Dashboard", await service.GetBuyerDashboardAsync(UserId, true, ct));
        return Forbid();
    }

    [Authorize(Policy = "AdminOnly"), HttpGet("email")]
    public async Task<IActionResult> EmailAdministration(CancellationToken ct)
    {
        ViewData["AdminPage"] = "email";
        return View("Dashboard", await emailAdministration.GetDashboardAsync(ct));
    }

    [HttpGet("marketplace")]
    public async Task<IActionResult> Marketplace(string? role, string? region, string? product, string? search, CancellationToken ct)
    {
        ViewData["PortalPage"] = "marketplace";
        return View("Dashboard", await marketplace.GetMarketplaceAsync(UserId, role, region, product, search, ct));
    }

    [Authorize(Policy = "FarmerOnly"), HttpPost("marketplace/production")]
    public async Task<IActionResult> SaveMarketplaceProduction(PartnerProductionListingForm form, CancellationToken ct) =>
        await RunMarketplace(async () => await marketplace.SaveProductionListingAsync(UserId, form.ToRequest(), ct), "Η μη δεσμευμένη παραγωγή δημοσιεύτηκε στο δίκτυο.");

    [Authorize(Policy = "FarmerOnly"), HttpPost("marketplace/production/{id:guid}/status")]
    public async Task<IActionResult> SetMarketplaceProductionStatus(Guid id, bool active, CancellationToken ct) =>
        await RunMarketplace(async () => await marketplace.SetProductionListingActiveAsync(UserId, id, active, ct), active ? "Η προσφορά παραγωγής ενεργοποιήθηκε." : "Η προσφορά παραγωγής αποσύρθηκε.");

    [Authorize(Policy = "BuyerOnly"), HttpPost("marketplace/demand")]
    public async Task<IActionResult> CreateMarketplaceDemand(PartnerBuyingRequestForm form, CancellationToken ct) =>
        await RunMarketplace(async () => await marketplace.CreateBuyingRequestAsync(UserId, form.ToRequest(), ct), "Η ζήτηση αγοράς δημοσιεύτηκε στο δίκτυο.");

    [Authorize(Policy = "BuyerOnly"), HttpPost("marketplace/demand/{id:guid}/status")]
    public async Task<IActionResult> SetMarketplaceDemandStatus(Guid id, bool active, CancellationToken ct) =>
        await RunMarketplace(async () => await marketplace.SetBuyingRequestActiveAsync(UserId, id, active, ct), active ? "Η ζήτηση ενεργοποιήθηκε." : "Η ζήτηση έκλεισε.");

    [HttpPost("marketplace/inquiries")]
    public async Task<IActionResult> SendMarketplaceInquiry(PartnerMarketplaceInquiryForm form, CancellationToken ct) =>
        await RunMarketplace(async () => await marketplace.SendInquiryAsync(UserId, form.ToRequest(), ct), "Το ενδιαφέρον στάλθηκε στον συνεργάτη και καταγράφηκε στην πλατφόρμα.");

    [Authorize(Policy = "AdminOnly"), HttpPost("email/settings")]
    public async Task<IActionResult> SaveEmailSettings(BrevoSettingsForm form, CancellationToken ct) =>
        await RunEmail(async () => await emailAdministration.SaveSettingsAsync(form.ToRequest(), UserId, ct), "Οι ρυθμίσεις Brevo αποθηκεύτηκαν.");

    [Authorize(Policy = "AdminOnly"), HttpPost("email/test")]
    public async Task<IActionResult> SendTestEmail(string recipientEmail, CancellationToken ct) =>
        await RunEmail(async () => await emailAdministration.SendTestAsync(recipientEmail, ct), "Το δοκιμαστικό email παραδόθηκε στη Brevo.");

    [Authorize(Policy = "AdminOnly"), HttpPost("email/subscribers")]
    public async Task<IActionResult> AddNewsletterSubscriber(NewsletterSubscriberForm form, CancellationToken ct) =>
        await RunEmail(async () => await emailAdministration.AddSubscriberAsync(form.Email, form.DisplayName, ct), "Ο συνδρομητής προστέθηκε στη λίστα.");

    [Authorize(Policy = "AdminOnly"), HttpPost("email/subscribers/{id:guid}/status")]
    public async Task<IActionResult> SetNewsletterSubscriberStatus(Guid id, bool active, CancellationToken ct) =>
        await RunEmail(async () => await emailAdministration.SetSubscriberActiveAsync(id, active, ct), active ? "Ο συνδρομητής ενεργοποιήθηκε." : "Ο συνδρομητής απενεργοποιήθηκε.");

    [Authorize(Policy = "AdminOnly"), HttpPost("email/campaigns")]
    public async Task<IActionResult> SendEmailCampaign(EmailCampaignForm form, CancellationToken ct)
    {
        try
        {
            var result = await emailAdministration.SendCampaignAsync(form.ToRequest(), UserId, ct);
            TempData["Success"] = $"Η αποστολή ολοκληρώθηκε: {result.SentCount} επιτυχίες, {result.FailedCount} αποτυχίες, {result.RecipientCount} παραλήπτες.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Email campaign failed");
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(EmailAdministration));
    }

    [Authorize(Policy = "FarmerOnly"), HttpGet("farmer/{page}")]
    public async Task<IActionResult> ProducerPage(string page, CancellationToken ct)
    {
        if (!ProducerPages.Contains(page)) return NotFound();
        ViewData["ProducerPage"] = page.ToLowerInvariant();
        return View("Dashboard", await service.GetProducerDashboardAsync(UserId, ct));
    }

    [Authorize(Policy = "FarmerOnly"), HttpPost("production/save")]
    public async Task<IActionResult> SaveProduction(ProductionForm form, CancellationToken ct) => await Run(async () => await service.SaveProductionAsync(UserId, form.Id, form.ToRequest(), ct), "Η δήλωση παραγωγής αποθηκεύτηκε.", producerPage: "production");

    [Authorize(Policy = "FarmerOnly"), HttpPost("production/{id:guid}/delete")]
    public async Task<IActionResult> DeleteProduction(Guid id, CancellationToken ct) => await Run(async () => await service.DeleteProductionAsync(UserId, id, ct), "Η δήλωση διαγράφηκε.", producerPage: "production");

    [Authorize(Policy = "FarmerOnly"), HttpPost("supply/join")]
    public async Task<IActionResult> JoinSupply(SupplyParticipationForm form, CancellationToken ct) => await Run(async () => await service.JoinSupplyOrderAsync(UserId, form.OrderId, new(form.Quantity), ct), "Η συμμετοχή σας καταχωρίστηκε.", producerPage: "offers");

    [Authorize(Policy = "BuyerOnly"), HttpPost("offer/counter")]
    public async Task<IActionResult> CounterOffer(CounterOfferForm form, CancellationToken ct) => await Run(async () => await service.SubmitCounterOfferAsync(UserId, form.OfferId, new(form.PricePerUnit, form.Quantity), ct), "Η αντιπροσφορά υποβλήθηκε.");

    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Company}"), HttpPost("prices/create")]
    public async Task<IActionResult> CreatePrice(PriceItemForm form, CancellationToken ct) => await Run(async () => await service.CreatePriceItemAsync(UserId, new(form.Category, form.ProductName, form.Price, form.Unit, form.EffectiveFrom, form.EffectiveTo, form.VisibleToRoles), ct), "Η τιμή δημοσιεύτηκε.");

    [Authorize(Policy = "AdminOnly"), HttpPost("applications/update")]
    public async Task<IActionResult> UpdateApplication(ApplicationUpdateForm form, CancellationToken ct) => await Run(async () => await service.UpdateApplicationAsync(form.Id, form.Status, form.Notes, UserId, ct), "Η αίτηση ενημερώθηκε.");

    [Authorize(Policy = "AdminOnly"), HttpPost("applications/{id:guid}/approve")]
    public async Task<IActionResult> ApproveApplication(Guid id, CancellationToken ct) => await Run(async () => { var result = await service.ApproveApplicationAsync(id, UserId, ct); TempData["Invite"] = $"Ο λογαριασμός {result.Email} δημιουργήθηκε. Προσωρινός κωδικός: {result.TemporaryPassword}"; }, "Η αίτηση εγκρίθηκε και εστάλη πρόσκληση.");

    [Authorize(Policy = "AdminOnly"), HttpPost("users/active")]
    public async Task<IActionResult> SetUserActive(UserActiveForm form, CancellationToken ct) => await Run(async () => await service.SetUserActiveAsync(form.UserId, form.Active, ct), "Ο λογαριασμός ενημερώθηκε.");

    [Authorize(Policy = "AdminOnly"), HttpPost("users/role")]
    public async Task<IActionResult> ChangeUserRole(UserRoleForm form, CancellationToken ct) => await Run(async () => await service.ChangeUserRoleAsync(form.UserId, form.Role, ct), "Ο ρόλος ενημερώθηκε.");

    [Authorize(Policy = "AdminOnly"), HttpPost("users/{userId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string userId, CancellationToken ct)
    {
        try { var password = await service.ResetPasswordAsync(userId, ct); TempData["Invite"] = $"Δημιουργήθηκε προσωρινός κωδικός: {password}"; }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "AdminOnly"), HttpPost("users/{userId}/delete-personal-data")]
    public async Task<IActionResult> DeletePersonalData(string userId, CancellationToken ct) => await Run(async () => await service.DeletePersonalDataAsync(userId, ct), "Τα προσωπικά δεδομένα ανωνυμοποιήθηκαν.");

    [Authorize(Policy = "AdminOnly"), HttpPost("deals/create")]
    public async Task<IActionResult> CreateDeal(DealForm form, CancellationToken ct) => await Run(async () => await service.CreateDealAsync(UserId, new(form.DealType, form.ProductionDeclarationId, form.FarmerUserId, form.BuyPricePerUnit, form.BuyQuantity, form.BuyerUserId, form.SellPricePerUnit, form.SellQuantity), ct), "Η συμφωνία δημιουργήθηκε.");

    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Company}"), HttpPost("supply/create")]
    public async Task<IActionResult> CreateSupplyOrder(SupplyOrderForm form, CancellationToken ct) => await Run(async () => await service.CreateSupplyOrderAsync(new(form.Title, form.Product, form.Description, form.DeadlineDate), ct), "Η συλλογική παραγγελία δημιουργήθηκε.");

    [Authorize(Policy = "AdminOnly"), HttpPost("supply/{id:guid}/close")]
    public async Task<IActionResult> CloseSupplyOrder(Guid id, CancellationToken ct) => await Run(async () => await service.CloseSupplyOrderAsync(id, ct), "Η συλλογική παραγγελία έκλεισε.");

    [Authorize(Policy = "AdminOnly"), HttpPost("contracts/{id:guid}/activate")]
    public async Task<IActionResult> ActivateContract(Guid id, CancellationToken ct) => await Run(async () => await service.ActivateContractAsync(id, ct), "Η σύμβαση ενεργοποιήθηκε.");

    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Company}"), HttpPost("prices/{id:guid}/delete")]
    public async Task<IActionResult> DeletePrice(Guid id, CancellationToken ct) => await Run(async () => await service.DeletePriceItemAsync(UserId, User.IsInRole(RoleNames.Admin), id, ct), "Η τιμή διαγράφηκε.");

    [Authorize(Policy = "AdminOnly"), HttpPost("messages/{id:guid}/read")]
    public async Task<IActionResult> MarkContactRead(Guid id, CancellationToken ct) => await Run(async () => await service.MarkContactReadAsync(id, ct), "Το μήνυμα σημειώθηκε ως αναγνωσμένο.");

    [Authorize(Policy = "FarmerOnly"), HttpPost("deals/{id:guid}/confirm-buy")]
    public async Task<IActionResult> ConfirmBuy(Guid id, CancellationToken ct) => await Run(async () => await service.ConfirmDealSideAsync(UserId, id, true, false, ct), "Το σκέλος αγοράς επιβεβαιώθηκε.", producerPage: "production");

    [Authorize(Policy = "BuyerOnly"), HttpPost("deals/{id:guid}/confirm-sell")]
    public async Task<IActionResult> ConfirmSell(Guid id, CancellationToken ct) => await Run(async () => await service.ConfirmDealSideAsync(UserId, id, false, false, ct), "Το σκέλος πώλησης επιβεβαιώθηκε.");

    [Authorize(Policy = "BuyerOnly"), HttpPost("pickups/schedule")]
    public async Task<IActionResult> SchedulePickup(PickupForm form, CancellationToken ct) => await Run(async () => await service.SchedulePickupAsync(UserId, form.DealId, new(form.ScheduledDate, form.TransportDetails), ct), "Η παραλαβή προγραμματίστηκε.");

    [HttpGet("transactions.csv")]
    public async Task<IActionResult> TransactionsCsv(CancellationToken ct) => File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(await service.ExportTransactionsCsvAsync(UserId, User.IsInRole(RoleNames.Admin), ct))).ToArray(), "text/csv", "transactions.csv");

    [Authorize(Policy = "AdminOnly"), HttpGet("margin.csv")]
    public async Task<IActionResult> MarginCsv(CancellationToken ct) => File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(await service.ExportMarginCsvAsync(ct))).ToArray(), "text/csv", "margin-report.csv");

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/profile")]
    public async Task<IActionResult> SaveProducerProfile(ProducerProfileForm form, CancellationToken ct) =>
        await Run(async () => await service.SaveProducerProfileAsync(form.ProducerUserId, form.ToRequest(), UserId, ct), "Ο φάκελος και η κατηγορία του παραγωγού ενημερώθηκαν.", form.ProducerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/production")]
    public async Task<IActionResult> SaveAdminProduction(AdminProductionForm form, CancellationToken ct) =>
        await Run(async () => await service.SaveProductionAsync(form.ProducerUserId, form.Id, form.ToRequest(), ct), "Η παραγωγή του συνεργάτη ενημερώθηκε από τη διαχείριση.", form.ProducerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/production/{id:guid}/delete")]
    public async Task<IActionResult> DeleteAdminProduction(Guid id, string producerUserId, CancellationToken ct) =>
        await Run(async () => await service.DeleteProductionAsync(producerUserId, id, ct), "Η δήλωση παραγωγής διαγράφηκε.", producerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/documents")]
    public async Task<IActionResult> AddPartnerDocument(PartnerDocumentForm form, CancellationToken ct) =>
        await Run(async () => await service.AddPartnerDocumentAsync(form.ProducerUserId, form.ToRequest(), UserId, ct), "Το έγγραφο προστέθηκε στον φάκελο του παραγωγού.", form.ProducerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/documents/{id:guid}/delete")]
    public async Task<IActionResult> DeletePartnerDocument(Guid id, string producerUserId, CancellationToken ct) =>
        await Run(async () => await service.DeletePartnerDocumentAsync(id, UserId, ct), "Το έγγραφο διαγράφηκε.", producerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/invoices")]
    public async Task<IActionResult> AddPartnerInvoice(PartnerInvoiceForm form, CancellationToken ct) =>
        await Run(async () => await service.AddPartnerInvoiceAsync(form.ProducerUserId, form.ToRequest(), UserId, ct), "Το τιμολόγιο καταχωρίστηκε.", form.ProducerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/invoices/{id:guid}/delete")]
    public async Task<IActionResult> DeletePartnerInvoice(Guid id, string producerUserId, CancellationToken ct) =>
        await Run(async () => await service.DeletePartnerInvoiceAsync(id, UserId, ct), "Το τιμολόγιο διαγράφηκε.", producerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/financial-entries")]
    public async Task<IActionResult> AddPartnerFinancialEntry(PartnerFinancialEntryForm form, CancellationToken ct) =>
        await Run(async () => await service.AddPartnerFinancialEntryAsync(form.ProducerUserId, form.ToRequest(), UserId, ct), "Η οικονομική κίνηση καταχωρίστηκε.", form.ProducerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/financial-entries/{id:guid}/delete")]
    public async Task<IActionResult> DeletePartnerFinancialEntry(Guid id, string producerUserId, CancellationToken ct) =>
        await Run(async () => await service.DeletePartnerFinancialEntryAsync(id, UserId, ct), "Η οικονομική κίνηση διαγράφηκε.", producerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/deliveries")]
    public async Task<IActionResult> SaveProducerDelivery(ProducerDeliveryForm form, CancellationToken ct) =>
        await Run(async () => await service.SaveProducerDeliveryAsync(form.ProducerUserId, form.Id, form.ToRequest(), UserId, ct), "Το δρομολόγιο, η ζύγιση και ο οικονομικός διακανονισμός αποθηκεύτηκαν.", form.ProducerUserId);

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/deliveries/{id:guid}/delete")]
    public async Task<IActionResult> DeleteProducerDelivery(Guid id, string producerUserId, CancellationToken ct) =>
        await Run(async () => await service.DeleteProducerDeliveryAsync(id, UserId, ct), "Το δρομολόγιο διαγράφηκε.", producerUserId);

    private async Task<IActionResult> Run(Func<Task> action, string success, string? producerUserId = null, string? producerPage = null)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or KeyNotFoundException or UnauthorizedAccessException)
        { logger.LogWarning(ex, "Portal action failed"); TempData["Error"] = ex.Message; }
        if (producerPage is not null) return RedirectToAction(nameof(ProducerPage), new { page = producerPage });
        return RedirectToAction(nameof(Index), producerUserId is null ? null : new { producerId = producerUserId });
    }

    private async Task<IActionResult> RunEmail(Func<Task> action, string success)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        { logger.LogWarning(ex, "Email administration action failed"); TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(EmailAdministration));
    }

    private async Task<IActionResult> RunMarketplace(Func<Task> action, string success)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or UnauthorizedAccessException)
        { logger.LogWarning(ex, "Partner marketplace action failed"); TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Marketplace));
    }
}
