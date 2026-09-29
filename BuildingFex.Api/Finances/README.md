# Finances — Finanzas

**Responsable:** _asignar_

## Colecciones en db.json

- `fees`, `payments`, `receipts`, `financeSettings`, `kpi`
- `adminManagementExpenses`, `sharedUtilityServices`, `fixedPayoutRecipients`

## Frontend relacionado

- `Fronted-1/src/finances/infrastructure/*.js`
- `Fronted-1/src/finances/domain/receipt.js`

## Estado (Fase 4)

- [x] 8 aggregates + EF configuration
- [x] 8 compat controllers (`/fees`, `/payments`, `/receipts`, `/financeSettings`, `/kpi`, `/adminManagementExpenses`, `/sharedUtilityServices`, `/fixedPayoutRecipients`)
- [x] `DbJsonFinanceSeeder` desde `Fronted-1/server/db.json`
- [x] Migración `20260610200000_FinancesInitial`

## Nota

MercadoPago sigue en el frontend; el backend solo persiste pagos vía `POST /payments`.
