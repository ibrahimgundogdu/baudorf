using Baudorf.Web.Data;
using Baudorf.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Baudorf.Web.Services;

public interface ILookupService
{
    /// <summary>Aktive Optionen einer Kategorie, nach Reihenfolge sortiert.</summary>
    IReadOnlyList<LookupOption> Options(string kategorie);

    /// <summary>Anzeige-Label zu einem Wert (Fallback: der Wert selbst bzw. leer).</summary>
    string Label(string kategorie, string? wert);

    void Invalidate();
}

/// <summary>Singleton: hält die Lookup-Optionen im Speicher (10 Min.), Invalidate bei Admin-Änderungen.</summary>
public sealed class LookupService(IServiceScopeFactory scopeFactory, IMemoryCache cache) : ILookupService
{
    private const string CacheKey = "lookup-options-all";

    public void Invalidate() => cache.Remove(CacheKey);

    private Dictionary<string, List<LookupOption>> GetAll()
    {
        if (cache.TryGetValue(CacheKey, out Dictionary<string, List<LookupOption>>? map) && map is not null)
            return map;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var all = db.LookupOptions.AsNoTracking()
            .OrderBy(l => l.Kategorie).ThenBy(l => l.Reihenfolge).ThenBy(l => l.Label)
            .ToList();

        map = all.GroupBy(l => l.Kategorie).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        cache.Set(CacheKey, map, TimeSpan.FromMinutes(10));
        return map;
    }

    public IReadOnlyList<LookupOption> Options(string kategorie)
    {
        if (GetAll().TryGetValue(kategorie, out var list))
            return list.Where(l => l.IstAktiv).ToList();
        return [];
    }

    public string Label(string kategorie, string? wert)
    {
        if (string.IsNullOrWhiteSpace(wert)) return string.Empty;
        if (GetAll().TryGetValue(kategorie, out var list))
        {
            var hit = list.FirstOrDefault(l => string.Equals(l.Wert, wert, StringComparison.OrdinalIgnoreCase));
            if (hit is not null) return hit.Label;
        }
        return wert; // Fallback: Rohwert anzeigen
    }
}
