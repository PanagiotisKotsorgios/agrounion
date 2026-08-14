using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Web.Models;
using AgroUnion.Web.ViewModels;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;

namespace AgroUnion.Web.Controllers;

public sealed class HomeController(IAgroUnionService service, IEmailAdministrationService emailAdministration, ILogger<HomeController> logger) : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();

    [HttpGet("/about")]
    public IActionResult About() => View();

    [HttpGet("/history")]
    public IActionResult History() => View();

    [HttpGet("/team")]
    public IActionResult Team() => View();

    [HttpGet("/vision")]
    public IActionResult Vision() => View();

    [HttpGet("/network")]
    public IActionResult Network() => View();

    [HttpGet("/products")]
    public IActionResult Products() => View();

    [HttpGet("/sustainability")]
    public IActionResult Sustainability() => View();

    [HttpGet("/faq")]
    public IActionResult Faq() => View();

    [HttpGet("/services")]
    public IActionResult Services() => View();

    [HttpGet("/how-it-works")]
    public IActionResult HowItWorks() => View();

    [HttpGet("/partners")]
    public IActionResult Partners() => View();

    [HttpGet("/contracts")]
    public IActionResult Contracts() => View();

    [HttpGet("/apply")]
    public IActionResult Apply() => View(new InterestForm());

    [HttpPost("/apply"), EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Apply(InterestForm form, CancellationToken ct)
    {
        try
        {
            await service.SubmitInterestAsync(form.ToRequest(), ct);
            TempData["Success"] = "Η αίτησή σας καταχωρίστηκε. Θα επικοινωνήσουμε σύντομα μαζί σας.";
            return RedirectToAction(nameof(Apply));
        }
        catch (ValidationException ex)
        {
            foreach (var error in ex.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return View(form);
        }
    }

    [HttpGet("/account/register")]
    public IActionResult Register() => View(new InterestForm());

    [HttpPost("/account/register"), EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Register(InterestForm form, CancellationToken ct)
    {
        try
        {
            await service.SubmitInterestAsync(form.ToRequest(), ct);
            TempData["Success"] = "Η αίτηση πρόσβασης καταχωρίστηκε. Θα επικοινωνήσουμε σύντομα μαζί σας.";
            return RedirectToAction(nameof(Register));
        }
        catch (ValidationException ex)
        {
            foreach (var error in ex.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return View(form);
        }
    }

    [HttpGet("/contact")]
    public IActionResult Contact() => View(new ContactForm());

    [HttpPost("/contact"), EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Contact(ContactForm form, CancellationToken ct)
    {
        try
        {
            await service.SubmitContactAsync(form.ToRequest(), ct);
            TempData["Success"] = "Το μήνυμά σας στάλθηκε με επιτυχία.";
            return RedirectToAction(nameof(Contact));
        }
        catch (ValidationException ex)
        {
            foreach (var error in ex.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return View(form);
        }
    }

    [HttpPost("/newsletter"), EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Newsletter(string? newsletterEmail, string? returnPath, string? website, CancellationToken ct)
    {
        var destination = Url.IsLocalUrl(returnPath) ? returnPath! : Url.Action(nameof(Index))!;

        if (!string.IsNullOrWhiteSpace(website))
            return LocalRedirect($"{destination}#newsletter");

        if (!System.Net.Mail.MailAddress.TryCreate(newsletterEmail?.Trim(), out var address))
        {
            TempData["Error"] = "Συμπληρώστε ένα έγκυρο email για την εγγραφή στο newsletter.";
            return LocalRedirect($"{destination}#newsletter");
        }

        try
        {
            await emailAdministration.SubscribeAsync(address.Address, source: "Website", ct: ct);
            TempData["Success"] = "Η εγγραφή σας στο newsletter ολοκληρώθηκε.";
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Μη έγκυρη εγγραφή newsletter για {Email}", address.Address);
            TempData["Error"] = "Δεν ήταν δυνατή η εγγραφή. Ελέγξτε το email και δοκιμάστε ξανά.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Αποτυχία εγγραφής newsletter για {Email}", address.Address);
            TempData["Error"] = "Η εγγραφή δεν ολοκληρώθηκε αυτή τη στιγμή. Δοκιμάστε ξανά αργότερα.";
        }

        return LocalRedirect($"{destination}#newsletter");
    }

    [HttpGet("/newsletter/unsubscribe/{token:guid}")]
    public async Task<IActionResult> UnsubscribeNewsletter(Guid token, CancellationToken ct)
    {
        var removed = await emailAdministration.UnsubscribeAsync(token, ct);
        TempData[removed ? "Success" : "Error"] = removed
            ? "Η διεύθυνσή σας αφαιρέθηκε από τις ενημερώσεις της AGRO UNION."
            : "Ο σύνδεσμος διαγραφής δεν είναι έγκυρος.";
        return RedirectToAction(nameof(Index), "Home", null, "newsletter");
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("/payments")]
    public IActionResult Payments() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        logger.LogError("Unhandled request error {TraceId}", HttpContext.TraceIdentifier);
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
