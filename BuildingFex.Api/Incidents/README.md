# Incidents — Incidencias

**Responsable:** _asignar_

## Frontend relacionado

- `Fronted-1/src/incidents/infrastructure/incidentsApi.js`
- `Fronted-1/src/incidents/domain/model/incident.entity.js`

## Endpoints (compat json-server)

| Método | Ruta |
|--------|------|
| GET | `/incidents?ownerAdminId=` |
| POST | `/incidents` |
| PUT | `/incidents/{id}` |
| DELETE | `/incidents/{id}` |

## Fase 3 — completado

- [x] Aggregate `Incident` con `ExternalId` (compat json-server)
- [x] Repositorio + command/query services
- [x] `IncidentsCompatController`
- [x] Migración `IncidentsInitial`
- [x] Seed desde `Fronted-1/server/db.json`
- [x] `incidentsApi.js` funciona sin cambios

## Campos (API / db.json)

- `id`, `ownerAdminId`, `residentId`, `residentName`, `description`, `status`, `createdAt`, `provider`
