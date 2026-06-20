# Deployment — VDS (Windows Server + IIS) über GitHub Actions Self-hosted Runner

Ziel: **`git push` auf `main` → der Server baut und veröffentlicht automatisch.**
Kein manuelles Kopieren mehr. Der Runner läuft als Windows-Dienst auf dem VDS, holt
den Code, baut mit `dotnet publish` und kopiert in den IIS-Ordner.

Die Pipeline liegt in [.github/workflows/deploy.yml](.github/workflows/deploy.yml).
Zwei Werte oben in der Datei ggf. anpassen: `SITE_PATH` und `APP_POOL`.

---

## A. Einmalige Server-Vorbereitung (auf dem VDS)

### 1. .NET 10 installieren
- **ASP.NET Core 10 Hosting Bundle** (IIS-Modul + Runtime) — Pflicht, damit IIS die App ausführt.
  Danach `iisreset` ausführen.
- **.NET 10 SDK** — nötig, weil der Build auf dem Server läuft.

Prüfen in PowerShell:
```powershell
dotnet --version          # z. B. 10.0.xxx
dotnet --list-runtimes    # muss Microsoft.AspNetCore.App 10.x enthalten
```

### 2. IIS-Site + Anwendungspool anlegen
```powershell
Import-Module WebAdministration
$site = "Baudorf"
$path = "C:\inetpub\baudorf"
New-Item -ItemType Directory -Force -Path $path | Out-Null

# App-Pool: "No Managed Code" (ASP.NET Core läuft Out-of-Process/InProcess über das ANCM-Modul)
New-WebAppPool -Name $site
Set-ItemProperty IIS:\AppPools\$site -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\$site -Name startMode -Value "AlwaysRunning"

# Site (Port 80; HTTPS/Domain unten)
New-Website -Name $site -PhysicalPath $path -ApplicationPool $site -Port 80
```
Domain/HTTPS: später Binding für `deine-domain.de` hinzufügen und Zertifikat
(z. B. win-acme / Let's Encrypt) einrichten.

### 3. Produktions-Konfiguration anlegen (bleibt auf dem Server, wird NIE überschrieben)
Datei `C:\inetpub\baudorf\appsettings.Production.json` anlegen:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=...;Initial Catalog=BaudorfWebDB;User ID=...;Password=...;TrustServerCertificate=True"
  },
  "Seed": { "AdminEmail": "andrea.krueger@baudorf.de", "AdminPassword": "<sicheres-Passwort>" }
}
```
> Diese Datei ist in der Pipeline von der Synchronisierung **ausgeschlossen** (`/XF`),
> bleibt also bei jedem Deploy erhalten. Secrets gehören **nicht** ins Git-Repo.

ASP.NET Core nutzt im IIS automatisch `ASPNETCORE_ENVIRONMENT=Production`.

### 4. Ordnerrechte
Der App-Pool braucht Lese-/Schreibrechte (u. a. für `wwwroot/uploads`):
```powershell
icacls "C:\inetpub\baudorf" /grant "IIS AppPool\Baudorf:(OI)(CI)M" /T
```

### 5. GitHub Actions Self-hosted Runner installieren
GitHub → Repo **Settings → Actions → Runners → New self-hosted runner → Windows**.
Den dort gezeigten Befehlen folgen (Download + `config.cmd`). Wichtig:
- Beim `config.cmd` als **Labels** `windows` ergänzen (das nutzt die Pipeline:
  `runs-on: [self-hosted, windows]`).
- Als **Dienst** installieren, damit er dauerhaft läuft:
  ```powershell
  .\svc.sh install      # bzw. unter Windows: .\svc install  (siehe Runner-Anleitung)
  .\svc start
  ```
- Der Runner-Dienst muss IIS verwalten dürfen (App-Pool neu starten). Empfehlung:
  Dienstkonto mit lokalen Admin-Rechten verwenden (oder LocalSystem).

---

## B. Ab jetzt: deployen

```bash
git push origin main
```
→ GitHub triggert den Runner → `dotnet publish` → `app_offline.htm` → Dateien
synchronisieren (Prod-Config & Uploads bleiben) → App-Pool recyceln → online.

Status sichtbar im GitHub **Actions**-Tab. Manuell auslösbar dort über **Run workflow**.

---

## C. Was die Pipeline bewusst NICHT anfasst
- `appsettings.Production.json` (deine Server-Secrets)
- `wwwroot/uploads` (vom Admin hochgeladene Medien)

## D. Erststart
Beim ersten Start wendet die App automatisch die EF-Core-Migrationen an und seedet
Rollen/Admin/Inhalte (idempotent). Stelle sicher, dass die Produktions-Datenbank
erreichbar ist und der Connection String in `appsettings.Production.json` stimmt.
