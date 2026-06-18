namespace Baudorf.Web.Data;

/// <summary>Identity-Rollen der Anwendung.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Redakteur = "Redakteur";
    public const string Investor = "Investor";

    public static readonly string[] All = [Admin, Redakteur, Investor];
}
