namespace Baudorf.Web.Services;

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
}
