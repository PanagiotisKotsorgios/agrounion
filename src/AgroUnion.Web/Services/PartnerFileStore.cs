using Microsoft.AspNetCore.Http;

namespace AgroUnion.Web.Services;

public sealed class PartnerFileStore(IWebHostEnvironment environment)
{
    private const long MaxPdfBytes = 20 * 1024 * 1024;
    private readonly string root = Path.Combine(environment.ContentRootPath, "App_Data", "partner-files");

    public async Task<string?> SavePdfAsync(IFormFile? file, string category, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > MaxPdfBytes) throw new InvalidOperationException("Το PDF δεν μπορεί να ξεπερνά τα 20 MB.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Επιτρέπονται αποκλειστικά αρχεία PDF.");

        await using var input = file.OpenReadStream();
        var signature = new byte[5];
        if (await input.ReadAsync(signature.AsMemory(0, signature.Length), ct) != signature.Length ||
            !signature.SequenceEqual("%PDF-"u8.ToArray()))
            throw new InvalidOperationException("Το αρχείο δεν αναγνωρίστηκε ως έγκυρο PDF.");

        var safeCategory = category is "invoices" or "documents" ? category : throw new InvalidOperationException("Μη έγκυρη κατηγορία αρχείου.");
        var directory = Path.Combine(root, safeCategory);
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}.pdf";
        var fullPath = Path.Combine(directory, fileName);
        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await output.WriteAsync(signature, ct);
        await input.CopyToAsync(output, ct);
        return $"partner-files/{safeCategory}/{fileName}";
    }

    public FileStream OpenRead(string storageKey) => new(Resolve(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read);

    public void Delete(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || !storageKey.StartsWith("partner-files/", StringComparison.OrdinalIgnoreCase)) return;
        var path = Resolve(storageKey);
        if (File.Exists(path)) File.Delete(path);
    }

    private string Resolve(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || !storageKey.StartsWith("partner-files/", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("Το αρχείο δεν βρέθηκε.");

        var relative = storageKey["partner-files/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, relative));
        var safeRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            throw new FileNotFoundException("Το αρχείο δεν βρέθηκε.");
        return fullPath;
    }
}
