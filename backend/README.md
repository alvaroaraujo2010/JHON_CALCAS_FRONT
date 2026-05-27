# Jhon Calcas API

Backend REST en **ASP.NET Core 9** (ContaNexo) con JWT, EF Core y MySQL.

## Requisitos

- .NET SDK 9
- MySQL 8

## Configuracion

```bash
cp appsettings.example.json appsettings.json
```

Edite la cadena de conexion y la clave JWT en `appsettings.json`.

## Base de datos

Base MySQL: **`calcas_db`** (misma estructura y seed que ContaNexo; solo cambia el nombre).

```bash
mysql -u administrador -p < database/schema.sql
```

La API crea tablas con EF (`EnsureCreated`) y datos iniciales al arrancar si la base existe y el usuario tiene permisos.

## Ejecutar

```bash
dotnet run
```

API: `http://localhost:5001`

## Repositorio relacionado

Frontend Angular: repositorio `frontend` del mismo proyecto.
