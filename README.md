# Challenge Full-Stack: flujo de solicitud de préstamo (.NET + Next.js)

> **Vídeo demo:** _[https://www.loom.com/share/644b710ec563491b94afd18f7a0bbaf1]_

Implementación del challenge "Prueba para realizar en casa: Ingeniero Full-Stack (.NET + Next.js)".

Flujo: el usuario rellena un formulario en **Next.js**, un **motor de reglas** (backend .NET) decide si la solicitud se aprueba o se deniega, las aprobadas se persisten en **SQLite** (cliente + solicitud + evento outbox en una única transacción real) y un **procesador en segundo plano** entrega el evento a un **servicio externo simulado** vía HTTP.

## Componentes y puertos

| Componente | Tecnología | Puerto |
|---|---|---|
| Backend (`backend/`) | ASP.NET Core 10 + EF Core + SQLite | http://localhost:5000 |
| Servicio externo (`external-service/`) | Minimal API .NET (simulación) | http://localhost:5100 |
| Frontend (`frontend/`) | Next.js 16 (App Router, TypeScript, Tailwind) | http://localhost:3000 |

Requiere **.NET SDK 10** y **Node.js ≥ 20**.

## Cómo ejecutarlo localmente

Tres terminales:

**1) Servicio externo simulado**
```bash
cd external-service
dotnet run
```

**2) Backend (API + worker de eventos)**
```bash
cd backend/src/LoanChallenge.Api
dotnet run
```
La base de datos `loan.db` se crea sola en este directorio al primer arranque. Para empezar de cero: `rm loan.db` y vuelve a arrancar.

**3) Frontend**
```bash
cd frontend
npm install
npm run dev
```

Abre **http://localhost:3000**. El frontend llama a la API en `http://localhost:5000` (se configura con `NEXT_PUBLIC_API_BASE_URL` en `frontend/.env.local`, ver `.env.local.example`).

Endpoints útiles para verificar la entrega de eventos:
- `GET http://localhost:5100/api/customers` — lo que el servicio externo ha recibido (los registros se actualizan por SSN, nunca se duplican).
- `POST http://localhost:5000/api/loan-applications` — contrato del formulario (Swagger: `GET /openapi/v1.json`).

## Cómo ejecutar las pruebas

```bash
cd backend
dotnet test
```

11 pruebas (xUnit): motor de reglas (5), endpoint + persistencia + cliente recurrente (6). Las pruebas contra la API usan `WebApplicationFactory` con una base SQLite real en un archivo temporal (transacciones reales; el worker outbox se desactiva en pruebas).

## Datos de prueba

SSN en la lista negra (config `Blacklist:Ssns` en `appsettings.json`): **`111-11-1111`** y **`222-22-2222`**.

| Escenario | Qué escribir en el formulario | Resultado |
|---|---|---|
| **Aprobación (nuevo cliente)** | SSN `333-33-3333`, estado distinto de NY (p. ej. California) | Página de aprobación; en `GET /api/customers` aparece un registro con `isNewCustomer: true` |
| **Denegación por estado NY** | Cualquier dato con estado **New York (NY)** | Página de denegación: "no disponibles en NY" |
| **Denegación por lista negra** | SSN `111-11-1111` | Página de denegación: "SSN en lista negra" |
| **Cliente recurrente** | SSN `333-33-3333` de nuevo, con otro monto | Página de aprobación indicando actualización; la DB sigue teniendo 1 cliente y 1 solicitud (monto actualizado), y el servicio externo actualiza el mismo registro (`isNewCustomer: false`) |

El SSN es la clave: se acepta con o sin guiones (`333-33-3333` = `333333333`).

## Estructura del repositorio

```
loan-challenge/
├── backend/
│   ├── LoanChallenge.sln
│   ├── src/
│   │   ├── LoanChallenge.Core/    # Dominio + aplicación (reglas, servicio, contratos)
│   │   └── LoanChallenge.Api/     # Controlador, EF Core, worker outbox, cliente HTTP
│   └── tests/LoanChallenge.Tests/ # xUnit
├── external-service/              # Simulación del servicio externo (minimal API)
└── frontend/                      # Next.js: formulario, páginas de aprobación/denegación
```

La arquitectura y las decisiones de diseño están documentadas en **[ARCHITECTURE.md](./ARCHITECTURE.md)**.

## Qué falta y por qué

- **Sin autenticación** (no requerida por el enunciado).
- **Sin Docker / CI** (se ejecuta con 3 comandos; SQLite no necesita contenedor).
- **Vídeo demo pendiente**: grabar con Loom o similar y reemplazar el enlace de arriba.
