# IAM — Identity & Access Management

**Responsable:** _asignar_

## Qué hace

- Login por email + contraseña (BCrypt + JWT)
- Registro de administradores de edificio
- Gestión de usuarios (admin / resident)
- Multi-tenancy vía `ownerAdminId`

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/v1/authentication/sign-in` | Login |
| POST | `/api/v1/authentication/register-admin` | Registro admin |
| GET | `/users` | Lista usuarios (compat json-server) |
| GET | `/users/{id}` | Usuario por id (external id string) |
| POST | `/users` | Crear residente (`role=resident`) |
| PATCH | `/users/{id}` | Actualizar credenciales del residente |
| DELETE | `/users/{id}` | Eliminar residente |

Query params compat en `GET /users`: `email`, `role`, `code`, `ownerAdminId`.

## Archivos clave

```
Iam/
├── Domain/Model/Aggregates/User.cs      ← entidad principal (+ ExternalId)
├── Domain/Repositories/IUserRepository.cs
├── Application/CommandServices/         ← login, registro
├── Application/QueryServices/           ← consultas
├── Infrastructure/Persistence/          ← EF Core + MySQL + seed db.json
└── Interfaces/Rest/                     ← controllers
```

## Fase 1 — completado

- [x] Migración EF: `IamInitial`
- [x] Seed de usuarios desde `Fronted-1/server/db.json`
- [x] JWT Bearer en `Program.cs`
- [x] Compat `GET /users` con filtros email/role/code/ownerAdminId
- [x] `authApi.js` conectado a `sign-in` y `register-admin`

## Fase 2 — completado

- [x] `POST /users` — crear residente (validación código duplicado por admin)
- [x] `PATCH /users/{id}` — credenciales (email + password BCrypt)
- [x] `DELETE /users/{id}` — eliminar residente
- [x] `residentsApi.js` funciona sin cambios

## Pendiente (fases siguientes)

- [ ] Middleware/`[Authorize]` en rutas protegidas
- [ ] Interceptor JWT en `apiClient.js` del frontend
