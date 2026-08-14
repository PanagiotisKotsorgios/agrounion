using AgroUnion.Application.Services;
using AgroUnion.Web.Models;
using AgroUnion.Web.ViewModels;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;

namespace AgroUnion.Web.Controllers;

public sealed class HomeController(IAgroUnionService service, ILogger<HomeController> logger) : Controller
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
