using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Markt &amp; Insights — Blogartikel.</summary>
public class BlogPost
{
    public int Id { get; set; }

    [Required, MaxLength(200)] public string Titel { get; set; } = string.Empty;
    [Required, MaxLength(220)] public string Slug { get; set; } = string.Empty;

    [MaxLength(400)] public string? Excerpt { get; set; }
    public string? Body { get; set; }

    [MaxLength(500)] public string? CoverUrl { get; set; }
    [MaxLength(80)] public string? Kategorie { get; set; }
    [MaxLength(300)] public string? Tags { get; set; }  // komma-getrennt

    // SEO
    [MaxLength(200)] public string? MetaTitle { get; set; }
    [MaxLength(320)] public string? MetaDescription { get; set; }

    public bool IstVeroeffentlicht { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
