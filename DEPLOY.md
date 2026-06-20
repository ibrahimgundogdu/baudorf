# Deploy — VDS (Windows Server + IIS) · GitHub Actions Self-hosted Runner

Amaç: **`main`'e `git push` → sunucu otomatik build alıp yayınlasın.**
Artık manuel kopyalama yok. Runner, VDS üzerinde bir Windows servisi olarak çalışır;
kodu çeker, `dotnet publish` ile derler ve IIS klasörüne kopyalar.

Pipeline dosyası: [.github/workflows/deploy.yml](.github/workflows/deploy.yml).
Dosyanın en üstündeki iki değeri gerekirse kendi sunucuna göre düzenle: `SITE_PATH` ve `APP_POOL`.

---

## A. Sunucuda tek seferlik hazırlık (VDS üzerinde)

### 1. .NET 10 kurulumu
- **ASP.NET Core 10 Hosting Bundle** (IIS modülü + runtime) — zorunlu; IIS'in uygulamayı
  çalıştırması için gerekir. Kurduktan sonra `iisreset` çalıştır.
- **.NET 10 SDK** — build sunucuda yapılacağı için gerekli.

PowerShell ile doğrula:
```powershell
dotnet --version          # örn. 10.0.xxx
dotnet --list-runtimes    # Microsoft.AspNetCore.App 10.x içermeli
```

### 2. IIS sitesi + uygulama havuzu (app pool) oluştur
```powershell
Import-Module WebAdministration
$site = "Baudorf"
$path = "C:\inetpub\baudorf"
New-Item -ItemType Directory -Force -Path $path | Out-Null

# App pool: "No Managed Code" (ASP.NET Core, ANCM modülü üzerinden çalışır)
New-WebAppPool -Name $site
Set-ItemProperty IIS:\AppPools\$site -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\$site -Name startMode -Value "AlwaysRunning"

# Site (Port 80; HTTPS/domain aşağıda)
New-Website -Name $site -PhysicalPath $path -ApplicationPool $site -Port 80
```
Domain/HTTPS: sonradan `senin-domainin.de` için binding ekle ve sertifika
(örn. win-acme / Let's Encrypt) kur.

### 3. Production yapılandırması (sunucuda kalır, ASLA üzerine yazılmaz)
`C:\inetpub\baudorf\appsettings.Production.json` dosyasını oluştur:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=...;Initial Catalog=BaudorfWebDB;User ID=...;Password=...;TrustServerCertificate=True"
  },
  "Seed": { "AdminEmail": "andrea.krueger@baudorf.de", "AdminPassword": "<guclu-parola>" }
}
```
> Bu dosya pipeline'da senkronizasyondan **hariç tutulur** (`/XF`), yani her deploy'da
> korunur. Secret'lar **kesinlikle** Git deposuna girmez.

IIS altında ASP.NET Core otomatik olarak `ASPNETCORE_ENVIRONMENT=Production` kullanır.

### 4. Klasör izinleri
App pool'un (özellikle `wwwroot/uploads` için) okuma/yazma iznine ihtiyacı var:
```powershell
icacls "C:\inetpub\baudorf" /grant "IIS AppPool\Baudorf:(OI)(CI)M" /T
```

### 5. GitHub Actions Self-hosted Runner kurulumu
GitHub → Repo **Settings → Actions → Runners → New self-hosted runner → Windows**.
Orada gösterilen komutları uygula (indir + `config.cmd`). Önemli noktalar:
- `config.cmd` sırasında **label** olarak `windows` ekle (pipeline bunu kullanıyor:
  `runs-on: [self-hosted, windows]`).
- **Servis** olarak kur ki sürekli çalışsın:
  ```powershell
  .\svc install
  .\svc start
  ```
- Runner servisi IIS'i yönetebilmeli (app pool restart). Öneri: servis hesabı olarak
  yerel **admin** haklı bir hesap kullan (veya LocalSystem).

---

## B. Bundan sonra deploy

```bash
git push origin main
```
→ GitHub runner'ı tetikler → `dotnet publish` → `app_offline.htm` → dosyalar
senkronize edilir (Prod config & uploads korunur) → app pool recycle → site online.

Durumu GitHub **Actions** sekmesinden izlersin. Elle tetiklemek için orada **Run workflow**.

---

## C. Pipeline'ın bilerek dokunmadıkları
- `appsettings.Production.json` (sunucudaki secret'ların)
- `wwwroot/uploads` (admin'den yüklenen medya)

## D. İlk çalıştırma
Uygulama ilk açılışta EF Core migration'larını otomatik uygular ve
rolleri/admin'i/içeriği seed'ler (idempotent). Production veritabanının erişilebilir
ve `appsettings.Production.json` içindeki connection string'in doğru olduğundan emin ol.

---

## E. Workflow'da düzenleyebileceğin iki değer
[.github/workflows/deploy.yml](.github/workflows/deploy.yml) en üstte:
- `SITE_PATH` — IIS sitesinin fiziksel yolu (varsayılan `C:\inetpub\baudorf`)
- `APP_POOL` — IIS uygulama havuzu adı (varsayılan `Baudorf`)
