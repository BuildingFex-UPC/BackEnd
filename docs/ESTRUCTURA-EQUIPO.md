# Guía de trabajo en equipo — BuildingFex API

## Carpetas del workspace

```
Downloads/
├── Fronted-1/                 ← Frontend Vue (ya existe)
├── learning-center-platform/  ← Solo referencia (NO modificar)
└── buildingfex-api/           ← Backend nuevo (ESTE proyecto)
```

## Cómo se ve un módulo completo (ejemplo IAM)

```
Iam/
├── Domain/
│   ├── Model/
│   │   ├── Aggregates/User.cs       ← reglas de negocio
│   │   ├── Commands/SignInCommand.cs
│   │   └── IamError.cs
│   └── Repositories/IUserRepository.cs
├── Application/
│   ├── CommandServices/             ← escribe datos (login, crear)
│   └── QueryServices/               ← lee datos (listar, buscar)
├── Infrastructure/
│   ├── Persistence/EntityFrameworkCore/
│   │   ├── Repositories/UserRepository.cs
│   │   └── Configuration/Extensions/ModelBuilderExtensions.cs
│   ├── Hashing/BCrypt/
│   └── Tokens/Jwt/
└── Interfaces/
    └── Rest/
        ├── AuthenticationController.cs     ← /api/v1/...
        └── Compat/UsersCompatController.cs ← rutas del frontend
```

## Flujo de trabajo por persona

1. Lee el `README.md` de tu módulo
2. Revisa el `*Api.js` equivalente en `Fronted-1`
3. Revisa los datos en `Fronted-1/server/db.json`
4. Implementa Domain → Application → Infrastructure → Interfaces
5. Registra en `Program.cs` y `AppDbContext`
6. Crea migración y prueba en Swagger
7. Conecta el frontend cambiando `VITE_API_BASE_URL`

## División sugerida

| Persona | Módulo |
|---------|--------|
| Dev 1 | IAM + Residents |
| Dev 2 | Incidents |
| Dev 3 | Finances |
| Dev 4 | SocialSpaces + Information |
| Dev 5 | Support + Team + Import |

## Comandos útiles

```bash
cd BuildingFex.Api
dotnet restore
dotnet build
dotnet run

# Migraciones (cuando MySQL esté corriendo)
dotnet ef migrations add NombreCambio
dotnet ef database update
```
