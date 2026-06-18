namespace Baudorf.Web.Models.Entities;

/// <summary>Merkliste-Eintrag: ein vom Benutzer gespeichertes Objekt.</summary>
public class Favorite
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
