# Implementation Plan: PetCare Hogar — seguimiento colaborativo del cuidado de mascotas

**Branch**: `001-petcare-hogar` | **Date**: 2026-10-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-petcare-hogar/spec.md`

## Summary

Web app mobile-first para que los cuidadores de un único hogar registren y vean el cuidado de
sus mascotas, con alarmas por email ante tareas vencidas. Enfoque técnico: una aplicación
ASP.NET Core Razor Pages con HTMX (polling de 10 s para el estado del día), SQLite con EF Core,
un procesador programado en segundo plano que genera instancias y envía emails a través de una
outbox con reintentos, y todas las reglas de tiempo calculadas desde el `TimeProvider` inyectado
para poder testearlas (detalle en [research.md](./research.md)).

## Technical Context

**Language/Version**: C# sobre .NET 10 LTS (SDK 10.0.401 instalado)

**Primary Dependencies**: ASP.NET Core Razor Pages (sin Blazor), HTMX 2.x (copiado en
`wwwroot`), EF Core 10 + `Microsoft.EntityFrameworkCore.Sqlite`, autenticación por cookie +
`PasswordHasher<T>`, MailKit; tests: xUnit, `Microsoft.AspNetCore.Mvc.Testing`,
`Microsoft.Extensions.TimeProvider.Testing`. Herramienta local: `dotnet-ef` (manifiesto en
`.config/dotnet-tools.json`, todavía no existe)

**Storage**: SQLite (`petcare.db`), esquema solo por migraciones de EF Core

**Testing**: xUnit: dominio (puro), servicios y procesador (SQLite en memoria con migraciones),
web (`WebApplicationFactory`); test-first (R11)

**Target Platform**: servidor Linux o Windows; clientes: navegador móvil ≥ 360 px

**Project Type**: aplicación web server-rendered (un solo proceso con servicio en segundo plano)

**Performance Goals**: cambios visibles en otros cuidadores en ≤ 15 s (RNF-04); emails
entregados al servicio en ≤ 5 min (RNF-01); instancias diarias en ≤ 5 min desde las 00:00
UTC−3 (RNF-05)

**Constraints**: hora UTC−3 fija sin horario de verano; toda hora desde `TimeProvider`; sin
`DateTime.Now`/`UtcNow`; sin `EnsureCreated()`/`Migrate()` al arrancar; sin credenciales en
`appsettings*.json`; emails a la consola en desarrollo

**Scale/Scope**: un hogar, pocos cuidadores y mascotas; ~10 pantallas/parciales

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Cómo lo cumple el plan | Estado |
|---|---|---|
| I. Test-First | Reglas puras en `Dominio/` y procesador ejecutable por ciclo para testear sin esperar; `FakeTimeProvider` en todos los tests de tiempo; tests nombrados por AC del PRD; `tasks.md` deberá poner cada test antes de su implementación (R11) | ✅ |
| II. Sin secretos | SMTP por variables de entorno o user-secrets; `ConsoleEmailSender` en desarrollo; la app falla al arrancar en producción si falta configuración (R7) | ✅ |
| III. Alcance del PRD | Cada decisión cita los FR del spec, que trazan a RF/RNF del PRD; nada fuera de alcance (sin SignalR, sin historial, sin roles) | ✅ |
| Restricciones técnicas | Stack de `AGENTS.md` sin agregados salvo paquetes de testing; `TimeProvider` en todo; esquema por migraciones; los tests aplican migraciones en su fixture, nunca la app al arrancar (R11) | ✅ |
| Flujo de desarrollo | Flujo Spec Kit respetado (spec → clarify → checklists → plan); `dotnet test` como compuerta antes de commitear a `main` | ✅ |

**Re-check post-diseño (Fase 1)**: sin violaciones. El modelo de datos no guarda marcas
calculables (R3), los contratos no exponen nada fuera de los FR, y la outbox (R7) cubre
reintentos y no-duplicación sin dependencias nuevas. La sección Complexity Tracking queda vacía.

## Project Structure

### Documentation (this feature)

```text
specs/001-petcare-hogar/
├── plan.md              # este archivo
├── research.md          # Fase 0: decisiones R1–R12
├── data-model.md        # Fase 1: entidades, estados, índices
├── quickstart.md        # Fase 1: cómo correr y validar
├── contracts/
│   ├── ui-routes.md     # páginas, handlers HTMX y mensajes de rechazo
│   └── emails.md        # contenido y reglas de envío de los emails
├── checklists/          # requirements.md, general.md
└── tasks.md             # Fase 2 (/speckit-tasks; no lo crea este comando)
```

### Source Code (repository root)

```text
PetCare.sln
.config/dotnet-tools.json            # dotnet-ef
src/PetCare.Web/
├── Program.cs                       # DI, cookie auth, TimeProvider, hosted service
├── Infra/                           # EventosCookie (renovación de sesión, 401 HTMX, motivo)
├── Dominio/                         # reglas puras: RelojHogar, GeneradorInstancias,
│                                    # ReglasVencimiento, CodigoHogar, PlantillasEmail, enums
├── Datos/                           # PetCareDbContext, configuraciones, Migrations/
├── Servicios/                       # Registro, Mascotas, Deberes, Cumplimientos,
│                                    # EstadoDelDia (consulta), Notificaciones
├── Email/                           # IEmailSender, ConsoleEmailSender, SmtpEmailSender
├── Programado/                      # ProcesadorProgramado, ProgramadorHostedService
├── Pages/                           # Index (estado del día), Cuenta/, Hogar/, Mascotas/,
│                                    # Shared/ (layout, parciales)
└── wwwroot/                         # css/, lib/htmx/
tests/PetCare.Tests/
├── Dominio/                         # tests unitarios de reglas puras
├── Servicios/                       # contra SQLite en memoria
├── Programado/                      # ciclos del procesador con FakeTimeProvider
├── Web/                             # WebApplicationFactory: acceso, HTMX, sesión
└── Infra/                           # FakeEmailSender, fixture de base de datos
```

**Structure Decision**: un proyecto web y un proyecto de tests (R1). `src/PetCare.Web` es la
ruta que ya usan los comandos de `AGENTS.md`.

## Complexity Tracking

Sin violaciones de la constitución que justificar.
