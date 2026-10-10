# PetCare Hogar Constitution

## Core Principles

### I. Test-First (NO NEGOCIABLE)

Toda la lógica de la aplicación se desarrolla test-first:

- Cada comportamiento nuevo o modificado DEBE arrancar con un test xUnit que lo describa y que
  falle antes de escribir la implementación (ciclo Rojo → Verde → Refactor).
- No se mergea lógica (reglas de dominio, vencimientos, generación de instancias, envío de
  alarmas, autorización) sin tests que la cubran.
- Los tests que dependen del tiempo DEBEN usar un `TimeProvider` controlable; nunca
  `DateTime.Now`/`UtcNow`.
- Un bug se corrige escribiendo primero el test que lo reproduce.

**Rationale**: los vencimientos, cambios de día (UTC−3) y alarmas son la parte crítica del
producto y solo son confiables si están fijados por tests escritos antes del código.

### II. Sin secretos en el código

- Ninguna credencial (contraseñas SMTP, API keys, connection strings con secretos, claves de
  cifrado) DEBE aparecer en el código fuente, en `appsettings*.json` ni en ningún archivo
  versionado.
- Las credenciales DEBEN leerse de variables de entorno o, en desarrollo local, de
  user-secrets (`dotnet user-secrets`), que se guardan fuera del repositorio.
- Si falta una credencial requerida, la app DEBE fallar con un mensaje claro, sin valores por
  defecto que la reemplacen.
- En desarrollo, los emails se escriben en la consola; no se envían emails reales.

**Rationale**: el repositorio es compartido y su historial es permanente; un secreto
commiteado debe considerarse filtrado.

### III. Alcance acotado al PRD vigente

- Solo se implementan features respaldadas por un requerimiento (RF/RNF) del PRD vigente
  (`PRD.md`). Toda spec, plan y tarea DEBE trazar a los requerimientos que cubre.
- Lo listado en "Fuera de Alcance" del PRD NO DEBE implementarse.
- Si se necesita algo que el PRD no cubre, primero se actualiza el PRD (skill `create-prd`) y
  recién después se especifica e implementa.

**Rationale**: evita crecimiento no acordado del producto y mantiene el PRD como única fuente
de verdad sobre qué se construye.

## Restricciones técnicas

- Stack: .NET 10 LTS, C#, ASP.NET Core Razor Pages (sin Blazor), HTMX, SQLite + EF Core 10,
  autenticación por cookie con `PasswordHasher<T>`, MailKit, xUnit.
- Los cambios de esquema van exclusivamente por migraciones (`dotnet ef migrations add`); nada
  de `EnsureCreated()`, `Migrate()` al arrancar ni borrar `petcare.db`.

## Flujo de desarrollo

- Las features siguen el flujo Spec Kit: `/speckit-specify` → `/speckit-plan` →
  `/speckit-tasks` → `/speckit-implement`.
- El "Constitution Check" de cada plan DEBE verificar los tres principios: tests antes que
  implementación en `tasks.md`, ausencia de secretos y trazabilidad a requerimientos del PRD.
- `dotnet test` DEBE pasar completo antes de cada commit a `main`.
- Los commits siguen Conventional Commits.

## Governance

- Esta constitución prevalece sobre cualquier otra práctica del proyecto. `AGENTS.md` contiene la
  guía operativa del día a día y no puede contradecirla.
- Enmiendas: se proponen con `/speckit-constitution`, documentando el cambio y su motivo, y se
  commitean como cambio aislado.
- Versionado semántico: MAJOR para quitar o redefinir principios; MINOR para agregar principios
  o secciones, o ampliar materialmente una guía; PATCH para aclaraciones y redacción.
- Toda revisión de código (PR/review) DEBE verificar el cumplimiento de los principios; un
  desvío requiere justificación explícita en el plan (sección de complejidad) o se rechaza.

**Version**: 1.0.0 | **Ratified**: 2026-10-10 | **Last Amended**: 2026-10-10
