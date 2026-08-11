#!/usr/bin/env bash
# ============================================================
#  JHON CALCAS / ContaNexo — Deploy backend + frontend
#  Ejecutar DESDE TU MAQUINA (Windows: Git Bash)
#
#  Uso:
#    ./deploy.sh user@IP_VPS [password_mysql]
#    ./deploy.sh root@203.0.113.10
# ============================================================
set -euo pipefail

SSH_TARGET="${1:?Uso: deploy.sh user@IP_VPS}"
REMOTE_DIR="/var/www/jhoncalcas"
SERVICE_NAME="jhoncalcas-api"
SSH_PORT="${SSH_PORT:-5277}"
MYSQL_PASS="${2:-ingAlv4r0}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
FRONTEND_DIR="$ROOT_DIR/frontend"
OUT_DIR="$SCRIPT_DIR/_out"

echo "==> [1/5] Compilando backend (dotnet publish linux-x64)..."
cd "$BACKEND_DIR"
rm -rf bin/Release obj/Release
dotnet publish "$BACKEND_DIR/ContaNexo.API.csproj" -c Release -r linux-x64 --self-contained false -o "$OUT_DIR/api"

echo "==> [2/5] Generando appsettings.json de producción..."
cp "$SCRIPT_DIR/appsettings.production.json" "$OUT_DIR/api/appsettings.json"

echo "==> [3/5] Compilando frontend Angular (production)..."
cd "$FRONTEND_DIR"
command -v npm >/dev/null 2>&1 || { echo "npm no encontrado en esta máquina"; exit 1; }
[ -d node_modules ] || npm install
rm -rf "$OUT_DIR/www"
npx ng build --configuration production --output-path "$OUT_DIR/www"
# El builder @angular/build:application genera la app dentro de browser/ → aplanar
if [ -d "$OUT_DIR/www/browser" ]; then
  mkdir -p "$OUT_DIR/www/_tmp"
  cp -r "$OUT_DIR/www/browser/." "$OUT_DIR/www/_tmp/"
  rm -rf "$OUT_DIR/www/browser"
  cp -r "$OUT_DIR/www/_tmp/." "$OUT_DIR/www/"
  rm -rf "$OUT_DIR/www/_tmp"
fi

echo "==> [4/5] Subiendo al servidor $SSH_TARGET (puerto $SSH_PORT)..."
ssh -p "$SSH_PORT" "$SSH_TARGET" "sudo rm -rf /tmp/api.new /tmp/www.new"
scp -P "$SSH_PORT" -r "$OUT_DIR/api" "$SSH_TARGET:/tmp/api.new"
scp -P "$SSH_PORT" -r "$OUT_DIR/www" "$SSH_TARGET:/tmp/www.new"
scp -P "$SSH_PORT" "$SCRIPT_DIR/conta-nexo-api.service" "$SSH_TARGET:/tmp/jhoncalcas-api.service"
scp -P "$SSH_PORT" "$SCRIPT_DIR/nginx-jhoncalcas.conf" "$SSH_TARGET:/tmp/nginx-jhoncalcas.conf"

echo "==> [5/5] Instalando en el servidor y arrancando servicio..."
ssh -p "$SSH_PORT" "$SSH_TARGET" sudo bash -s <<EOF
set -e
mkdir -p $REMOTE_DIR
rm -rf $REMOTE_DIR/api $REMOTE_DIR/www
mv /tmp/api.new $REMOTE_DIR/api
mv /tmp/www.new $REMOTE_DIR/www
mkdir -p $REMOTE_DIR/api/wwwroot/uploads/gallery

# Base de datos JHON CALCAS independiente
mysql <<SQL
CREATE DATABASE IF NOT EXISTS calcas_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'administrador'@'localhost' IDENTIFIED BY '$MYSQL_PASS';
ALTER USER 'administrador'@'localhost' IDENTIFIED BY '$MYSQL_PASS';
GRANT ALL PRIVILEGES ON calcas_db.* TO 'administrador'@'localhost';
FLUSH PRIVILEGES;
SQL

# Unit systemd independiente para no tocar ContaNexo
cp /tmp/jhoncalcas-api.service /etc/systemd/system/$SERVICE_NAME.service
systemctl daemon-reload
chown -R www-data:www-data $REMOTE_DIR/api $REMOTE_DIR/www
systemctl enable $SERVICE_NAME
systemctl restart $SERVICE_NAME

# Config Nginx independiente para JHON CALCAS
cp /tmp/nginx-jhoncalcas.conf /etc/nginx/sites-available/jhoncalcas
ln -sf /etc/nginx/sites-available/jhoncalcas /etc/nginx/sites-enabled/jhoncalcas
nginx -t
systemctl reload nginx

sleep 2
echo "==> Estatus API:"
systemctl status $SERVICE_NAME --no-pager | head -n 10 || true
echo "==> Últimos logs:"
journalctl -u $SERVICE_NAME -n 8 --no-pager || true
exit 0
EOF

echo ""
echo "======================================================"
echo " BACKEND Y FRONTEND PUBLICADOS OK"
echo "  API: http://127.0.0.1:5002 (dentro del VPS)"
echo "  Siguiente: Nginx + SSL (README-DESPLIEGUE.md paso 5)"
echo "======================================================"
