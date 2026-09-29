# BuildingFex API

Backend oficial de la aplicación **BuildingFex**. Proyecto independiente del frontend (`Fronted-1`) y de la plantilla académica (`learning-center-platform`).

## ¿Qué es este proyecto?

| Carpeta | Rol |
|---------|-----|
| `Fronted-1` | App web (Vue 3) — ya existe |
| `learning-center-platform` | Referencia de arquitectura — **no se modifica** |
| `buildingfex-api` | **Este proyecto** — API REST real con MySQL |

## Stack

- **C# / .NET 10** + ASP.NET Core
- **Entity Framework Core** + **MySQL**
- **JWT** + **BCrypt** (autenticación)
- **Swagger** (documentación)
- Arquitectura **DDD** + **CQRS** por bounded contexts

## Estructura del código

Cada módulo de negocio sigue las mismas 4 capas:

```
<Módulo>/
├── Domain/           → Entidades, reglas de negocio, interfaces de repositorio
├── Application/    → Casos de uso (CommandServices, QueryServices)
├── Infrastructure/ → EF Core, repositorios, JWT, BCrypt
└── Interfaces/     → Controllers REST, DTOs (Resources), Assemblers
```

### Módulos (bounded contexts)

| Módulo | Responsabilidad | Estado |
|--------|-----------------|--------|
| `Iam` | Login, registro admin, usuarios, roles, JWT | 🟢 Fase 1 lista |
| `Profiles` | Datos extendidos del administrador | ⚪ Por implementar |
| `Finances` | Pagos, cuotas, recibos, KPI financiero | 🟢 Fase 4 lista |
| `Incidents` | Incidencias de residentes | 🟢 Fase 3 lista |
| `SocialSpaces` | Espacios comunes y reservas | 🟢 Fase 5 lista |
| `Information` | Comunicados / anuncios | ⚪ Por implementar |
| `Support` | Chat de soporte | ⚪ Por implementar |
| `Team` | Trabajadores del edificio | ⚪ Por implementar |
| `Import` | Carga masiva de datos | ⚪ Por implementar |
| `Shared` | DbContext, repositorio base, Result, middleware | 🟢 Base lista |

## Cómo trabaja el equipo

1. **Cada persona toma un módulo** (ej. Finances, Incidents).
2. **Empieza por Domain**: define el aggregate y el repositorio (`IIncidentRepository`).
3. **Application**: implementa comandos y consultas (`CreateIncidentCommand`, `GetAllIncidentsQuery`).
4. **Infrastructure**: repositorio EF Core + configuración en `ModelBuilderExtensions`.
5. **Interfaces**: controller REST que expone los endpoints que el frontend ya usa.
6. **Registra** el módulo en `Program.cs` y en `AppDbContext.OnModelCreating`.
7. **Crea la migración**: `dotnet ef migrations add NombreModulo`.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MySQL 8 (local o Docker)

## Inicio rápido (Fases 1–5 — IAM + Residentes + Incidencias + Finanzas + SocialSpaces)

```bash
# 1. Levantar MySQL (Docker)
docker compose up -d

# 2. Restaurar, compilar y ejecutar API
cd BuildingFex.Api
dotnet restore
dotnet build
dotnet run
# HTTP: http://localhost:5001  |  HTTPS: https://localhost:7001
# Swagger: http://localhost:5001/swagger

# 3. Frontend (otra terminal)
cd ../Fronted-1
npm install
npm run dev
# Usa .env.development → VITE_API_BASE_URL=http://localhost:5001
```

Al arrancar, la API aplica migraciones y siembra usuarios desde `Fronted-1/server/db.json` (solo la primera vez).

### Probar login (Swagger)

`POST /api/v1/authentication/sign-in`

```json
{ "email": "admin@buildingfex.test", "password": "admin123" }
```

### Probar login y residentes (frontend)

1. Abrir `http://localhost:5173`
2. Admin: `admin@buildingfex.test` / `admin123`
3. Residente con credenciales: p. ej. `giuseppevillanueva15@gmail.com` / `naruto15`
4. Como admin: listar, crear y eliminar residentes en la sección Residentes
5. Flujo invitación: buscar por código → asignar email/contraseña (PATCH /users)
6. Incidencias: listar/crear como admin o reportar como residente

### Probar incidencias (Swagger)

`GET /incidents?ownerAdminId=admin-seed-1`

`POST /incidents`
```json
{
  "id": "incident-test-1",
  "description": "Fuga de agua",
  "status": "open",
  "provider": "",
  "ownerAdminId": "admin-seed-1"
}
```

## Conexión con el frontend

En `Fronted-1/.env.development` (ya incluido):

```env
VITE_API_BASE_URL=http://localhost:5001
```

El frontend hoy llama rutas planas (`/users`, `/incidents`). Los controllers de compatibilidad en `Interfaces/Rest/Compat/` mantienen esas rutas mientras migramos módulo por módulo.

## Orden de implementación sugerido

1. **Iam** — login real, JWT, registro admin ✅
2. **Residents** — CRUD residentes con `ownerAdminId` ✅
3. **Incidents** — primer módulo de negocio completo ✅
4. **Finances** — pagos y cuotas
5. **SocialSpaces** — espacios y reservas
6. Resto de módulos

## Multi-tenancy

Casi todos los datos pertenecen a un administrador de edificio (`ownerAdminId`). Cada query service debe filtrar por el admin del JWT.
