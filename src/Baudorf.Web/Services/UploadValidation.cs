namespace Baudorf.Web.Services;

/// <summary>Sichere Upload-Prüfung: erlaubte Typen + Größenlimit.</summary>
public static class UploadValidation
{
    public const long MaxBytes = 12 * 1024 * 1024; // 12 MB

    private static readonly HashSet<string> AllowedImageExt =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".avif", ".gif" };

    private static readonly HashSet<string> AllowedImageMime =
        new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp", "image/avif", "image/gif" };

    /// <summary>Prüft Bild-Upload; gibt false + Fehlertext zurück, wenn ungültig.</summary>
    public static bool IsValidImage(string fileName, string contentType, long length, out string? error)
    {
        error = null;
        if (length <= 0) { error = "Die Datei ist leer."; return false; }
        if (length > MaxBytes) { error = $"Die Datei ist zu groß (max. {MaxBytes / (1024 * 1024)} MB)."; return false; }

        var ext = Path.GetExtension(fileName);
        if (!AllowedImageExt.Contains(ext)) { error = $"Dateityp {ext} ist nicht erlaubt."; return false; }
        if (!AllowedImageMime.Contains(contentType)) { error = "Ungültiger Bildtyp."; return false; }
        return true;
    }
}
