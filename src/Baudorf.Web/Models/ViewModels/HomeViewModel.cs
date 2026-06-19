using Baudorf.Web.Models.Entities;

namespace Baudorf.Web.Models.ViewModels;

public class HomeViewModel
{
    public IReadOnlyList<Property> FeaturedObjekte { get; set; } = [];
    public IReadOnlyList<TeamMember> Team { get; set; } = [];
    public IReadOnlyList<BlogPost> Insights { get; set; } = [];
    public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

    /// <summary>Sichtbare Startseiten-Abschnitte nach Key (z. B. "hero", "philosophie").</summary>
    public IDictionary<string, HomeSection> Sections { get; set; } = new Dictionary<string, HomeSection>();

    public string S(string key, string fallback = "") =>
        Settings.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

    public HomeSection? Sec(string key) => Sections.TryGetValue(key, out var s) ? s : null;
}
