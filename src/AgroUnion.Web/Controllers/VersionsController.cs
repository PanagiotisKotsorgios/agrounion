using System.Security.Claims;
using AgroUnion.Domain.Entities;
using AgroUnion.Infrastructure.Persistence;
using AgroUnion.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroUnion.Web.Controllers;

[Route("versions")]
public sealed class VersionsController(
    AgroUnionDbContext db,
    IWebHostEnvironment environment,
    ILogger<VersionsController> logger) : Controller
{
    private const long MaxFileSize = 250L * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".msi", ".exe", ".apk", ".dmg", ".pkg", ".deb", ".rpm", ".gz", ".pdf"
    };

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
    private string StorageRoot => Path.Combine(environment.ContentRootPath, "App_Data", "releases");

    [AllowAnonymous, HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var isAdmin = User.IsInRole(RoleNames.Admin);
        var query = db.PlatformReleases.AsNoTracking().Include(x => x.Assets).AsQueryable();
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
                x.IsPublished,
                x.Assets.OrderBy(a => a.DisplayName).Select(a => new ReleaseAssetViewModel(
                    a.Id,
                    a.DisplayName,
                    a.OriginalFileName,
                    a.TargetPlatform,
                    a.SizeBytes,
                    a.DownloadCount,
                    !string.IsNullOrWhiteSpace(a.GitHubDownloadUrl))).ToList())).ToList()
        });
    }

    [AllowAnonymous, HttpGet("download/{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var asset = await db.PlatformReleaseAssets
            .Include(x => x.PlatformRelease)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (asset?.PlatformRelease is null || (!asset.PlatformRelease.IsPublished && !User.IsInRole(RoleNames.Admin)))
            return NotFound();

        if (!string.IsNullOrWhiteSpace(asset.GitHubDownloadUrl))
        {
            if (!TryValidateGitHubUrl(asset.GitHubDownloadUrl, out var githubUrl)) return NotFound();
            asset.DownloadCount++;
            await db.SaveChangesAsync(ct);
            return Redirect(githubUrl);
        }

        if (string.IsNullOrWhiteSpace(asset.StoredFileName)) return NotFound();
        var path = ResolveStoredFile(asset.StoredFileName);
        if (!System.IO.File.Exists(path))
        {
            logger.LogWarning("Release asset {AssetId} points to missing file {StoredFileName}", asset.Id, asset.StoredFileName);
            return NotFound();
        }

        asset.DownloadCount++;
        await db.SaveChangesAsync(ct);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return File(stream, asset.ContentType, asset.OriginalFileName, enableRangeProcessing: true);
    }

    [Authorize(Policy = "AdminOnly"), HttpPost("create")]
    [RequestFormLimits(MultipartBodyLengthLimit = 262_144_000)]
    [RequestSizeLimit(262_144_000)]
    public async Task<IActionResult> Create(ReleaseUploadForm form, CancellationToken ct)
    {
        var error = ValidateRelease(form);
        if (error is not null) return Failure(error);

        var version = form.Version.Trim();
        if (await db.PlatformReleases.AnyAsync(x => x.Version == version, ct))
            return Failure($"Η έκδοση {version} υπάρχει ήδη. Προσθέστε νέο αρχείο στην υπάρχουσα έκδοση.");

        var release = new PlatformRelease
        {
            Version = version,
            Title = form.Title.Trim(),
            ReleaseNotes = form.ReleaseNotes.Trim(),
            PublishedAtUtc = DateTime.SpecifyKind(form.PublishedAt, DateTimeKind.Local).ToUniversalTime(),
            IsPublished = form.IsPublished,
            CreatedByUserId = UserId
        };

        var savedFiles = new List<string>();
        try
        {
            foreach (var file in form.Files.Where(x => x.Length > 0))
            {
                var asset = await SaveFileAsync(file, form.TargetPlatform, savedFiles, ct);
                release.Assets.Add(asset);
            }
            if (TryValidateGitHubUrl(form.GitHubDownloadUrl, out var githubUrl))
                release.Assets.Add(CreateGitHubAsset(githubUrl, form.GitHubAssetName, form.TargetPlatform));

            db.PlatformReleases.Add(release);
            await db.SaveChangesAsync(ct);
            TempData["Success"] = $"Η έκδοση {release.Version} καταχωρίστηκε με {release.Assets.Count} αρχείο/α.";
            return RedirectToCatalog("release-catalog");
        }
        catch (Exception ex)
        {
            DeleteFiles(savedFiles);
            logger.LogError(ex, "Failed to create platform release {Version}", version);
            return Failure("Η έκδοση δεν αποθηκεύτηκε. Ελέγξτε τα αρχεία και δοκιμάστε ξανά.");
        }
    }

    [Authorize(Policy = "AdminOnly"), HttpPost("asset")]
    [RequestFormLimits(MultipartBodyLengthLimit = 262_144_000)]
    [RequestSizeLimit(262_144_000)]
    public async Task<IActionResult> AddAsset(ReleaseAssetUploadForm form, CancellationToken ct)
    {
        var release = await db.PlatformReleases.Include(x => x.Assets).SingleOrDefaultAsync(x => x.Id == form.ReleaseId, ct);
        if (release is null) return NotFound();

        var error = ValidateAssets(form.Files, form.GitHubDownloadUrl, form.GitHubAssetName);
        if (error is not null) return Failure(error);

        var savedFiles = new List<string>();
        try
        {
            foreach (var file in form.Files.Where(x => x.Length > 0))
                release.Assets.Add(await SaveFileAsync(file, form.TargetPlatform, savedFiles, ct));
            if (TryValidateGitHubUrl(form.GitHubDownloadUrl, out var githubUrl))
                release.Assets.Add(CreateGitHubAsset(githubUrl, form.GitHubAssetName, form.TargetPlatform));

            await db.SaveChangesAsync(ct);
            TempData["Success"] = $"Τα νέα αρχεία προστέθηκαν στην έκδοση {release.Version}.";
            return RedirectToCatalog($"release-{release.Id}");
        }
        catch (Exception ex)
        {
            DeleteFiles(savedFiles);
            logger.LogError(ex, "Failed to add assets to platform release {ReleaseId}", release.Id);
            return Failure("Τα αρχεία δεν αποθηκεύτηκαν. Δοκιμάστε ξανά.");
        }
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

    [Authorize(Policy = "AdminOnly"), HttpPost("asset/{id:guid}/delete")]
    public async Task<IActionResult> DeleteAsset(Guid id, CancellationToken ct)
    {
        var asset = await db.PlatformReleaseAssets.FindAsync([id], ct);
        if (asset is null) return NotFound();
        var storedFileName = asset.StoredFileName;
        var releaseId = asset.PlatformReleaseId;
        db.PlatformReleaseAssets.Remove(asset);
        await db.SaveChangesAsync(ct);
        if (!string.IsNullOrWhiteSpace(storedFileName)) DeleteFiles([storedFileName]);
        TempData["Success"] = "Το αρχείο αφαιρέθηκε από την έκδοση.";
        return RedirectToCatalog($"release-{releaseId}");
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
        return ValidateAssets(form.Files, form.GitHubDownloadUrl, form.GitHubAssetName);
    }

    private static string? ValidateAssets(IEnumerable<IFormFile> files, string? githubUrl, string? githubAssetName)
    {
        var uploaded = files.Where(x => x.Length > 0).ToList();
        if (uploaded.Count == 0 && string.IsNullOrWhiteSpace(githubUrl)) return "Επιλέξτε τουλάχιστον ένα αρχείο ή προσθέστε σύνδεσμο GitHub.";
        foreach (var file in uploaded)
        {
            if (string.IsNullOrWhiteSpace(Path.GetFileName(file.FileName)) || Path.GetFileName(file.FileName).Length > 180)
                return "Το όνομα κάθε αρχείου πρέπει να είναι από 1 έως 180 χαρακτήρες.";
            if (file.Length > MaxFileSize) return $"Το αρχείο {Path.GetFileName(file.FileName)} υπερβαίνει το όριο των 250 MB.";
            var extension = Path.GetExtension(Path.GetFileName(file.FileName));
            if (!AllowedExtensions.Contains(extension)) return $"Ο τύπος αρχείου {extension} δεν επιτρέπεται.";
        }
        if (!string.IsNullOrWhiteSpace(githubUrl) && !TryValidateGitHubUrl(githubUrl, out _)) return "Ο εξωτερικός σύνδεσμος πρέπει να είναι έγκυρο HTTPS URL του github.com.";
        if (!string.IsNullOrWhiteSpace(githubUrl) && string.IsNullOrWhiteSpace(githubAssetName)) return "Συμπληρώστε όνομα για το αρχείο GitHub.";
        if (githubAssetName?.Trim().Length > 180) return "Το όνομα του αρχείου GitHub είναι πολύ μεγάλο.";
        return null;
    }

    private async Task<PlatformReleaseAsset> SaveFileAsync(IFormFile file, string? targetPlatform, ICollection<string> savedFiles, CancellationToken ct)
    {
        Directory.CreateDirectory(StorageRoot);
        var originalName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = ResolveStoredFile(storedName);
        await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous))
            await file.CopyToAsync(output, ct);
        savedFiles.Add(storedName);
        return new PlatformReleaseAsset
        {
            DisplayName = Path.GetFileNameWithoutExtension(originalName),
            OriginalFileName = originalName,
            TargetPlatform = CleanPlatform(targetPlatform),
            StoredFileName = storedName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType[..Math.Min(file.ContentType.Length, 150)],
            SizeBytes = file.Length
        };
    }

    private static PlatformReleaseAsset CreateGitHubAsset(string url, string? assetName, string? targetPlatform)
    {
        var name = assetName!.Trim();
        return new PlatformReleaseAsset
        {
            DisplayName = Path.GetFileNameWithoutExtension(name),
            OriginalFileName = name,
            TargetPlatform = CleanPlatform(targetPlatform),
            GitHubDownloadUrl = url
        };
    }

    private static string CleanPlatform(string? value)
        => string.IsNullOrWhiteSpace(value) ? "Όλες οι πλατφόρμες" : value.Trim()[..Math.Min(value.Trim().Length, 100)];

    private static bool TryValidateGitHubUrl(string? value, out string url)
    {
        url = string.Empty;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return false;
        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) return false;
        url = uri.AbsoluteUri;
        return true;
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
