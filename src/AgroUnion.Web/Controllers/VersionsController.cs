using System.Security.Claims;
using AgroUnion.Domain.Entities;
using AgroUnion.Infrastructure.Persistence;
using AgroUnion.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroUnion.Web.Controllers;

[Authorize]
[Route("versions")]
public sealed class VersionsController(
    AgroUnionDbContext db,
    IWebHostEnvironment environment,
    ILogger<VersionsController> logger) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
    private string StorageRoot => Path.Combine(environment.ContentRootPath, "App_Data", "releases");

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var isAdmin = User.IsInRole(RoleNames.Admin);
        var query = db.PlatformReleases.AsNoTracking().AsQueryable();
        if (!isAdmin) query = query.Where(x => x.IsPublished);

        var releases = await query
            .OrderByDescending(x => x.PublishedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        return View(new ReleaseCatalogViewModel
        {
            IsAdmin = isAdmin,
            Releases = releases.Select(x => new ReleaseViewModel(
                x.Id,
                x.Version,
                x.Title,
                x.ReleaseNotes,
                x.PublishedAtUtc,
                x.IsPublished)).ToList()
        });
    }

    [Authorize(Policy = "AdminOnly"), HttpPost("create")]
    public async Task<IActionResult> Create(ReleaseUploadForm form, CancellationToken ct)
    {
        var error = ValidateRelease(form);
        if (error is not null) return Failure(error);

        var version = form.Version.Trim();
        if (await db.PlatformReleases.AnyAsync(x => x.Version == version, ct))
            return Failure($"Η έκδοση {version} υπάρχει ήδη. Επιλέξτε διαφορετικό αριθμό έκδοσης.");

        var release = new PlatformRelease
        {
            Version = version,
            Title = form.Title.Trim(),
            ReleaseNotes = form.ReleaseNotes.Trim(),
            PublishedAtUtc = DateTime.SpecifyKind(form.PublishedAt, DateTimeKind.Local).ToUniversalTime(),
            IsPublished = form.IsPublished,
            CreatedByUserId = UserId
        };

        db.PlatformReleases.Add(release);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"Η έκδοση {release.Version} καταχωρίστηκε.";
        return RedirectToCatalog("release-catalog");
    }

    [Authorize(Policy = "AdminOnly"), HttpPost("{id:guid}/visibility")]
    public async Task<IActionResult> ToggleVisibility(Guid id, CancellationToken ct)
    {
        var release = await db.PlatformReleases.FindAsync([id], ct);
        if (release is null) return NotFound();
        release.IsPublished = !release.IsPublished;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = release.IsPublished ? "Η έκδοση δημοσιεύτηκε." : "Η έκδοση μεταφέρθηκε στα πρόχειρα.";
        return RedirectToCatalog($"release-{id}");
    }

    [Authorize(Policy = "AdminOnly"), HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> DeleteRelease(Guid id, CancellationToken ct)
    {
        var release = await db.PlatformReleases.Include(x => x.Assets).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (release is null) return NotFound();
        var storedFiles = release.Assets.Where(x => !string.IsNullOrWhiteSpace(x.StoredFileName)).Select(x => x.StoredFileName!).ToList();
        db.PlatformReleases.Remove(release);
        await db.SaveChangesAsync(ct);
        DeleteFiles(storedFiles);
        TempData["Success"] = $"Η έκδοση {release.Version} διαγράφηκε.";
        return RedirectToCatalog("release-catalog");
    }

    private static string? ValidateRelease(ReleaseUploadForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Version) || form.Version.Trim().Length > 40) return "Συμπληρώστε έγκυρο αριθμό έκδοσης.";
        if (string.IsNullOrWhiteSpace(form.Title) || form.Title.Trim().Length > 180) return "Συμπληρώστε έγκυρο τίτλο έκδοσης.";
        if (form.ReleaseNotes?.Trim().Length > 5000) return "Οι σημειώσεις έκδοσης είναι πολύ μεγάλες.";
        if (form.PublishedAt == default) return "Συμπληρώστε ημερομηνία δημοσίευσης.";
        return null;
    }

    private string ResolveStoredFile(string storedFileName)
    {
        if (!string.Equals(storedFileName, Path.GetFileName(storedFileName), StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid release file name.");
        var root = Path.GetFullPath(StorageRoot);
        var path = Path.GetFullPath(Path.Combine(root, storedFileName));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid release file path.");
        return path;
    }

    private void DeleteFiles(IEnumerable<string> storedFileNames)
    {
        foreach (var storedFileName in storedFileNames)
        {
            try
            {
                var path = ResolveStoredFile(storedFileName);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch (Exception ex) { logger.LogWarning(ex, "Could not delete release file {StoredFileName}", storedFileName); }
        }
    }

    private IActionResult Failure(string message)
    {
        TempData["Error"] = message;
        return RedirectToCatalog("release-admin");
    }

    private IActionResult RedirectToCatalog(string fragment)
        => Redirect($"{Url.Action(nameof(Index), "Versions")}#{fragment}");
}
