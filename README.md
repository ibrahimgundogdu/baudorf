# Baudorf Immobilien GmbH — Web & Admin

Off-Market-Immobilienplattform (Velbert/NRW). ASP.NET Core MVC (.NET 10), EF Core Code-First,
SQL Server, ASP.NET Core Identity. Diskretes Luxus-Design (Gold/Ink/Creme, Inter).

Vollständiger Anforderungs-Brief: [`template/uploads/baudorf.md`](template/uploads/baudorf.md).
Architektur- & Konventionsleitfaden für die Entwicklung: [`CLAUDE.md`](CLAUDE.md).

## Stack

- .NET 10 · ASP.NET Core MVC (Razor) · C#
- EF Core 10 (Code-First) · SQL Server
- ASP.NET Core Identity (Rollen: Admin / Redakteur / Investor)
- Custom CSS + Design-Tokens · Alpine.js (CDN) · IntersectionObserver
- Medien: `IStorageService` → `wwwroot/uploads` (später R2/S3 austauschbar)

## Projektstruktur

```
src/Baudorf.Web/
  Areas/Admin/            # Verwaltungsbereich (Policy "AdminArea")
  Controllers/            # öffentliche MVC-Controller
  Data/                   # DbContext, Migrations, Roles, DbSeeder
  Models/                 # Enums + Entities
  Services/               # IStorageService, IEmailService
  wwwroot/                # css/site.css (Tokens), js, img/brand, uploads
```

## Lokal starten

1. **Connection String** in `src/Baudorf.Web/appsettings.Development.json` setzen
   (Datei ist git-ignoriert — niemals echte Zugangsdaten committen):

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=...;Database=Baudorf;User Id=...;Password=...;TrustServerCertificate=True"
     },
     "Seed": { "AdminEmail": "andrea.krueger@baudorf.de", "AdminPassword": "<sicheres-Passwort>" }
   }
   ```

2. Datenbank anlegen + Seed (läuft auch automatisch beim App-Start):

   ```bash
   dotnet ef database update --project src/Baudorf.Web
   ```

3. Starten:

   ```bash
   dotnet run --project src/Baudorf.Web
   ```

   Admin: `/Admin` (Login mit Seed-Admin). Öffentliche Seite: `/`.

## Migrationen

```bash
dotnet ef migrations add <Name> --project src/Baudorf.Web -o Data/Migrations
dotnet ef database update --project src/Baudorf.Web
```

## Umgebungsvariablen / Secrets

| Schlüssel                          | Zweck                          |
|------------------------------------|--------------------------------|
| `ConnectionStrings:DefaultConnection` | SQL Server Verbindung       |
| `Seed:AdminEmail` / `Seed:AdminPassword` | Initialer Admin-Account  |
| (später) SMTP / R2-Keys            | Mailversand / Cloud-Storage    |

Lokal via `appsettings.Development.json` oder User-Secrets; in Produktion via Umgebungsvariablen.

## Deploy (Ubuntu 24.04 + Nginx + systemd) — *folgt*

Ziel: Hetzner VPS, Kestrel hinter Nginx-Reverse-Proxy, TLS via Let's Encrypt, systemd-Service.
Detaillierte Schritte werden bei Produktionsreife ergänzt.
