using System.ComponentModel.DataAnnotations;

namespace Baudorf.Web.Models.Entities;

/// <summary>Bild / Video / virtueller Rundgang zu einer Property.</summary>
public class PropertyMedia
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public MediaType Typ { get; set; }

    [Required, MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(500)] public string? ThumbnailUrl { get; set; }
    [MaxLength(200)] public string? Alt { get; set; }

    public int Reihenfolge { get; set; }
    public bool IstCover { get; set; }
}
