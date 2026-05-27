# ContaNexo

**El nexo entre su contabilidad e inventario**

Proyecto dividido en **dos repositorios independientes** (mismo estilo que AGLitigios / SISPARK):

| Repo | Carpeta local | GitHub |
|------|---------------|--------|
| **ContaNexo-backend** | `backend/` | https://github.com/alvaroaraujo2010/ContaNexo-backend |
| **ContaNexo-frontend** | `frontend/` | https://github.com/alvaroaraujo2010/ContaNexo-frontend |

## Publicar en GitHub (una sola vez)

Desde **Git Bash**, con sesion activa en GitHub (`gh auth login`):

```bash
bash scripts/publish-github.sh
```

Ese script crea ambos repos en `alvaroaraujo2010` y hace push.

## Desarrollo local

```bash
# Backend
cd backend
cp appsettings.example.json appsettings.json   # editar credenciales MySQL
dotnet run

# Frontend (otra terminal)
cd frontend
npm install
npm start
```

- API: http://localhost:5001  
- App: http://localhost:4200  

## Otros

- `LOGO/` — assets de marca (fuente)
- `database/schema.sql` — copia tambien en `backend/database/`
