using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Baudorf.Web.Services;

public interface ISocialLinkService
{
    /// <summary>Aktive Social-Links in Anzeigereihenfolge (für Footer und JSON-LD).</summary>
    IReadOnlyList<SocialLink> Active();

    void Invalidate();
}

/// <summary>
/// Singleton: hält die Social-Links im Speicher (10 Min.). Der Footer erscheint auf jeder
/// Seite — ohne Cache wäre das eine DB-Abfrage pro Request. Invalidate bei Admin-Änderungen.
/// </summary>
public sealed class SocialLinkService(IServiceScopeFactory scopeFactory, IMemoryCache cache) : ISocialLinkService
{
    private const string CacheKey = "social-links-active";

    public void Invalidate() => cache.Remove(CacheKey);

    public IReadOnlyList<SocialLink> Active()
    {
        if (cache.TryGetValue(CacheKey, out List<SocialLink>? list) && list is not null)
            return list;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        list = db.SocialLinks.AsNoTracking()
            .Where(s => s.IstAktiv)
            .OrderBy(s => s.Reihenfolge).ThenBy(s => s.Id)
            .ToList();

        cache.Set(CacheKey, list, TimeSpan.FromMinutes(10));
        return list;
    }
}
