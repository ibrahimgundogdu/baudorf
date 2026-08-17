using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Baudorf.Web.Services;

public interface IRedirectService
{
    /// <summary>Passende aktive Weiterleitung für Pfad(+Query) oder null.</summary>
    Task<(int Id, string Nach, int Code)?> MatchAsync(string path, string? query);

    /// <summary>Trefferzähler einer ausgelösten Weiterleitung erhöhen (fire-and-forget).</summary>
    Task BumpAsync(int id);

    /// <summary>Nicht gefundenen Pfad protokollieren (aggregiert je Pfad).</summary>
    Task LogNotFoundAsync(string pathAndQuery, string? referrer);

    /// <summary>Redirect-Cache nach Admin-Änderungen leeren.</summary>
    void InvalidateCache();

    /// <summary>Pfad normalisieren: klein, führender Slash, ohne abschließenden Slash.</summary>
    static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var p = path.Trim().ToLowerInvariant();
        var q = "";
        var qi = p.IndexOf('?');
        if (qi >= 0) { q = p[qi..]; p = p[..qi]; }
        if (!p.StartsWith('/')) p = "/" + p;
        if (p.Length > 1) p = p.TrimEnd('/');
        if (p.Length == 0) p = "/";
        return p + q;
    }
}

/// <summary>Singleton: hält die aktiven Weiterleitungen 60 s im Speicher (schneller Lookup).</summary>
public sealed class RedirectService(IServiceScopeFactory scopeFactory, IMemoryCache cache) : IRedirectService
{
    private const string CacheKey = "seo-redirect-map";

    public void InvalidateCache() => cache.Remove(CacheKey);

    private async Task<Dictionary<string, (int id, string nach, int code)>> GetMapAsync()
    {
        if (cache.TryGetValue(CacheKey, out Dictionary<string, (int, string, int)>? map) && map is not null)
            return map;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var list = await db.Redirects.AsNoTracking().Where(r => r.IstAktiv).ToListAsync();

        map = new(StringComparer.Ordinal);
        foreach (var r in list)
            map[IRedirectService.Normalize(r.VonPfad)] = (r.Id, r.NachPfad, r.Code);

        cache.Set(CacheKey, map, TimeSpan.FromSeconds(60));
        return map;
    }

    public async Task<(int Id, string Nach, int Code)?> MatchAsync(string path, string? query)
    {
        var map = await GetMapAsync();
        if (map.Count == 0) return null;

        // 1) Pfad + Query (z. B. "/?p=123"), 2) nur Pfad.
        if (!string.IsNullOrEmpty(query))
        {
            var pq = IRedirectService.Normalize(path + query);
            if (map.TryGetValue(pq, out var m1)) return m1;
        }
        if (map.TryGetValue(IRedirectService.Normalize(path), out var m2)) return m2;
        return null;
    }

    public async Task BumpAsync(int id)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Redirects.Where(r => r.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Treffer, r => r.Treffer + 1)
                    .SetProperty(r => r.LetzterTreffer, _ => DateTimeOffset.UtcNow));
        }
        catch { /* Statistik ist unkritisch */ }
    }

    public async Task LogNotFoundAsync(string pathAndQuery, string? referrer)
    {
        var key = IRedirectService.Normalize(pathAndQuery);
        if (key.Length > 400) key = key[..400];
        if (referrer is { Length: > 600 }) referrer = referrer[..600];

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hit = await db.NotFoundHits.FirstOrDefaultAsync(n => n.Pfad == key);
            if (hit is null)
            {
                db.NotFoundHits.Add(new NotFoundHit { Pfad = key, LetzterReferrer = referrer });
            }
            else
            {
                hit.Anzahl++;
                hit.Zuletzt = DateTimeOffset.UtcNow;
                if (!string.IsNullOrWhiteSpace(referrer)) hit.LetzterReferrer = referrer;
            }
            await db.SaveChangesAsync();
        }
        catch { /* z. B. seltener Unique-Race bei Erstanlage — ignorieren */ }
    }
}
