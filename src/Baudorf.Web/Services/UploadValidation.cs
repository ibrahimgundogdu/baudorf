namespace Baudorf.Web.Services;

/// <summary>Sichere Upload-Prüfung: erlaubte Typen + Größenlimit (Bilder &amp; Videos).</summary>
public static class UploadValidation
{
    public const long MaxBytes = 12 * 1024 * 1024;         // 12 MB (Bilder)
    public const long MaxVideoBytes = 80 * 1024 * 1024;    // 80 MB (Videos)

    private static readonly HashSet<string> AllowedImageExt =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".avif", ".gif" };

    private static readonly HashSet<string> AllowedImageMime =
        new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp", "image/avif", "image/gif" };

    private static readonly HashSet<string> AllowedVideoExt =
        new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".webm", ".ogg", ".ogv", ".mov" };

    public static bool IsVideoExtension(string fileName) =>
        AllowedVideoExt.Contains(Path.GetExtension(fileName));

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

    /// <summary>Prüft Video-Upload.</summary>
    public static bool IsValidVideo(string fileName, string contentType, long length, out string? error)
    {
        error = null;
        if (length <= 0) { error = "Die Datei ist leer."; return false; }
        if (length > MaxVideoBytes) { error = $"Das Video ist zu groß (max. {MaxVideoBytes / (1024 * 1024)} MB)."; return false; }

        var ext = Path.GetExtension(fileName);
        if (!AllowedVideoExt.Contains(ext)) { error = $"Dateityp {ext} ist nicht erlaubt."; return false; }
        if (!(contentType ?? "").StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        { error = "Ungültiger Videotyp."; return false; }
        return true;
    }

    /// <summary>Prüft Bild- oder Video-Upload je nach Dateiendung.</summary>
    public static bool IsValidMedia(string fileName, string contentType, long length, out string? error) =>
        IsVideoExtension(fileName)
            ? IsValidVideo(fileName, contentType, length, out error)
            : IsValidImage(fileName, contentType, length, out error);
}
