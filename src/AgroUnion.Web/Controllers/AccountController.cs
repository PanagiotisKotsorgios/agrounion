using AgroUnion.Application.Services;
using AgroUnion.Infrastructure.Persistence;
using AgroUnion.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using System.Net.Mail;

namespace AgroUnion.Web.Controllers;

[Route("account")]
public sealed class AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, IEmailSender emailSender, ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous, HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginForm { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        var user = await users.FindByEmailAsync(form.Email.Trim());
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError("", "Το email ή ο κωδικός δεν είναι σωστός.");
            return View(form);
        }
        var result = await signIn.PasswordSignInAsync(user, form.Password, form.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.IsLockedOut ? "Ο λογαριασμός κλειδώθηκε προσωρινά." : "Το email ή ο κωδικός δεν είναι σωστός.");
            return View(form);
        }
        return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : "/portal");
    }

    [AllowAnonymous, HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordForm());

    [AllowAnonymous, HttpPost("forgot-password"), EnableRateLimiting("public-forms")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordForm form, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(form.Website)) return BadRequest();
        if (!MailAddress.TryCreate(form.Email?.Trim(), out var address))
        {
            ModelState.AddModelError(nameof(form.Email), "Συμπληρώστε ένα έγκυρο email.");
            return View(form);
        }

        var user = await users.FindByEmailAsync(address.Address);
        if (user is { IsActive: true })
        {
            try
            {
                var token = await users.GeneratePasswordResetTokenAsync(user);
                var resetUrl = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token }, Request.Scheme);
                if (!string.IsNullOrWhiteSpace(resetUrl))
                {
                    var safeUrl = WebUtility.HtmlEncode(resetUrl);
                    await emailSender.SendAsync(user.Email!, "Επαναφορά κωδικού AGRO UNION", $"Ζητήθηκε επαναφορά κωδικού για το Portal της AGRO UNION.<br><br><a href=\"{safeUrl}\">Ορίστε νέο κωδικό</a><br><br>Αν δεν κάνατε εσείς το αίτημα, αγνοήστε αυτό το μήνυμα.", ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Αποτυχία αποστολής email επαναφοράς κωδικού για {Email}", address.Address);
            }
        }

        TempData["PasswordResetRequested"] = "Αν υπάρχει ενεργός λογαριασμός με αυτό το email, θα λάβετε σύντομα οδηγίες επαναφοράς.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [AllowAnonymous, HttpGet("reset-password")]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token)) return RedirectToAction(nameof(ForgotPassword));
        return View(new ResetPasswordForm { Email = email, Token = token });
    }

    [AllowAnonymous, HttpPost("reset-password"), EnableRateLimiting("public-forms")]
    public async Task<IActionResult> ResetPassword(ResetPasswordForm form)
    {
        if (string.IsNullOrWhiteSpace(form.NewPassword)) ModelState.AddModelError(nameof(form.NewPassword), "Συμπληρώστε νέο κωδικό.");
        if (form.NewPassword != form.ConfirmPassword) ModelState.AddModelError(nameof(form.ConfirmPassword), "Οι κωδικοί δεν ταιριάζουν.");
        if (!ModelState.IsValid) return View(form);

        var user = await users.FindByEmailAsync(form.Email.Trim());
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError("", "Ο σύνδεσμος επαναφοράς δεν είναι έγκυρος ή έχει λήξει.");
            return View(form);
        }

        var result = await users.ResetPasswordAsync(user, form.Token, form.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(form);
        }

        TempData["PasswordResetSuccess"] = "Ο κωδικός σας άλλαξε. Μπορείτε τώρα να συνδεθείτε.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }

    [Authorize, HttpGet("change-password")]
    public IActionResult ChangePassword() => View(new ChangePasswordForm());

    [Authorize, HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordForm form)
    {
        if (form.NewPassword != form.ConfirmPassword) ModelState.AddModelError(nameof(form.ConfirmPassword), "Οι νέοι κωδικοί δεν ταιριάζουν.");
        if (!ModelState.IsValid) return View(form);
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await users.ChangePasswordAsync(user, form.CurrentPassword, form.NewPassword);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return View(form); }
        await signIn.RefreshSignInAsync(user); TempData["Success"] = "Ο κωδικός σας άλλαξε."; return RedirectToAction(nameof(ChangePassword));
    }

    [AllowAnonymous, HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();
}
