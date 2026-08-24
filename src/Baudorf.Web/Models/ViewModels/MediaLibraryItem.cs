namespace Baudorf.Web.Models.ViewModels;

/// <summary>
/// Mediathek-Grid'inin tek satırı: disk + DB (MediaAsset) birleşimi. Diskte olup DB'de
/// olmayan dosyalar da, DB'de olup diski kaybolan kayıtlar da burada görünür.
/// </summary>
public class MediaLibraryItem
{
    /// <summary>MediaAsset kaydı varsa Id; yoksa (yalnızca diskte olan "yetim" dosya) null.</summary>
    public int? AssetId { get; set; }

    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Alt { get; set; }

    public bool IsVideo { get; set; }

    /// <summary>Fiziksel dosya diskte var mı? false → "Datei fehlt".</summary>
    public bool FileExists { get; set; }

    /// <summary>Bir içerikte (Objekt/Startseite/Team/News/Leistung) kullanılıyor mu?</summary>
    public bool InUse { get; set; }

    /// <summary>Kullanıldığı yerlerin etiketi (ör. "Objekt, Startseite").</summary>
    public string? UsedIn { get; set; }

    public DateTimeOffset SortDate { get; set; }
}
