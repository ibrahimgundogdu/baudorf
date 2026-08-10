# Deploy — Hetzner (Ubuntu 24.04) + Nginx + systemd

Gerçek prod hedefi: **Hetzner Cloud VPS + Ubuntu 24.04**. Kestrel arkada (127.0.0.1:5000),
önünde **Nginx** reverse proxy, TLS **Let's Encrypt** (certbot). E-posta All-Inkl'de kalır —
yayına alırken **yalnızca web'in `A` kaydı** yeni sunucu IP'sine döner, **MX'e dokunulmaz**.

Dosyalar: [deploy/setup-server.sh](deploy/setup-server.sh) · [deploy/baudorf.service](deploy/baudorf.service) · [deploy/nginx-baudorf.conf](deploy/nginx-baudorf.conf)

---

## 1) Hetzner'de sunucu oluştur (Cloud Console)
- **console.hetzner.cloud** → Proje → **Add Server**
- **Location:** Nürnberg 🇩🇪 (stok yoksa Falkenstein / veya tip **CAX11** ARM)
- **Image:** **Ubuntu 24.04 LTS**
- **Type:** **CX22** (2 vCPU / 4 GB) — bu uygulamaya fazlasıyla yeter
- **SSH Key:** varsa ekle (en güvenli). Yoksa root parolası e-posta/console'dan gelir.
- **Firewall:** 22 (SSH), 80 (HTTP), 443 (HTTPS) açık olsun.
- Oluştur → **IP adresini** al.

Bağlan (Windows PowerShell): `ssh root@SUNUCU_IP`

## 2) Kodu sunucuya al + kurulum scriptini çalıştır
Kolay yol — repo'yu sunucuya klonla (setup scripti repodan çalışır):
```bash
apt-get update -y && apt-get install -y git
git clone https://github.com/ibrahimgundogdu/baudorf.git /opt/baudorf-src
cd /opt/baudorf-src
sudo bash deploy/setup-server.sh baudorf.de     # domain'i kendine göre yaz
```
Script şunları yapar: .NET 10 ASP.NET Core runtime, Nginx, `baudorf` kullanıcısı,
`/var/www/baudorf` klasörleri, systemd servisi, Nginx reverse proxy.

## 3) Uygulamayı publish et + yükle (local — Windows)
```powershell
# repo kökünde
dotnet publish src/Baudorf.Web/Baudorf.Web.csproj -c Release -o publish

# ÖNEMLİ: secret sızıntısını önle — dev config'i publish'ten sil
Remove-Item publish/appsettings.Development.json -ErrorAction SilentlyContinue

# sunucuya kopyala (scp Windows 10+ ile hazır gelir)
scp -r publish/* root@SUNUCU_IP:/var/www/baudorf/
```
> Not: `wwwroot/uploads` (admin medyası) ve `appsettings.Production.json` sunucuda kalır —
> tekrar deploy ederken bunların üzerine yazma. rsync kullanırsan: `--exclude appsettings.Production.json --exclude wwwroot/uploads`.

## 4) Prod ayarları (sunucuda, bir kez)
`/var/www/baudorf/appsettings.Production.json` oluştur:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=185.187.170.107;Initial Catalog=BaudorfWebDB;User ID=baudorfadmin;Password=...;TrustServerCertificate=True"
  },
  "Seed": { "AdminEmail": "andrea.krueger@baudorf.de", "AdminPassword": "<guclu>" },
  "Turnstile": { "SiteKey": "...", "SecretKey": "..." }
  // SMTP eklenecekse buraya
}
```
> **DSGVO/SQL:** Uzak SQL `185.187.170.107` — SQL firewall'unda Hetzner IP'sine izin ver.
> O SQL'in AB'de olduğunu teyit et (içinde PII var).

## 5) Başlat
```bash
chown -R baudorf:baudorf /var/www/baudorf
systemctl restart baudorf
systemctl status baudorf --no-pager
journalctl -u baudorf -n 50 --no-pager      # log/hata
```
Uygulama ilk açılışta migration + seed uygular (idempotent).
Test: `curl -I http://127.0.0.1:5000` (200/302 dönmeli).

## 6) HTTPS (domain DNS'i sunucuya bakınca)
```bash
apt-get install -y certbot python3-certbot-nginx
certbot --nginx -d baudorf.de -d www.baudorf.de
```
certbot 443 bloğunu + otomatik http→https yönlendirmeyi Nginx'e ekler, sertifikayı yeniler.

## 7) DNS geçişi (en son — mail kesintisiz)
1. All-Inkl (KAS) mevcut kayıtları **not al** (özellikle MX).
2. Sunucu + site + TLS **çalıştığını doğrula** (geçici olarak `hosts` dosyasıyla test edebilirsin).
3. KAS'ta **sadece `A` kaydını** (ve gerekiyorsa `www`) yeni Hetzner IP'sine çevir. **MX'e DOKUNMA** → `@baudorf.de` maili kesintisiz kalır.
4. Yayılma (~dakikalar–saat) sonrası `https://baudorf.de` canlı.

---

## Tekrar deploy (güncelleme)
```powershell
dotnet publish src/Baudorf.Web/Baudorf.Web.csproj -c Release -o publish
Remove-Item publish/appsettings.Development.json -ErrorAction SilentlyContinue
scp -r publish/* root@SUNUCU_IP:/var/www/baudorf/
```
```bash
ssh root@SUNUCU_IP "chown -R baudorf:baudorf /var/www/baudorf && systemctl restart baudorf"
```
> İleride bu adımlar bir script'e/CI'a bağlanabilir; şimdilik manuel akış yeterli.

## Kod notu
`Program.cs`'e **ForwardedHeaders** eklendi (Nginx arkasında `X-Forwarded-Proto`/`-For`
tanınsın → HTTPS redirect döngüsü olmaz, gerçek client IP loglanır). Data-protection
anahtarları `/var/www/baudorf/keys` altında kalıcı (restart'ta logout olmaz).
