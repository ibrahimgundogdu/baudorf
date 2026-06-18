using Baudorf.Web.Models;
using Baudorf.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Baudorf.Web.Data;

/// <summary>Migration + Seed beim Start (Rollen, Admin, Team, Demo-Objekte, Einstellungen).</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));

        var cfg = sp.GetRequiredService<IConfiguration>();
        var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = sp.GetRequiredService<ILogger<ApplicationDbContext>>();
        var adminEmail = cfg["Seed:AdminEmail"] ?? "andrea.krueger@baudorf.de";
        var adminPwd = cfg["Seed:AdminPassword"];

        // Kein Default-Passwort im Quellcode: fehlt es, wird der Admin nicht angelegt.
        if (string.IsNullOrWhiteSpace(adminPwd))
        {
            logger.LogWarning("Seed:AdminPassword fehlt — Admin-Benutzer wird nicht angelegt. " +
                              "Setze ihn in appsettings.Development.json oder via Umgebungsvariable.");
        }
        else if (await userMgr.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                AnzeigeName = "Andrea Krüger",
                IstFreigegeben = true,
                FreigegebenAm = DateTime.UtcNow
            };
            var res = await userMgr.CreateAsync(admin, adminPwd);
            if (res.Succeeded)
                await userMgr.AddToRoleAsync(admin, Roles.Admin);
        }

        await SeedTeamAsync(db);
        await SeedSettingsAsync(db);
        await SeedPropertiesAsync(db);

        await db.SaveChangesAsync();
    }

    private static async Task SeedTeamAsync(ApplicationDbContext db)
    {
        if (await db.TeamMembers.AnyAsync()) return;

        db.TeamMembers.AddRange(
            new TeamMember
            {
                Name = "Andrea Krüger",
                Rolle = "Geschäftsführerin · Immobilienmaklerin · Dipl.-Ing. Architektin",
                Bio = "Seit 1994 in der Immobilienwelt zuhause — von der Architektur zur stillen Vermarktung. " +
                      "Diskretion, Verbindlichkeit und ein Gespür für besondere Objekte.",
                FotoUrl = "/img/team/andrea.jpg",
                Reihenfolge = 1
            },
            new TeamMember
            {
                Name = "Ayla",
                Rolle = "Immobilien-Spürnase",
                Bio = "Unsere Bürohündin und gute Seele des Hauses — immer dabei, immer charmant.",
                FotoUrl = "/img/team/ayla.jpg",
                Reihenfolge = 2
            });
    }

    private static async Task SeedSettingsAsync(ApplicationDbContext db)
    {
        if (await db.SiteSettings.AnyAsync()) return;

        void Set(string key, string value, string? desc = null) =>
            db.SiteSettings.Add(new SiteSetting { Key = key, Value = value, Beschreibung = desc });

        Set("contact.company", "Baudorf Immobilien GmbH");
        Set("contact.street", "Auf der Egge 68");
        Set("contact.city", "42555 Velbert");
        Set("contact.phone", "0177 – 838 78 98");
        Set("contact.email", "andrea.krueger@baudorf.de");
        Set("contact.hours", "Mo–Fr 09:00–18:00");
        Set("brand.slogan", "Still, wirkungsvoll, mit Stil.");
        Set("brand.claim", "Diskret. Exklusiv. Direkt.");
    }

    private static async Task SeedPropertiesAsync(ApplicationDbContext db)
    {
        if (await db.Properties.AnyAsync()) return;

        db.Properties.AddRange(
            new Property
            {
                Titel = "Faktor 20,7 — 40 Wohneinheiten · KfW 40 / QNG",
                Slug = "faktor-20-7-40-wohneinheiten-kfw40-qng",
                Art = PropertyKind.Kapitalanlage,
                Status = PropertyStatus.OffMarket,
                Region = "NRW",
                Land = "Deutschland",
                Wohnflaeche = 2809,
                Grundstuecksflaeche = 2940,
                Baujahr = 2027,
                Einheiten = 40,
                Faktor = 20.7m,
                Kaufpreis = 9_750_000m,
                Zustand = "Neubau",
                Energieklasse = "A+",
                Beschreibung = "Hochwertiges Neubauprojekt mit 40 Wohneinheiten nach KfW 40 / QNG-Standard. " +
                               "Details für vorgemerkte Investoren nach Freigabe.",
                IstOffMarket = true,
                IstFeatured = true,
                IstVeroeffentlicht = true,
                MetaTitle = "Off-Market Kapitalanlage NRW — Faktor 20,7"
            },
            new Property
            {
                Titel = "Neubau-Senioreneinrichtung — 80 stationäre Plätze",
                Slug = "neubau-senioreneinrichtung-80-plaetze-herne",
                Art = PropertyKind.Investment,
                Status = PropertyStatus.Verfuegbar,
                Region = "Herne, NRW",
                Land = "Deutschland",
                Grundstuecksflaeche = 7565,
                Baujahr = 2023,
                Einheiten = 80,
                Zustand = "Neubau",
                Energieklasse = "A",
                Beschreibung = "Moderne Senioreneinrichtung mit 80 stationären Plätzen — langfristig vermietet, " +
                               "institutionelle Kapitalanlage.",
                IstOffMarket = false,
                IstFeatured = true,
                IstVeroeffentlicht = true,
                MetaTitle = "Pflegeimmobilie Herne — 80 Plätze"
            });
    }
}
