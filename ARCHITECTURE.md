# Arquitectura

## Estructura y responsabilidades

```
backend/
├── src/
│   ├── LoanChallenge.Core/              ← sin dependencias externas
│   │   ├── Domain/                      ← entidades (Customer, LoanApplication, LoanRequest)
│   │   │   └── Rules/                   ← motor de reglas y las reglas de negocio
│   │   └── Application/                 ← LoanApplicationService (caso de uso), ILoanRepository, evento
│   └── LoanChallenge.Api/               ← orquestación + infraestructura
│       ├── Controllers/                 ← controlador ligero (1 endpoint)
│       ├── Contracts/                   ← DTO de entrada con validación
│       ├── Data/                        ← LoanDbContext, repositorio EF Core, entidad outbox
│       ├── Workers/                     ← OutboxProcessor (BackgroundService)
│       ├── Services/                    ← cliente HTTP del servicio externo, lista negra
│       └── Options/                     ← opciones de configuración
backend/tests/LoanChallenge.Tests/       ← xUnit
external-service/                        ← simulación del servicio externo
frontend/                                ← Next.js (formulario, /approved, /denied)
```

**Regla de dependencias:** todo apunta hacia dentro. `Core` no conoce EF Core ni HTTP; `Api` implementa las interfaces del `Core` (`ILoanRepository`, `ILoanBlacklist`). Base de datos, cliente HTTP y entrega de eventos son reemplazables sin tocar el negocio. El controlador solo valida (atributos) y delega en `LoanApplicationService`.

## Motor de reglas

- Interfaz `ILoanDenialRule` (`Code`, `Reason`, `AppliesTo(LoanRequest)`). Cada regla es una clase independiente: `NyStateRule` (estado `NY`) y `BlacklistedSsnRule` (SSN en la lista negra de configuración).
- `LoanRulesEngine.Decide(request)` recibe `IEnumerable<ILoanDenialRule>` (inyectado por DI) y devuelve la primera denegación que aplique; si ninguna, aprueba.
- La lista negra se lee de `appsettings.json` (`Blacklist:Ssns`) y se normaliza al cargar.

**Agregar una regla nueva** = crear la clase implementando `ILoanDenialRule` + una línea en `Program.cs`:

```csharp
builder.Services.AddSingleton<ILoanDenialRule, MyNewRule>();
```

No se modifica ninguna regla existente, ni el motor, ni el servicio, ni el controlador.

## Flujo del endpoint `POST /api/loan-applications`

1. El controlador valida el DTO (campos obligatorios, estado de 2 letras, SSN de 9 dígitos, monto > 0) y delega en `LoanApplicationService.SubmitAsync`.
2. El servicio decide con `LoanRulesEngine`. Si deniega, responde `200 { status: "Denied", denialCode, denialReason }` (una denegación es un resultado de negocio válido, no un error HTTP). No persiste nada.
3. Si aprueba:
   - Busca el cliente por **SSN normalizado** (`Ssn.Normalize` elimina guiones).
   - **Nuevo**: crea `Customer` + `LoanApplication` (monto, fecha) y un `LoanApprovalEvent` con `IsNewCustomer: true`.
   - **Recurrente**: actualiza los campos del `Customer` existente y el `RequestedAmount` de su `LoanApplication` existente (mismo `customerId` y `applicationId`); el evento sale con `IsNewCustomer: false`. Un SSN = un cliente = una solicitud (índice único en `Customers.Ssn`).
4. `LoanApplicationService` entrega todo a `ILoanRepository.SaveAsync`, que es el **único punto de persistencia**.

## La transacción (unidad de trabajo)

`EfLoanRepository.SaveAsync` agrega o rastrea el cliente, agrega o rastrea la solicitud y **agrega el mensaje outbox**, y ejecuta un único `SaveChangesAsync()`. EF Core envuelve cada `SaveChanges` en una transacción de SQLite real: cliente, solicitud y evento se guardan juntos o no se guarda nada.

**Si fallan la base de datos o el guardado del evento** → ninguna de las tres escrituras queda persistida y el endpoint devuelve 500; el formulario muestra un error y el cliente no queda guardado a medias.

**La "publicación" del evento ocurre dentro de esta transacción** (escribir el mensaje outbox): cumple el requisito de publicar junto con el guardado sin enviar red en el request HTTP que responde al formulario.

## Evento en segundo plano → servicio externo

- `OutboxProcessor` (`BackgroundService`) consulta `OutboxMessages` cada 5 s, toma los `Pending` por orden y envía `PUT api/customers/{ssn}` al servicio externo con la carga útil JSON (`LoanApprovalEvent`). Al éxito marca `Processed`; al fallo incrementa `Attempts` y deja el mensaje `Pending` para el siguiente ciclo (el poll actúa como reintento natural con backoff). Tras `MaxAttempts` (5) lo marca `Failed` y registra el error.
- **Contrato del servicio externo — por qué un solo `PUT` upsert:** el SSN es la clave natural del registro. Un `PUT /api/customers/{ssn}` crea el registro si no existe y lo actualiza si existe. Esto es idempotente por diseño: un reintento o una entrega duplicada nunca crean un registro de más, y el cliente recurrente se actualiza automáticamente (nunca se duplica en el servicio externo). Separar `POST` (alta) y `PUT` (actualización) exigiría que el backend conociera y transmitiera el estado del cliente y rompería la idempotencia de los reintentos.
- **Qué pasa si el servicio externo cae:** el evento queda `Pending` y se reintenta en cada ciclo mientras el servicio siga abajo; los datos no se pierden ni el `200` al formulario depende de la entrega.

## Frontend

- `/` formulario (componente cliente): validación inmediata, SSN formateado `123-45-6789`, desplegable de estados (el demo de NY es evidente), estado de carga y errores del servidor.
- La respuesta del backend decide la navegación: `Denied` → `/denied?reason=<code>` (la página mapea el código a un mensaje legible); `Approved` → `/approved?...` con confirmación de alta o actualización.
- Sin librerías de UI: `fetch` directo contra la API (CORS habilitado para `localhost:3000`).

## Pruebas

- `RulesEngineTests`: reglas del motor (NY, lista negra, caso insensible, formato de SSN).
- `ApiTests` (`WebApplicationFactory<Program>` con SQLite real en archivo temporal): aprobación persistiendo cliente + solicitud + evento outbox, denegaciones por NY y lista negra sin persistir nada, **cliente recurrente** (mismas ids, monto actualizado, dos eventos outbox), SSN con/sin guiones como mismo cliente, y validación → 400. El worker se desactiva en pruebas vía configuración.

## Concesiones (omisiones decididas)

- **SQLite en vez de PostgreSQL/SQL Server:** transacciones reales con EF Core y cero infraestructura; es la opción más simple para un entorno local. No usa el provider en memoria de EF Core.
- **Lista negra en configuración** (no en DB): más simple y suficiente para el demo; migrar a tabla sería cambiar `ConfigLoanBlacklist` sin tocar el resto.
- **Servicio externo sin persistencia** (en memoria): es una simulación que devuelve 200; la persistencia real no aporta nada al contrato y los índices de memoria lo muestran para el demo.
- **Outbox con polling en vez de broker/mensajería:** con una sola instancia del proceso cubre el requisito (entrega en segundo plano, reintentos, no bloquea el request) sin añadir piezas.
- **`EnsureCreated()` en vez de migraciones EF:** el esquema es de 3 tablas y la base se regenera borrando el archivo; las migraciones añadirían complejidad sin beneficio en este contexto.
- **Sin auth, sin Docker, sin logging estructurado:** no aportan a la evaluación y el enunciado los marca como opcionales.
- **SSN en claro en la DB:** en producción se cifraría en reposo; aquí se prioriza la legibilidad del demo.

## Posibles mejoras (no implementadas)

- Contrato `POST`/`PUT` separados en el servicio externo con un campo `operation` en el evento, si se pidiera mantener idempotencia por otra vía.
- Índice sobre `OutboxMessages.Status` ya existe; con más volumen se pasaría a procesar por lotes con transacción por mensaje.
- Máscara total del SSN en el frontend (hoy se muestra parcialmente).