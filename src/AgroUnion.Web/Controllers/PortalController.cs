using System.Security.Claims;
using System.Text;
using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Domain.Entities;
using AgroUnion.Web.ViewModels;
using AgroUnion.Web.Services;
using AgroUnion.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace AgroUnion.Web.Controllers;

[Authorize, Route("portal")]
public sealed class PortalController(IAgroUnionService service, IEmailAdministrationService emailAdministration, PartnerFileStore fileStore, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, ILogger<PortalController> logger) : Controller
{
    private static readonly HashSet<string> ProducerPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "overview", "collaboration", "finances", "production", "logistics", "invoices", "documents", "offers"
    };
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet("")]
    public async Task<IActionResult> Index(string? producerId, CancellationToken ct)
    {
        ViewData["CompactDashboard"] = (await service.GetAccountPreferencesAsync(UserId, ct)).CompactDashboard;
        ViewData["AccountDisplayName"] = await service.GetAccountDisplayNameAsync(UserId, ct);
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

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        ViewData["PortalPage"] = "profile";
        var profile = await service.GetAccountProfileAsync(UserId, ct);
        ViewData["CompactDashboard"] = profile.Preferences.CompactDashboard;
        return View("Dashboard", profile);
    }

    [HttpPost("profile/details")]
    public async Task<IActionResult> UpdateProfile(AccountProfileForm form, CancellationToken ct)
    {
        try { await service.UpdateAccountProfileAsync(UserId, form.ToRequest(), ct); TempData["Success"] = "Τα προσωπικά στοιχεία ενημερώθηκαν."; }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or KeyNotFoundException) { logger.LogWarning(ex, "Account profile update failed"); TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("profile/preferences")]
    public async Task<IActionResult> UpdateProfilePreferences(AccountPreferenceForm form, CancellationToken ct)
    {
        try { await service.UpdateAccountPreferencesAsync(UserId, form.ToRequest(), ct); TempData["Success"] = "Οι ρυθμίσεις λογαριασμού αποθηκεύτηκαν."; }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or KeyNotFoundException) { logger.LogWarning(ex, "Account preferences update failed"); TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("profile/password")]
    public async Task<IActionResult> ChangeProfilePassword(ProfilePasswordForm form, CancellationToken ct)
    {
        try
        {
            if (form.NewPassword != form.ConfirmPassword) throw new ValidationException("Οι νέοι κωδικοί δεν ταιριάζουν.");
            await service.ChangeOwnPasswordAsync(UserId, form.CurrentPassword, form.NewPassword, ct);
            var user = await users.FindByIdAsync(UserId) ?? throw new KeyNotFoundException("Ο λογαριασμός δεν βρέθηκε.");
            await signIn.RefreshSignInAsync(user);
            TempData["Success"] = "Ο κωδικός πρόσβασης άλλαξε επιτυχώς.";
        }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or KeyNotFoundException) { logger.LogWarning(ex, "Account password change failed"); TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet("profile/statistics.csv")]
    public async Task<IActionResult> ProfileStatisticsCsv(CancellationToken ct)
    {
        var csv = await service.ExportAccountStatisticsCsvAsync(UserId, ct);
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv", $"agro-union-profile-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [Authorize(Policy = "AdminOnly"), HttpGet("email")]
    public async Task<IActionResult> EmailAdministration(CancellationToken ct)
    {
        ViewData["AdminPage"] = "email";
        ViewData["CompactDashboard"] = (await service.GetAccountPreferencesAsync(UserId, ct)).CompactDashboard;
        ViewData["AccountDisplayName"] = await service.GetAccountDisplayNameAsync(UserId, ct);
        return View("Dashboard", await emailAdministration.GetDashboardAsync(ct));
    }

    [HttpGet("marketplace")]
    public IActionResult Marketplace() => MarketplaceUnavailable();

    [HttpPost("support")]
    public async Task<IActionResult> SubmitDashboardSupport(DashboardSupportForm form, CancellationToken ct)
    {
        var returnPath = Url.IsLocalUrl(form.ReturnPath) && form.ReturnPath!.StartsWith("/portal", StringComparison.OrdinalIgnoreCase)
            ? form.ReturnPath
            : "/portal";

        try
        {
            var isHelpRequest = string.Equals(form.Kind, "help", StringComparison.OrdinalIgnoreCase);
            var isContactRequest = string.Equals(form.Kind, "contact", StringComparison.OrdinalIgnoreCase);
            if (!isHelpRequest && !isContactRequest) throw new InvalidOperationException("Το είδος αιτήματος δεν είναι έγκυρο.");
            if (string.IsNullOrWhiteSpace(form.Topic)) throw new InvalidOperationException("Επιλέξτε θέμα αιτήματος.");
            if (string.IsNullOrWhiteSpace(form.Message)) throw new InvalidOperationException("Συμπληρώστε το μήνυμα του αιτήματος.");

            var email = User.Identity?.Name ?? throw new UnauthorizedAccessException();
            var requestLabel = isHelpRequest ? "Αίτημα υποστήριξης Portal" : "Επικοινωνία συνεργάτη Portal";
            var phoneLine = string.IsNullOrWhiteSpace(form.Phone) ? "" : $"\nΤηλέφωνο επικοινωνίας: {form.Phone.Trim()}";
            var message = $"{requestLabel}\nΘέμα: {form.Topic.Trim()}{phoneLine}\n\n{form.Message?.Trim()}";

            await service.SubmitContactAsync(new ContactRequest($"Portal · {email}", email, message, form.Website), ct);
            TempData["Success"] = isHelpRequest
                ? "Το αίτημα υποστήριξης καταχωρίστηκε. Η ομάδα μας θα επικοινωνήσει μαζί σας."
                : "Το μήνυμά σας καταχωρίστηκε και προωθήθηκε στην ομάδα της AGRO UNION.";
        }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Dashboard support request failed");
            TempData["Error"] = ex.Message;
        }

        return LocalRedirect(returnPath);
    }

    [Authorize(Policy = "FarmerOnly"), HttpPost("marketplace/production")]
    public IActionResult SaveMarketplaceProduction(PartnerProductionListingForm form) => MarketplaceUnavailable();

    [Authorize(Policy = "FarmerOnly"), HttpPost("marketplace/production/{id:guid}/status")]
    public IActionResult SetMarketplaceProductionStatus(Guid id, bool active) => MarketplaceUnavailable();

    [Authorize(Policy = "BuyerOnly"), HttpPost("marketplace/demand")]
    public IActionResult CreateMarketplaceDemand(PartnerBuyingRequestForm form) => MarketplaceUnavailable();

    [Authorize(Policy = "BuyerOnly"), HttpPost("marketplace/demand/{id:guid}/status")]
    public IActionResult SetMarketplaceDemandStatus(Guid id, bool active) => MarketplaceUnavailable();

    [HttpPost("marketplace/inquiries")]
    public IActionResult SendMarketplaceInquiry(PartnerMarketplaceInquiryForm form) => MarketplaceUnavailable();

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
        ViewData["CompactDashboard"] = (await service.GetAccountPreferencesAsync(UserId, ct)).CompactDashboard;
        ViewData["AccountDisplayName"] = await service.GetAccountDisplayNameAsync(UserId, ct);
        return View("Dashboard", await service.GetProducerDashboardAsync(UserId, ct));
    }

    [Authorize(Policy = "FarmerOnly"), HttpPost("production/save")]
    public async Task<IActionResult> SaveProduction(ProductionForm form, CancellationToken ct) => await Run(async () => await service.SaveProductionAsync(UserId, form.Id, form.ToRequest(), ct), "Η δήλωση παραγωγής αποθηκεύτηκε.", producerPage: "production");

    [Authorize(Policy = "FarmerOnly"), HttpPost("production/{id:guid}/delete")]
    public async Task<IActionResult> DeleteProduction(Guid id, CancellationToken ct) => await Run(async () => await service.DeleteProductionAsync(UserId, id, ct), "Η δήλωση διαγράφηκε.", producerPage: "production");

    [Authorize(Policy = "FarmerOnly"), HttpPost("supply/join")]
    public IActionResult JoinSupply(SupplyParticipationForm form)
    {
        TempData["Info"] = "Η συμμετοχή σε συλλογικές προμήθειες θα είναι σύντομα διαθέσιμη.";
        return RedirectToAction(nameof(ProducerPage), new { page = "offers" });
    }

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

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/documents"), RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<IActionResult> AddPartnerDocument(PartnerDocumentForm form, CancellationToken ct)
    {
        string? storageKey = null;
        try
        {
            storageKey = await fileStore.SavePdfAsync(form.PdfFile, "documents", ct);
            if (storageKey is null) throw new InvalidOperationException("Επιλέξτε το αρχείο PDF του εγγράφου.");
            form.FileUrl = storageKey;
            await service.AddPartnerDocumentAsync(form.ProducerUserId, form.ToRequest(), UserId, ct);
            TempData["Success"] = "Το έγγραφο PDF προστέθηκε με ασφάλεια στον φάκελο του παραγωγού.";
        }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or KeyNotFoundException)
        {
            fileStore.Delete(storageKey);
            logger.LogWarning(ex, "Partner document upload failed");
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { producerId = form.ProducerUserId });
    }

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/documents/{id:guid}/delete")]
    public async Task<IActionResult> DeletePartnerDocument(Guid id, string producerUserId, CancellationToken ct)
    {
        try
        {
            PartnerFileAccessDto? access = null;
            try { access = await service.GetPartnerDocumentFileAsync(id, UserId, true, ct); } catch (KeyNotFoundException) { }
            await service.DeletePartnerDocumentAsync(id, UserId, ct);
            if (access is not null) fileStore.Delete(access.StorageKey);
            TempData["Success"] = "Το έγγραφο διαγράφηκε.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Partner document deletion failed");
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { producerId = producerUserId });
    }

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/invoices"), RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<IActionResult> AddPartnerInvoice(PartnerInvoiceForm form, CancellationToken ct)
    {
        string? storageKey = null;
        try
        {
            storageKey = await fileStore.SavePdfAsync(form.PdfFile, "invoices", ct);
            if (storageKey is null) throw new InvalidOperationException("Επιλέξτε το αρχείο PDF του τιμολογίου.");
            form.FileUrl = storageKey;
            await service.AddPartnerInvoiceAsync(form.ProducerUserId, form.ToRequest(), UserId, ct);
            TempData["Success"] = "Το τιμολόγιο και το PDF καταχωρίστηκαν με ασφάλεια.";
        }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or KeyNotFoundException)
        {
            fileStore.Delete(storageKey);
            logger.LogWarning(ex, "Partner invoice upload failed");
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { producerId = form.ProducerUserId });
    }

    [Authorize(Policy = "AdminOnly"), HttpPost("producer-workspace/invoices/{id:guid}/delete")]
    public async Task<IActionResult> DeletePartnerInvoice(Guid id, string producerUserId, CancellationToken ct)
    {
        try
        {
            PartnerFileAccessDto? access = null;
            try { access = await service.GetPartnerInvoiceFileAsync(id, UserId, true, ct); } catch (KeyNotFoundException) { }
            await service.DeletePartnerInvoiceAsync(id, UserId, ct);
            if (access is not null) fileStore.Delete(access.StorageKey);
            TempData["Success"] = "Το τιμολόγιο διαγράφηκε.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Partner invoice deletion failed");
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { producerId = producerUserId });
    }

    [HttpGet("documents/{id:guid}/file")]
    public async Task<IActionResult> PartnerDocumentFile(Guid id, bool download, CancellationToken ct)
    {
        try
        {
            var access = await service.GetPartnerDocumentFileAsync(id, UserId, User.IsInRole(RoleNames.Admin), ct);
            return File(fileStore.OpenRead(access.StorageKey), "application/pdf", download ? access.DownloadName : null, enableRangeProcessing: true);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) when (ex is KeyNotFoundException or FileNotFoundException) { return NotFound(); }
    }

    [HttpGet("invoices/{id:guid}/file")]
    public async Task<IActionResult> PartnerInvoiceFile(Guid id, bool download, CancellationToken ct)
    {
        try
        {
            var access = await service.GetPartnerInvoiceFileAsync(id, UserId, User.IsInRole(RoleNames.Admin), ct);
            return File(fileStore.OpenRead(access.StorageKey), "application/pdf", download ? access.DownloadName : null, enableRangeProcessing: true);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) when (ex is KeyNotFoundException or FileNotFoundException) { return NotFound(); }
    }

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

    private IActionResult MarketplaceUnavailable()
    {
        TempData["Info"] = "Η Αγορά Δικτύου βρίσκεται σε προετοιμασία και θα είναι σύντομα διαθέσιμη.";
        return RedirectToAction(nameof(Index));
    }
}
