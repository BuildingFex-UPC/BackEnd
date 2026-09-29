# SocialSpaces — Espacios comunes y reservas

**Responsable:** _asignar_

## Colecciones en db.json

- `socialSpaces`, `reservations`

## Frontend relacionado

- `Fronted-1/src/socialSpaces/infrastructure/spacesApi.js`
- `Fronted-1/src/socialSpaces/infrastructure/reservationsApi.js`

## Endpoints

| Ruta | Descripción |
|------|-------------|
| `/socialSpaces` | CRUD espacios |
| `/reservations` | CRUD reservas + invitados |

## Estado (Fase 5)

- [x] Aggregates `SocialSpace`, `Reservation`
- [x] `SocialSpacesCompatController` — GET/POST/PATCH/DELETE + GET `/{id}`
- [x] `ReservationsCompatController` — GET (filtros), GET `/{id}`, POST, PATCH, DELETE
- [x] Validación de solapamiento en `POST /reservations`
- [x] Lookup público por `guestInviteToken` (sin `ownerAdminId`)
- [x] `DbJsonSocialSpacesSeeder`
- [x] Migración `20260610300000_SocialSpacesInitial`
