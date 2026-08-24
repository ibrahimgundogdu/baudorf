namespace Baudorf.Web.Services;

/// <summary>Diskteki bir medya dosyasının özeti (Mediathek taraması için).</summary>
public record StoredMedia(string Url, DateTimeOffset Modified, long SizeBytes);

/// <summary>
/// Soyutlanmış medya depolama. Varsayılan impl yerel diske (wwwroot/uploads) yazar;
/// ileride Cloudflare R2 / S3 implementasyonu eklenebilir (arayüz değişmeden).
/// </summary>
public interface IStorageService
{
    /// <summary>Dosyayı kaydeder ve public erişilebilir göreli URL döndürür (örn. /uploads/...).</summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Verilen public URL'e ait dosyayı siler. Yoksa sessizce geçer.</summary>
    Task DeleteAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Var olan bir URL'in fiziksel dosyasını yeni içerikle DEĞİŞTİRİR (aynı ad/URL kalır),
    /// böylece bu URL'e bağlı tüm referanslar otomatik güncellenir.
    /// </summary>
    Task ReplaceAsync(string url, Stream content, CancellationToken ct = default);

    /// <summary>Verilen URL'in fiziksel dosyası diskte var mı?</summary>
    bool Exists(string url);

    /// <summary>Depodaki tüm medya dosyalarını (alt klasörler dahil) listeler.</summary>
    IReadOnlyList<StoredMedia> ListAll();
}
