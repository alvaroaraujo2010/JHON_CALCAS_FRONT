# Despliegue en VPS Ubuntu (.NET runtime) — SIN Docker

Aplicación: **JHON CALCAS / ContaNexo** — E-commerce + administrativo
Sitio público: `https://app.jhoncalcas.com.co`

## Arquitectura final

```
Usuario → https://app.jhoncalcas.com.co  (443, SSL Let's Encrypt)
                │
                ▼
             Nginx (reverse proxy + estáticos Angular)
                │
        ┌───────┴──────────┐
        ▼                  ▼
 / (frontend build)   /api → http://127.0.0.1:5002 (systemd: jhoncalcas-api)
                                     │
                                     ▼
                                  MySQL 8 (calcas_db)
```

- **Frontend Angular** compilado estático servido por Nginx (`/var/www/jhoncalcas`).
- **Backend .NET 9** publicado (`dotnet publish`) ejecutado como servicio `systemd` en el puerto 5002 (solo local).
- **Nginx** sirve los estáticos y hace proxy de `/api` y `/uploads` hacia el backend.
- **SSL** con Certbot (Let's Encrypt) para `app.jhoncalcas.com.co`.

## Archivos de este kit

| Archivo | Uso |
|---------|-----|
| `setup-server.sh` | Instala .NET SDK 9, MySQL 8, Nginx, Certbot (ejecutar UNA vez en el VPS) |
| `deploy.sh` | Compila backend + frontend y publica al servidor (ejecutar desde tu máquina) |
| `appsettings.production.json` | Config de producción de la API (CORS, URLs, MercadoPago) |
| `conta-nexo-api.service` | Unit de systemd para el backend |
| `nginx-jhoncalcas.conf` | Sitio de Nginx (proxy + estáticos + SSL) |

## Requisitos previos

- VPS **Ubuntu 22.04 o 24.04**, con acceso SSH (`root` o usuario con `sudo`).
- **2 GB RAM o más**, disco 20 GB.
- DNS: registro **A** de `app.jhoncalcas.com.co` → IP pública del VPS.
- Puerto 80/443 abiertos en el firewall del VPS y del proveedor.
- En tu máquina: `git`, `.NET SDK 9`, `Node.js 22` y `npm` instalados.

## Paso 1 — Preparar el VPS (una sola vez)

Sube y ejecuta el instalador en el servidor:

```bash
scp -P 5277 deploy/setup-server.sh user@IP_VPS:/tmp/
ssh -p 5277 user@IP_VPS "chmod +x /tmp/setup-server.sh && sudo /tmp/setup-server.sh"
```

Crea la base de datos y el usuario (anota las credenciales):

```bash
sudo mysql -e "CREATE DATABASE IF NOT EXISTS calcas_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
sudo mysql -e "CREATE USER IF NOT EXISTS 'administrador'@'localhost' IDENTIFIED BY 'COMO_DEFINIDO_EN_SETUP';"
sudo mysql -e "GRANT ALL PRIVILEGES ON calcas_db.* TO 'administrador'@'localhost'; FLUSH PRIVILEGES;"
```

## Paso 2 — Publicar la API

En tu máquina, dentro de `backend/`:

```bash
cd backend
rm -rf bin/Release
dotnet publish -c Release -r linux-x64 --self-contained false -o ../deploy/_out/api
```

Genera `appsettings.json` de producción y cópialo junto a la salida:

```bash
cp ../deploy/appsettings.production.json ../deploy/_out/api/appsettings.json
```

> Edita dentro de `appsettings.production.json` la cadena de conexión MySQL y los tokens reales de MercadoPago ANTES de copiarlo.

## Paso 3 — Publicar el frontend

```bash
cd frontend
npm ci
npx ng build --configuration production --output-path ../deploy/_out/www
```

## Paso 4 — Subir todo al servidor

```bash
scp -P 5277 -r deploy/_out/api user@IP_VPS:/var/www/jhoncalcas/api.new
scp -P 5277 -r deploy/_out/www user@IP_VPS:/var/www/jhoncalcas/www.new
```

## Paso 5 — Instalar y arrancar (en el VPS)

```bash
# Reemplazar carpetas
sudo rm -rf /var/www/jhoncalcas/api /var/www/jhoncalcas/www
sudo mv /var/www/jhoncalcas/api.new /var/www/jhoncalcas/api
sudo mv /var/www/jhoncalcas/www.new /var/www/jhoncalcas/www

# Servicio systemd
sudo cp deploy/conta-nexo-api.service /etc/systemd/system/jhoncalcas-api.service
sudo systemctl daemon-reload
sudo systemctl enable --now jhoncalcas-api
sudo journalctl -u jhoncalcas-api -f   # revisar arranque

# Nginx + SSL
sudo cp deploy/nginx-jhoncalcas.conf /etc/nginx/sites-available/jhoncalcas
sudo ln -sf /etc/nginx/sites-available/jhoncalcas /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx

# Certificado SSL (después de que el DNS apunte y puertos estén abiertos)
sudo certbot --nginx -d app.jhoncalcas.com.co
```

## Comandos útiles

- Estado API: `sudo systemctl status jhoncalcas-api`
- Logs API: `sudo journalctl -u jhoncalcas-api -n 100 -f`
- Reiniciar API: `sudo systemctl restart jhoncalcas-api`
- Logs Nginx: `sudo tail -f /var/log/nginx/jhoncalcas_error.log`

## Notas y seguridad

- El backend queda escuchando solo en `127.0.0.1:5002`, Nginx no lo expone.
- `appsettings.production.json` NO debe subirse a Git: contiene credenciales.
- Para el webhook de MercadoPago, Nginx lo redirige a la API automáticamente.
- Las imágenes de galería se guardan en `api/wwwroot/uploads/gallery` (hacer respaldo).
