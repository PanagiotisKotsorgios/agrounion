using AgroUnion.Application.Services;
using AgroUnion.Infrastructure.Persistence;
using AgroUnion.Domain.Entities;
using AgroUnion.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace AgroUnion.Web.Controllers;

[Route("account")]
public sealed class AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, AgroUnionDbContext db, IEmailSender emailSender, ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous, HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginForm { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        var user = await users.FindByEmailAsync(form.Email.Trim());
        if (user is null || !user.IsActive)
        {
            db.AuditLogs.Add(SecurityAudit(user?.Id, "LoginFailed", user?.Id ?? form.Email.Trim(), "Αποτυχημένη προσπάθεια σύνδεσης: άγνωστος ή ανενεργός λογαριασμός.", false, "Warning"));
            await db.SaveChangesAsync();
            ModelState.AddModelError("", "Το email ή ο κωδικός δεν είναι σωστός.");
            return View(form);
        }
        var platform = await db.PlatformConfigurations.AsNoTracking().SingleOrDefaultAsync();
        if (platform?.RequireConfirmedEmail == true && !user.EmailConfirmed)
        {
            db.AuditLogs.Add(SecurityAudit(user.Id, "LoginBlockedUnconfirmedEmail", user.Id, "Η σύνδεση απορρίφθηκε επειδή το email δεν είναι επιβεβαιωμένο.", false, "Warning"));
            await db.SaveChangesAsync();
            ModelState.AddModelError("", "Το email του λογαριασμού δεν έχει επιβεβαιωθεί. Επικοινωνήστε με τη διαχείριση.");
            return View(form);
        }
        var result = await signIn.PasswordSignInAsync(user, form.Password, form.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            db.AuditLogs.Add(SecurityAudit(user.Id, result.IsLockedOut ? "AccountLocked" : "LoginFailed", user.Id, result.IsLockedOut ? "Ο λογαριασμός κλειδώθηκε μετά από αποτυχημένες προσπάθειες." : "Αποτυχημένη προσπάθεια σύνδεσης.", false, result.IsLockedOut ? "Critical" : "Warning"));
            await db.SaveChangesAsync();
            ModelState.AddModelError("", result.IsLockedOut ? "Ο λογαριασμός κλειδώθηκε προσωρινά." : "Το email ή ο κωδικός δεν είναι σωστός.");
            return View(form);
        }
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        db.AuditLogs.Add(SecurityAudit(user.Id, "Login", user.Id, "Επιτυχής σύνδεση στο Portal."));
        await db.SaveChangesAsync();
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
        var emailEnabled = await db.PlatformConfigurations.AsNoTracking().Select(x => (bool?)x.EmailNotificationsEnabled).SingleOrDefaultAsync(ct) ?? true;
        if (user is { IsActive: true } && emailEnabled)
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
    public async Task<IActionResult> Logout()
    {
        var user = await users.GetUserAsync(User);
        if (user is not null)
        {
            db.AuditLogs.Add(SecurityAudit(user.Id, "Logout", user.Id, "Αποσύνδεση από το Portal."));
            await db.SaveChangesAsync();
        }
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpGet("change-password")]
    public IActionResult ChangePassword() => RedirectToAction("Profile", "Portal");

    [Authorize, HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordForm form)
    {
        if (form.NewPassword != form.ConfirmPassword) ModelState.AddModelError(nameof(form.ConfirmPassword), "Οι νέοι κωδικοί δεν ταιριάζουν.");
        if (!ModelState.IsValid) return View(form);
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await users.ChangePasswordAsync(user, form.CurrentPassword, form.NewPassword);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return View(form); }
        db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "PasswordChanged", EntityName = "Account", EntityId = user.Id, Details = "Ο κωδικός πρόσβασης άλλαξε επιτυχώς." });
        await db.SaveChangesAsync();
        await signIn.RefreshSignInAsync(user); TempData["Success"] = "Ο κωδικός σας άλλαξε."; return RedirectToAction(nameof(ChangePassword));
    }

    private AuditLog SecurityAudit(string? userId, string action, string entityId, string details, bool succeeded = true, string severity = "Info") => new()
    {
        UserId = userId,
        Action = action,
        Category = "Security",
        Severity = severity,
        EntityName = "Account",
        EntityId = entityId.Length > 80 ? entityId[..80] : entityId,
        Details = details,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent = Request.Headers.UserAgent.ToString() is { Length: > 0 } agent ? (agent.Length > 500 ? agent[..500] : agent) : null,
        CorrelationId = HttpContext.TraceIdentifier,
        Succeeded = succeeded
    };

    [AllowAnonymous, HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();
}
