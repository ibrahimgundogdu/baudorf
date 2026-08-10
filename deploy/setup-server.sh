#!/usr/bin/env bash
# ==========================================================================
# Baudorf — Hetzner / Ubuntu 24.04 sunucu kurulumu (root ile bir kez çalıştır)
# Kullanım:   sudo bash deploy/setup-server.sh baudorf.de
#             (domain boş bırakılırsa nginx tüm istekleri karşılar; TLS'i
#              domain hazır olunca ayrıca certbot ile ekleyebilirsin)
# ==========================================================================
set -euo pipefail

DOMAIN="${1:-_}"
APP_DIR="/var/www/baudorf"
APP_USER="baudorf"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "==> 1/6  Sistem paketleri"
apt-get update -y
apt-get install -y nginx curl ca-certificates

echo "==> 2/6  .NET 10 ASP.NET Core runtime"
if ! /usr/local/bin/dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App 10\."; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
  ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
fi
/usr/local/bin/dotnet --list-runtimes | grep "AspNetCore.App 10" || { echo "HATA: .NET 10 runtime kurulamadı"; exit 1; }

echo "==> 3/6  Uygulama kullanıcısı + klasörler"
id -u "$APP_USER" >/dev/null 2>&1 || useradd -r -s /usr/sbin/nologin "$APP_USER"
mkdir -p "$APP_DIR" "$APP_DIR/keys" "$APP_DIR/wwwroot/uploads"
chown -R "$APP_USER:$APP_USER" "$APP_DIR"

echo "==> 4/6  systemd servisi"
cp "$SCRIPT_DIR/baudorf.service" /etc/systemd/system/baudorf.service
systemctl daemon-reload
systemctl enable baudorf

echo "==> 5/6  Nginx reverse proxy"
sed "s/__DOMAIN__/${DOMAIN}/g" "$SCRIPT_DIR/nginx-baudorf.conf" > /etc/nginx/sites-available/baudorf
ln -sf /etc/nginx/sites-available/baudorf /etc/nginx/sites-enabled/baudorf
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl reload nginx

echo "==> 6/6  Firewall (varsa ufw)"
if command -v ufw >/dev/null 2>&1; then
  ufw allow 22/tcp  || true
  ufw allow 80/tcp  || true
  ufw allow 443/tcp || true
fi

cat <<EOF

==========================================================================
 Kurulum tamam. Sıradaki adımlar:

 1) Yayın dosyalarını yükle (local'den):
      dotnet publish src/Baudorf.Web/Baudorf.Web.csproj -c Release -o publish
      # publish/ içeriğini sunucuda $APP_DIR içine kopyala (scp/rsync)
      #   ÖNEMLİ: publish/appsettings.Development.json'u SİL (secret sızıntısı)

 2) Prod ayarlarını oluştur:
      $APP_DIR/appsettings.Production.json
      (ConnectionStrings:DefaultConnection + Seed:AdminPassword + Turnstile + SMTP)

 3) İzinler + servisi başlat:
      chown -R $APP_USER:$APP_USER $APP_DIR
      systemctl restart baudorf
      systemctl status baudorf --no-pager

 4) HTTPS (domain DNS'i bu sunucuya bakınca):
      apt-get install -y certbot python3-certbot-nginx
      certbot --nginx -d $DOMAIN -d www.$DOMAIN

 Detaylı rehber: DEPLOY-LINUX.md
==========================================================================
EOF
