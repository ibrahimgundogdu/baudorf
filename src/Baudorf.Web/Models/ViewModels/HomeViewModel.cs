using Baudorf.Web.Models.Entities;

namespace Baudorf.Web.Models.ViewModels;

public class HomeViewModel
{
    public IReadOnlyList<Property> FeaturedObjekte { get; set; } = [];
    public IReadOnlyList<TeamMember> Team { get; set; } = [];
    public IReadOnlyList<BlogPost> Insights { get; set; } = [];
    public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

    public string S(string key, string fallback = "") =>
        Settings.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;
}
