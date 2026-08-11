#!/usr/bin/env bash
# ============================================================
#  JHON CALCAS / ContaNexo — Instalación inicial del VPS
#  Ubuntu 22.04 / 24.04  — SIN Docker
#  Ejecutar UNA vez:  sudo bash setup-server.sh
# ============================================================
set -euo pipefail

MYSQL_PASSWORD="${1:-ingAlv4r0}"   # puede pasarse como argumento

echo "==> Actualizando sistema..."
apt-get update -y && apt-get upgrade -y

echo "==> Instalando dependencias base..."
apt-get install -y curl wget git ca-certificates gnupg lsb-release unzip \
    software-properties-common apt-transport-https

# ---------------- .NET SDK 9 ----------------
echo "==> Instalando .NET SDK 9..."
if ! dotnet --version >/dev/null 2>&1; then
  wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
  ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
fi
dotnet --version

# ---------------- MySQL 8 ----------------
echo "==> Instalando MySQL 8..."
if ! command -v mysql >/dev/null 2>&1; then
  apt-get install -y mysql-server
fi
echo "==> Configurando base y usuario..."
sudo mysql <<SQL
CREATE DATABASE IF NOT EXISTS calcas_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'administrador'@'localhost' IDENTIFIED BY '${MYSQL_PASSWORD}';
GRANT ALL PRIVILEGES ON calcas_db.* TO 'administrador'@'localhost';
FLUSH PRIVILEGES;
SQL
systemctl enable --now mysql

# ---------------- Node.js 22 + npm ----------------
echo "==> Instalando Node.js 22..."
if ! command -v node >/dev/null 2>&1; then
  curl -fsSL https://deb.nodesource.com/setup_22.x | bash -
  apt-get install -y nodejs
fi
node --version && npm --version

# ---------------- Nginx ----------------
echo "==> Instalando Nginx..."
apt-get install -y nginx
systemctl enable --now nginx

# ---------------- Certbot (SSL) ----------------
echo "==> Instalando Certbot..."
apt-get install -y certbot python3-certbot-nginx

# ---------------- Directorios del sitio ----------------
mkdir -p /var/www/jhoncalcas/api /var/www/jhoncalcas/www

echo ""
echo "======================================================"
echo " INSTALACION COMPLETADA"
echo "  - .NET:    $(dotnet --version)"
echo "  - Node:    $(node --version)"
echo "  - MySQL:   base calcas_db creada"
echo "  - Usuario: administrador / ${MYSQL_PASSWORD}"
echo "  - Nginx + Certbot listos"
echo ""
echo " Siguiente paso: subir la app (deploy.sh) y configurar"
echo " Nginx + SSL tal como indica README-DESPLIEGUE.md"
echo "======================================================"