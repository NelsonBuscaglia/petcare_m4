---

description: "Lista de tareas de implementación de PetCare Hogar"
---

# Tasks: PetCare Hogar — seguimiento colaborativo del cuidado de mascotas

**Input**: Design documents from `specs/001-petcare-hogar/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: OBLIGATORIOS. La constitución (Principio I, NO NEGOCIABLE) exige test-first: en cada
historia, las tareas de test van primero y deben **fallar** antes de implementar. Cada test se
nombra con el criterio de aceptación del PRD que verifica (por ejemplo `AC_29b_...`, R11).

**Organization**: tareas agrupadas por historia de usuario del spec (US1 a US9), en orden de
prioridad.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia a la que pertenece (US1 a US9)
- `W/` = `src/PetCare.Web/`, `T/` = `tests/PetCare.Tests/` (abreviaturas solo de esta lista;
  en el código, rutas completas)

## Reglas para todas las tareas

- Toda hora sale de `TimeProvider` o `RelojHogar`; prohibido `DateTime.Now`/`UtcNow`
  (`AGENTS.md`).
- Ningún cambio de esquema fuera de `dotnet ef migrations add`; nunca `EnsureCreated()` ni
  `Migrate()` en `W/`.
- Ninguna validación que no cite un RF/FR (Principio III). Los mensajes de rechazo son los de
  [contracts/ui-routes.md](./contracts/ui-routes.md#mensajes-de-rechazo-fr-051).
- Los tests de tiempo usan `FakeTimeProvider`; los de base usan
  `T/Infra/BaseDeDatosDePrueba.cs`; los web, `T/Infra/PetCareWebFactory.cs`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: crear la solución, los proyectos y las herramientas.

- [X] T001 Crear `PetCare.sln`, el proyecto Razor Pages `src/PetCare.Web/PetCare.Web.csproj` (`net10.0`, nullable habilitado) y el proyecto xUnit `tests/PetCare.Tests/PetCare.Tests.csproj` con referencia a `PetCare.Web`; agregar ambos a la solución
- [X] T002 Crear el manifiesto `.config/dotnet-tools.json` con `dotnet-ef` 10.x (`dotnet new tool-manifest` + `dotnet tool install dotnet-ef`)
- [X] T003 Agregar a `src/PetCare.Web/PetCare.Web.csproj` los paquetes `Microsoft.EntityFrameworkCore.Sqlite` 10.x, `Microsoft.EntityFrameworkCore.Design` 10.x y `MailKit`
- [X] T004 Agregar a `tests/PetCare.Tests/PetCare.Tests.csproj` los paquetes `Microsoft.AspNetCore.Mvc.Testing` 10.x, `Microsoft.Extensions.TimeProvider.Testing` y `Microsoft.EntityFrameworkCore.Sqlite` 10.x
- [X] T005 [P] Crear `.gitignore` en la raíz con `bin/`, `obj/`, `petcare.db`, `petcare.db-shm`, `petcare.db-wal` y `*.user`
- [X] T006 [P] Copiar HTMX 2.x minificado a `src/PetCare.Web/wwwroot/lib/htmx/htmx.min.js` (sin CDN, R12)
- [X] T007 [P] Configurar `src/PetCare.Web/appsettings.json` con `ConnectionStrings:PetCare = "Data Source=petcare.db"` y sin ninguna sección `Smtp` (Principio II)

**Checkpoint**: `dotnet build` y `dotnet test` corren (sin tests todavía).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: reloj del hogar, modelo de datos, base, autenticación y layout, que usan todas las
historias.

**⚠️ CRITICAL**: ninguna historia empieza antes de terminar esta fase.

### Tests primero

- [X] T008 [P] Escribir `tests/PetCare.Tests/Dominio/RelojHogarTests.cs`: `Hoy` y la hora del hogar se calculan con offset fijo −03:00 sin horario de verano (FR-044); 03:05 UTC es 00:05 del nuevo día (AC-28), 02:59 UTC es 23:59 del día anterior (AC-28b); conversión ida y vuelta `Dia`+`Hora` ↔ instante UTC
- [X] T009 Crear la infraestructura de tests en `tests/PetCare.Tests/Infra/BaseDeDatosDePrueba.cs` (conexión SQLite `DataSource=:memory:` abierta durante el test y esquema creado aplicando las migraciones de `PetCareDbContext`, R11); el esquema necesita la migración de T018. `FakeEmailSender` se crea en T098, junto con `IEmailSender` (US8)

### Implementación

- [X] T010 Implementar `src/PetCare.Web/Dominio/RelojHogar.cs` sobre el `TimeProvider` inyectado: `AhoraUtc`, `Hoy` (`DateOnly`), `HoraActual` (`TimeOnly`), `AUtc(DateOnly, TimeOnly)` y `ADiaHora(DateTime utc)`, offset fijo −03:00 (R2) — hace pasar T008
- [X] T011 [P] Crear los enums en `src/PetCare.Web/Dominio/Enums.cs`: `TipoDeber` (Comida, Paseo, Medicacion, Bano, VisitaEstudio), `SubtipoVisita` (Veterinario, Estudio, Vacuna, IntervencionMedica), `Modalidad` (HoraFija, CadaNHoras), `Recurrencia` (SoloHoy, TodosLosDias), `EstadoInstancia` (Vigente, Cumplida, DadaDeBaja), `TipoNotificacion` (Vencimiento, Recordatorio, AvisoTurnoBano), `EstadoNotificacion` (Pendiente, Aceptada, Perdida)
- [X] T012 [P] Crear `src/PetCare.Web/Dominio/Entidades/Hogar.cs` y `Cuidador.cs` según data-model: Hogar `Id` "siempre `1`: existe un único hogar", `Codigo` string(8) "se guarda en mayúsculas", `CreadoUtc`; Cuidador `Email`, `EmailNormalizado` "`ToUpperInvariant`; **único**", `PasswordHash`, `CreadoUtc`, FK `HogarId`
- [X] T013 [P] Crear `src/PetCare.Web/Dominio/Entidades/Mascota.cs`: `Nombre` "Obligatorio", `NombreNormalizado` "**Único** por hogar", `CreadaDia` (`DateOnly`), `MaxDiasEntreBanos` (`int?`) "Opcional; una vez definido se puede modificar pero no quitar"
- [X] T014 [P] Crear `src/PetCare.Web/Dominio/Entidades/Deber.cs` con todos los campos de data-model (`Tipo`, `Subtipo?` "Solo VisitaEstudio, obligatorio", `Modalidad?`, `HoraInicio?`, `IntervaloHoras?` "entero 1–23", `Recurrencia?`, `HastaDia?` "Solo medicación diaria; null = para siempre; inclusive", `Dia?`/`Hora?` "Solo puntuales", `CreadoUtc`, `CreadoDia`, `DadoDeBajaUtc?` "nunca vuelve a null"); sin propiedades calculadas (se agregan con sus tests en T042 y T074)
- [X] T015 [P] Crear `src/PetCare.Web/Dominio/Entidades/Instancia.cs` (`Dia`, `Hora`, `ProgramadaUtc`, `Estado`, `GeneradaUtc`, `GeneradaVencida`, `EpisodioVencimiento` "Empieza en 0", `ReactivadaUtc?`) y `RegistroCumplimiento.cs` (`InstanciaId` "**único**", `Dia` "Siempre el día en que se registra", `Hora?` "null en baño y visita/estudio", `RegistradoUtc`)
- [X] T016 [P] Crear `src/PetCare.Web/Dominio/Entidades/Notificacion.cs` (`Tipo`, `Clave` "**Única**", `InstanciaId?`, `DeberId?`, `MascotaId?`, `CondicionUtc`, `Estado`, `Intentos`, `UltimoIntentoUtc?`, `AceptadaUtc?`)
- [X] T017 Crear `src/PetCare.Web/Datos/PetCareDbContext.cs` con `DbSet` de las 7 entidades y la configuración de "Índices y restricciones" de data-model: únicos en `Cuidador.EmailNormalizado`, `Mascota (HogarId, NombreNormalizado)`, `Instancia (DeberId, ProgramadaUtc)`, `RegistroCumplimiento.InstanciaId`, `Notificacion.Clave`; índices en `Instancia (Estado, Dia)`, `RegistroCumplimiento.Dia`, `Notificacion.Estado`; check `IntervaloHoras BETWEEN 1 AND 23`; enums guardados como texto
- [X] T018 Generar la migración inicial con `dotnet ef migrations add Inicial --project src/PetCare.Web` en `src/PetCare.Web/Datos/Migrations/` y verificar que T009 crea el esquema con ella
- [X] T019 Configurar `src/PetCare.Web/Program.cs` (solo registro de servicios, sin lógica propia): `TimeProvider.System` y `RelojHogar` en DI; `PetCareDbContext` con SQLite; autenticación por cookie con `ExpireTimeSpan` 30 días, `SlidingExpiration = false` y `TimeProvider` inyectado (R9); política de autorización por defecto que exige usuario autenticado, con `/Cuenta/Registro` y `/Cuenta/Login` anónimos; exponer `public partial class Program` para los tests. Los eventos de la cookie se implementan en US1 después de sus tests
- [X] T020 Crear `tests/PetCare.Tests/Infra/PetCareWebFactory.cs` (`WebApplicationFactory<Program>` que reemplaza `TimeProvider` por un `FakeTimeProvider` y la base por la de T009; el reemplazo de `IEmailSender` lo agrega T098) y un helper para registrar e iniciar sesión con cookies
- [X] T021 [P] Crear `src/PetCare.Web/Pages/Shared/_Layout.cshtml` y `src/PetCare.Web/wwwroot/css/site.css`: viewport móvil, layout de una columna sin scroll horizontal desde 360 px (FR-042), script de HTMX local y `hx-headers` con el token antiforgery en el `<body>` (R12)

**Checkpoint**: base con migraciones, reloj testeado, autenticación configurada. Pueden empezar
las historias.

---

## Phase 3: User Story 1 - Sumarse al hogar compartido (Priority: P1) 🎯 MVP

**Goal**: registrarse (creando el hogar o con su código), iniciar y cerrar sesión, consultar el
código; nadie sin sesión ve datos.

**Independent Test**: registrar a la primera persona y ver el código; registrar a una segunda
con ese código; iniciar y cerrar sesión; sin sesión, toda página protegida redirige al login.

### Tests for User Story 1 ⚠️ (escribirlos primero y verlos fallar)

- [X] T022 [P] [US1] Escribir `tests/PetCare.Tests/Dominio/CodigoHogarTests.cs`: el código tiene exactamente 8 caracteres del alfabeto `ABCDEFGHJKMNPQRSTUVWXYZ23456789`, sin 0, O, 1, I ni L (AC-01c); la comparación ignora mayúsculas (AC-02h)
- [X] T023 [P] [US1] Escribir `tests/PetCare.Tests/Servicios/RegistroServiceTests.cs` contra la base de prueba: AC-01 (primera persona crea hogar y recibe código), AC-02b y AC-02c (código incorrecto o ausente → rechazo, nadie registrado), AC-02d y AC-02e (email repetido sin distinguir mayúsculas → rechazo), AC-02f y AC-02g (7 caracteres rechaza con "La contraseña debe tener al menos 8 caracteres."; 8 acepta), AC-02h (código en minúsculas), AC-02i (dos registros simultáneos sin código: un único hogar, uno registrado, el otro rechazado informando "El hogar ya existe: para sumarte necesitás su código."), AC-02j y AC-63d (email existente + código incorrecto → solo "El código de hogar es incorrecto.")
- [X] T024 [P] [US1] Escribir `tests/PetCare.Tests/Web/CuentaTests.cs` con `PetCareWebFactory`: AC-01b (segundo cuidador ve el mismo código en `/Hogar/Codigo`), AC-03 y AC-03b (login con email en otras mayúsculas lleva a `/`), AC-04 y AC-04f (contraseña incorrecta y email inexistente reciben el mismo mensaje "El email o la contraseña son incorrectos."), AC-04b (tras `POST /Cuenta/Logout`, `/` redirige al login), AC-05j (`/Hogar/Codigo` sin sesión no devuelve el código), AC-05k (`/` sin sesión redirige a `/Cuenta/Login?motivo=sin-sesion`, que muestra "Iniciá sesión para continuar." sin datos del hogar), y una solicitud HTMX sin sesión recibe `401` sin cuerpo con `HX-Redirect: /Cuenta/Login?motivo=sin-sesion`
- [X] T025 [P] [US1] Escribir `tests/PetCare.Tests/Web/SesionTests.cs` avanzando el `FakeTimeProvider`: AC-04c (última solicitud el día 1 10:00 → el día 31 10:01 `/` redirige al login sin datos), AC-04d (solicitud el día 20 → el día 45 sigue con sesión), y la cookie emitida al iniciar sesión es persistente, con expiración 30 días después (FR-005, R9)

### Implementation for User Story 1

- [X] T026 [P] [US1] Implementar `src/PetCare.Web/Dominio/CodigoHogar.cs`: `Generar()` con `RandomNumberGenerator.GetItems` sobre el alfabeto de R10 (8 caracteres) y `Coincide(ingresado, guardado)` sin distinguir mayúsculas (FR-046) — hace pasar T022
- [X] T027 [US1] Implementar `src/PetCare.Web/Servicios/RegistroService.cs`: si no existe el hogar, crearlo con `Id = 1` y el código (FR-001); si la inserción choca por clave primaria, reevaluar como hogar existente (FR-048); con hogar existente, validar **primero** el código (FR-049, FR-002), después email normalizado único (FR-003) y contraseña ≥ 8 (FR-047); hash con `PasswordHasher<Cuidador>`; devolver el motivo de rechazo con los mensajes del contrato (FR-051) — hace pasar T023
- [X] T028 [US1] Crear `src/PetCare.Web/Pages/Cuenta/Registro.cshtml(.cs)`: formulario email, contraseña y código; al registrarse inicia sesión con `IsPersistent = true` (R9) y muestra el código si creó el hogar (FR-001); muestra el motivo de rechazo
- [X] T029 [P] [US1] Crear `src/PetCare.Web/Pages/Cuenta/Login.cshtml(.cs)` (verificación con `PasswordHasher`, email normalizado; `SignInAsync` con `IsPersistent = true` (R9); con `motivo=sin-sesion` muestra "Iniciá sesión para continuar." (FR-051); ante cualquier fallo, "El email o la contraseña son incorrectos.", FR-050) y `src/PetCare.Web/Pages/Cuenta/Logout.cshtml(.cs)` (`POST` que cierra la sesión, FR-005)
- [X] T030 [P] [US1] Crear `src/PetCare.Web/Pages/Hogar/Codigo.cshtml(.cs)` que muestra el código de hogar al cuidador con sesión (FR-004)
- [X] T031 [US1] Crear `src/PetCare.Web/Pages/Index.cshtml(.cs)` mínima (requiere sesión, enlaces a código de hogar y cerrar sesión) como destino del login; US2 la completa
- [X] T032 [US1] Implementar `src/PetCare.Web/Infra/EventosCookie.cs` y registrarlo en `Program.cs`: `OnValidatePrincipal` fija `ShouldRenew = true` (R9); `OnRedirectToLogin` redirige a `/Cuenta/Login?motivo=sin-sesion` y, si la solicitud es HTMX (`HX-Request: true`), responde `401` sin cuerpo con `HX-Redirect: /Cuenta/Login?motivo=sin-sesion` (contracts/ui-routes.md, FR-006, FR-051) — hace pasar T024 y T025

**Checkpoint**: US1 funciona sola; MVP de acceso al hogar.

---

## Phase 4: User Story 2 - Cargar mascotas y deberes programables y ver el estado del día (Priority: P1)

**Goal**: registrar mascotas, dar de alta comida, paseo y medicación, y ver sus instancias en el
estado del día.

**Independent Test**: registrar una mascota con "paseo cada 4 horas desde las 08:00, todos los
días" y ver 08:00, 12:00, 16:00 y 20:00 hoy y mañana.

### Tests for User Story 2 ⚠️

- [X] T033 [P] [US2] Escribir `tests/PetCare.Tests/Dominio/GeneradorInstanciasTests.cs`: serie cada N horas dentro del día (RF-07: cada 4 h desde 08:00 → 08:00, 12:00, 16:00, 20:00; AC-10); hora fija una instancia; `SoloHoy` solo el día del alta (AC-12); `TodosLosDias` genera el día siguiente (AC-11); `HastaDia` inclusive (AC-13, AC-13b); para siempre (AC-15)
- [X] T034 [P] [US2] Escribir `tests/PetCare.Tests/Servicios/MascotasServiceTests.cs`: AC-06 (registro), AC-06b, AC-06c y AC-63 (nombre repetido sin distinguir mayúsculas → "Ya existe una mascota con ese nombre.")
- [X] T035 [P] [US2] Escribir `tests/PetCare.Tests/Servicios/DeberesProgramablesTests.cs`: AC-10b, AC-10c y AC-63b (N = 0 y N = 24 → "N debe ser un número entero entre 1 y 23."), AC-13d (fecha de fin anterior a hoy → "La fecha no puede ser anterior a hoy."), AC-14b (alta a las 13:00 genera también 08:00 y 12:00), AC-09 (el deber de la mascota A no aparece en B), AC-19b parte paseo, AC-19c (comida "solo para hoy" de ayer no cumplida no está activa), AC-19f (alta desde la configuración)
- [X] T036 [P] [US2] Escribir `tests/PetCare.Tests/Servicios/EstadoDelDiaQueryTests.cs`: sección por mascota con sus instancias vigentes del día ordenadas por hora (FR-033), AC-65 (sin mascotas → estado vacío con opción de registrar), AC-02 (segundo cuidador ve la sección de la mascota A)
- [X] T037 [P] [US2] Escribir `tests/PetCare.Tests/Programado/GeneracionDiariaTests.cs`: un ciclo de `ProcesadorProgramado` después de las 00:00 UTC−3 crea las instancias del día de los deberes diarios, y ejecutarlo dos veces no duplica (FR-017, R4)
- [X] T038 [P] [US2] Escribir `tests/PetCare.Tests/Web/AccesoMascotasTests.cs`: AC-05 (`/` sin sesión), AC-05c (configuración de deberes), AC-05d (registrar mascota) y AC-05e (alta de deber) se rechazan sin datos ni cambios
- [X] T099 [US2] Agregar a `tests/PetCare.Tests/Servicios/MascotasServiceTests.cs` (cambio de PRD del 2026-10-11, RF-04c y RF-04d): AC-06d y AC-63e (nombre vacío → "El nombre de la mascota no puede estar vacío."), AC-06e (tres espacios → el mismo rechazo), AC-06f ("  Firulais  " se guarda como "Firulais") y AC-06g ("Firulais " se rechaza con Firulais ya registrada); en ningún rechazo se crea la mascota

### Implementation for User Story 2

- [X] T039 [P] [US2] Implementar `src/PetCare.Web/Dominio/GeneradorInstancias.cs`: función pura que, dado un `Deber` y un `DateOnly`, devuelve los `(Dia, Hora)` a generar según modalidad, recurrencia, `HastaDia` y `CreadoDia` (FR-014) — hace pasar T033
- [X] T040 [P] [US2] Implementar `src/PetCare.Web/Dominio/ReglasDeber.cs` con `EsActivo(deber, instancias, hoy)`: "no dado de baja **y** (tiene una instancia vigente no expirada **o** es diario y todavía puede generar instancias: `HastaDia` null o `HastaDia > hoy`)" (data-model)
- [X] T041 [US2] Implementar `src/PetCare.Web/Servicios/MascotasService.cs`: alta con `NombreNormalizado` único por hogar y `CreadaDia = Hoy` (FR-007, FR-051) — hace pasar T034
- [X] T100 [US2] Agregar a `src/PetCare.Web/Servicios/MascotasService.cs` el recorte de espacios al principio y al final del nombre antes de guardarlo y de calcular `NombreNormalizado` (RF-04d) y el rechazo del nombre vacío o con solo espacios con su mensaje (RF-04c, FR-051) — hace pasar T099
- [X] T042 [US2] Agregar a `src/PetCare.Web/Dominio/Entidades/Deber.cs` la propiedad calculada `EsProgramable` (comida, paseo o medicación) e implementar `src/PetCare.Web/Servicios/DeberesService.cs` (alta programable): validar `IntervaloHoras` 1–23 (FR-009) y `HastaDia ≥ hoy` solo para medicación diaria (FR-010); guardar el deber y generar en el momento las instancias de hoy, incluidas las de horarios pasados, marcando `GeneradaVencida` si ya estaban vencidas (FR-018, R4); listado de deberes activos con `ReglasDeber` (FR-012) — hace pasar T035
- [X] T043 [US2] Implementar `src/PetCare.Web/Servicios/EstadoDelDiaQuery.cs`: modelo de vista por mascota con las instancias vigentes de hoy (FR-033) y el estado vacío del hogar (FR-053) — hace pasar T036
- [X] T044 [US2] Implementar `src/PetCare.Web/Programado/ProcesadorProgramado.cs` (`EjecutarCicloAsync`, paso 1: generar de forma idempotente las instancias de hoy de los deberes diarios activos, R4/R5) y `src/PetCare.Web/Programado/ProgramadorHostedService.cs` (`PeriodicTimer` de 30 s desde el `TimeProvider`); registrarlos en `Program.cs` — hace pasar T037
- [X] T045 [P] [US2] Crear `src/PetCare.Web/Pages/Mascotas/Nueva.cshtml(.cs)`: formulario de nombre con el motivo de rechazo (FR-007)
- [X] T046 [US2] Crear `src/PetCare.Web/Pages/Mascotas/Deberes.cshtml(.cs)` (ruta `/Mascotas/{id}/Deberes`): deberes activos con sus datos de alta (FR-012) y handler `AltaProgramable` (tipo, modalidad, hora, N, recurrencia, hasta) según contracts/ui-routes.md
- [X] T047 [US2] Completar `src/PetCare.Web/Pages/Index.cshtml(.cs)` y el parcial `src/PetCare.Web/Pages/Shared/_EstadoDelDia.cshtml`: una sección por mascota con sus instancias y enlace a su configuración; sin mascotas, mensaje y enlace a registrar una (FR-053) — hace pasar T038
- [X] T101 [US2] Agregar a `tests/PetCare.Tests/Web/CuentaTests.cs` el test web de AC-02: la primera persona registra la mascota A, una segunda persona se registra con el código de hogar y, al iniciar sesión, ve en `/` la sección de A (en T036 solo se verificó a nivel de la consulta); es un test de regresión: pasa desde que se escribe porque T047 ya está implementada

**Checkpoint**: US1 + US2: mascotas y deberes programables visibles en el estado del día.

---

## Phase 5: User Story 3 - Registrar y corregir cumplimientos (Priority: P1)

**Goal**: cumplir instancias con hora real, eliminar registros de hoy y ver el resumen de paseos
y últimas comidas y paseos.

**Independent Test**: cumplir un paseo, ver que desaparece y que "paseos de hoy" sube; eliminar
el registro y ver que la instancia vuelve.

### Tests for User Story 3 ⚠️

- [X] T048 [P] [US3] Escribir `tests/PetCare.Tests/Servicios/CumplimientosServiceTests.cs`: AC-32 (cumplir paseo), AC-32b (dos cumplimientos simultáneos de la misma instancia: uno registrado, el otro "Esta tarea ya fue cumplida."), AC-35 y AC-35b (hora propuesta o editada), AC-35c y AC-63c (hora futura → "La hora no puede ser posterior a la actual."), AC-35d (medicación de ayer cumplida hoy a las 08:00 con hora 22:00 → rechazo), AC-37 (eliminar uno de dos paseos de hoy), AC-37b (registro con fecha de ayer → "Solo se pueden eliminar registros de hoy."), AC-37c (registro de hoy sobre medicación de ayer → se elimina y la instancia vuelve a `Vigente`), que eliminar incrementa `EpisodioVencimiento` y fija `ReactivadaUtc`, y `FR_031_cumplir_instancia_inexistente_se_rechaza` (un `InstanciaId` inexistente se rechaza sin crear registro, informando "Esa tarea no existe.")
- [X] T049 [P] [US3] Escribir `tests/PetCare.Tests/Servicios/ResumenMascotaTests.cs`: AC-39c (última comida y último paseo de ayer), AC-39d (mascota sin cumplimientos), AC-65b (mascota sin instancias vigentes → sección con resumen y "no hay tareas pendientes"), AC-25 parte resumen (el paseo cumplido sigue contado)
- [X] T050 [P] [US3] Escribir `tests/PetCare.Tests/Web/CumplimientoWebTests.cs`: AC-60 (con la hora por defecto, `GET ?handler=FormCumplir` + `POST ?handler=Cumplir` bastan: 2 toques ≤ 5), AC-05b (cumplir sin sesión → 401 y sin registro) y AC-05h (eliminar sin sesión → 401 y el registro se conserva)

### Implementation for User Story 3

- [X] T051 [US3] Implementar `src/PetCare.Web/Servicios/CumplimientosService.cs`: `Cumplir` (FR-024; rechaza instancias inexistentes, FR-031) con `UPDATE ... WHERE Estado = Vigente` por `ExecuteUpdateAsync` en transacción e inserción del registro solo si afectó 1 fila; si afectó 0, releer y devolver "ya fue cumplida" o "ya fue dada de baja" (FR-028, FR-029, FR-045, R8); `Dia` = hoy (FR-052); hora real solo para comida, paseo y medicación (baño y visita/estudio guardan solo la fecha, FR-027), por defecto la actual y no posterior a ella (FR-025, FR-026); `Eliminar` solo si `Dia == hoy` (FR-030, FR-032), devolviendo la instancia a `Vigente` con `EpisodioVencimiento + 1` y `ReactivadaUtc = ahora` (data-model) — hace pasar T048
- [X] T052 [US3] Ampliar `src/PetCare.Web/Servicios/EstadoDelDiaQuery.cs` con el resumen por mascota: paseos de hoy, fecha y hora de la última comida y del último paseo o "ninguno" (FR-034), registros de hoy eliminables, y mensaje "no hay tareas pendientes" para mascotas sin instancias vigentes (FR-053) — hace pasar T049
- [X] T053 [US3] Agregar a `src/PetCare.Web/Pages/Index.cshtml.cs` los handlers HTMX `FormCumplir` (parcial `src/PetCare.Web/Pages/Shared/_FormCumplir.cshtml` con la hora propuesta, sin hora para baño y visita/estudio), `Cumplir` y `EliminarRegistro`, que devuelven la sección de la mascota con el motivo de rechazo si lo hay (contracts/ui-routes.md) — hace pasar T050
- [X] T054 [US3] Actualizar `src/PetCare.Web/Pages/Shared/_EstadoDelDia.cshtml`: botón "Hecho" por instancia, resumen de la mascota y lista de registros de hoy con "Eliminar" (FR-034, FR-043)

**Checkpoint**: MVP completo (US1 + US2 + US3): el hogar registra y ve lo hecho.

---

## Phase 6: User Story 4 - Ver vencimientos y arrastre de tareas olvidadas (Priority: P2)

**Goal**: marcas "pendiente", expiración de comida y paseo, arrastre de medicación y generación
al reiniciar.

**Independent Test**: con el reloj controlado, ver "pendiente" a los 15 minutos, la expiración
al cambiar de día y la medicación arrastrada.

### Tests for User Story 4 ⚠️

- [ ] T055 [P] [US4] Escribir `tests/PetCare.Tests/Dominio/ReglasVencimientoTests.cs`: programables vencen en `ahora ≥ programada + 15 min` (AC-29 a las 09:10 no, AC-29b a las 09:15 sí, AC-44), comida y paseo expiran con `Dia < hoy` (AC-30), medicación no expira, y la evaluación es en hora del hogar (AC-61: 05:15 UTC−3 no está pendiente una comida de las 08:00)
- [ ] T056 [P] [US4] Escribir `tests/PetCare.Tests/Servicios/VencimientosEstadoTests.cs`: AC-31 y AC-31b (medicación de ayer "pendiente" junto a la de hoy), AC-13c y AC-19e (medicación terminada el día 10 con dosis sin cumplir sigue pendiente y el deber activo el día 11), AC-14 (alta a las 10:30 de un paseo de las 10:00 → "pendiente"), AC-28 y AC-28b (instancias del nuevo día recién después de 00:00 UTC−3), AC-38 (al eliminar el registro erróneo, la instancia vuelve "pendiente")
- [ ] T057 [P] [US4] Escribir `tests/PetCare.Tests/Programado/ReinicioTests.cs`: AC-28c (sin ciclos entre las 23:00 y las 10:00, el primer ciclo a las 10:00 genera la instancia de las 08:00 con `GeneradaVencida = true` y marca "pendiente")

### Implementation for User Story 4

- [ ] T058 [US4] Implementar `src/PetCare.Web/Dominio/ReglasVencimiento.cs`: `EstaVencida(instancia, tipo, ahora)` y `EstaExpirada(instancia, tipo, hoy)` según la tabla "Marcas calculadas al leer" de data-model (R3) — hace pasar T055
- [ ] T059 [US4] Aplicar `ReglasVencimiento` en `src/PetCare.Web/Servicios/EstadoDelDiaQuery.cs`: ocultar comida y paseo expirados (FR-021), incluir medicación vigente de días anteriores (FR-022) y marcar "pendiente" (FR-019); en `ProcesadorProgramado` y `DeberesService`, fijar `GeneradaVencida` con la misma regla (FR-020) — hace pasar T056 y T057
- [ ] T060 [US4] Mostrar la marca "pendiente" en `src/PetCare.Web/Pages/Shared/_EstadoDelDia.cshtml`

**Checkpoint**: los olvidos se ven en pantalla sin emails.

---

## Phase 7: User Story 5 - Agendar baños y visitas/estudios (Priority: P2)

**Goal**: deberes puntuales con subtipo, marca "no cumplido" y máximo de días entre baños.

**Independent Test**: agendar un baño para dentro de unos días; no aparece hasta ese día;
aparece sin marca antes de la hora y "no cumplido" después.

### Tests for User Story 5 ⚠️

- [ ] T061 [P] [US5] Escribir `tests/PetCare.Tests/Servicios/DeberesPuntualesTests.cs`: AC-17b y AC-17c (fecha anterior a hoy → "La fecha no puede ser anterior a hoy."), AC-17d (hoy con hora pasada se acepta y queda "no cumplido"), AC-18 (subtipos válidos), AC-18b (visita/estudio sin subtipo → "Elegí el subtipo: veterinario, estudio, vacuna o intervención médica."), AC-19b parte baño, AC-19d (baño cumplido no está activo), AC-16 (vacuna cumplida no aparece mañana)
- [ ] T062 [P] [US5] Escribir `tests/PetCare.Tests/Servicios/MaxBanosTests.cs`: AC-08 (definir 20 sin deberes) y AC-07 (modificar a 30)
- [ ] T063 [P] [US5] Escribir `tests/PetCare.Tests/Servicios/PuntualesEstadoTests.cs`: AC-17 (baño del viernes aparece el viernes sin marca antes de las 15:00), AC-39b (turno en 3 días no aparece), AC-40 (subtipo visible), AC-45 y AC-46 ("no cumplido" de hace 3 días; al cumplirlo desaparece), AC-33 y AC-33b (paseo y baño del día juntos; cumplir el baño deja el paseo), AC-36 (cumplir un baño no pide hora), AC-39 (escenario combinado del estado del día)
- [ ] T064 [P] [US5] Escribir en `tests/PetCare.Tests/Web/AccesoMascotasTests.cs` el caso AC-05i (cambiar el máximo sin sesión → rechazo y sigue en 20)

### Implementation for User Story 5

- [ ] T065 [US5] Agregar a `src/PetCare.Web/Servicios/DeberesService.cs` el alta puntual: `Dia ≥ hoy` (FR-011), subtipo obligatorio solo en visita/estudio (FR-011, RF-11d), una instancia con `GeneradaVencida` si la hora ya pasó (AC-17d) — hace pasar T061
- [ ] T066 [US5] Agregar a `src/PetCare.Web/Servicios/MascotasService.cs` `DefinirMaxDiasEntreBanos` (definir o modificar, nunca quitar; FR-008) — hace pasar T062
- [ ] T067 [US5] Extender `src/PetCare.Web/Dominio/ReglasVencimiento.cs` y `EstadoDelDiaQuery.cs` para puntuales: "no cumplido" cuando `ahora > programada` (FR-023), aparecen desde su día y siguen en días posteriores mientras estén vigentes (FR-033), con subtipo — hace pasar T063
- [ ] T068 [US5] Agregar a `src/PetCare.Web/Pages/Mascotas/Deberes.cshtml(.cs)` los handlers `AltaPuntual` (tipo, subtipo, fecha, hora) y `MaxBanos`, y mostrar el máximo de días (FR-008, FR-012) — hace pasar T064
- [ ] T069 [US5] Mostrar subtipo y marca "no cumplido" en `src/PetCare.Web/Pages/Shared/_EstadoDelDia.cshtml`

**Checkpoint**: seguimiento de cuidados de salud e higiene.

---

## Phase 8: User Story 6 - Dar de baja deberes e instancias (Priority: P2)

**Goal**: bajas definitivas de deberes e instancias, con resolución de conflictos entre
cuidadores.

**Independent Test**: dar de baja la instancia de las 12:00 de un paseo recurrente (el deber
sigue y mañana vuelven las cuatro); dar de baja un deber y ver que desaparece.

### Tests for User Story 6 ⚠️

- [ ] T070 [P] [US6] Escribir `tests/PetCare.Tests/Servicios/BajasTests.cs`: AC-20d y AC-24 (baja de deber: desaparece de la configuración y no hay instancias mañana), AC-20b parte pantalla (medicación con instancias de ayer y hoy sin cumplir: ninguna aparece), AC-25 (los registros se conservan), AC-20 y AC-20c (baja de la instancia de un deber de instancia única da de baja el deber), AC-23 y AC-23b (baja de la instancia de las 12:00 conserva el deber y las demás)
- [ ] T071 [P] [US6] Escribir `tests/PetCare.Tests/Servicios/ConflictosTests.cs`: AC-32c (baja y después cumplimiento → "Esta tarea ya fue dada de baja."), AC-32d (cumplimiento y después baja → "Esta tarea ya fue cumplida."), AC-32e (dos bajas), AC-32f (baja del deber y después cumplimiento de una instancia), con la cantidad de paseos esperada en cada caso
- [ ] T072 [P] [US6] Escribir en `tests/PetCare.Tests/Web/AccesoMascotasTests.cs` los casos AC-05f (baja de deber sin sesión) y AC-05g (baja de instancia sin sesión)

### Implementation for User Story 6

- [ ] T073 [US6] Agregar a `src/PetCare.Web/Servicios/DeberesService.cs` `DarDeBaja(deberId)`: fija `DadoDeBajaUtc` y pasa a `DadaDeBaja` las instancias `Vigente` en una actualización condicional; conserva registros (FR-013)
- [ ] T074 [US6] Agregar a `src/PetCare.Web/Dominio/Entidades/Deber.cs` la propiedad calculada `EsDeInstanciaUnica` ("puntual, o programable `HoraFija` + `SoloHoy`") y crear `src/PetCare.Web/Servicios/BajasInstanciaService.cs` con `DarDeBaja(instanciaId)`: `UPDATE ... WHERE Estado = Vigente`; si afectó 0, devolver "ya fue cumplida" o "ya fue dada de baja" (FR-045); si el deber es de instancia única, darlo de baja (FR-016); si no, conservar el deber (FR-015) — hace pasar T070 y T071
- [ ] T075 [US6] Agregar el handler `Baja` en `src/PetCare.Web/Pages/Mascotas/Deberes.cshtml.cs` y `BajaInstancia` en `src/PetCare.Web/Pages/Index.cshtml.cs`, con botones en sus vistas — hace pasar T072

**Checkpoint**: planes de cuidado corregibles sin alarmas falsas.

---

## Phase 9: User Story 7 - Ver cambios de otros cuidadores sin recargar (Priority: P2)

**Goal**: el estado del día se actualiza solo en ≤ 15 s, sin renovar la sesión.

**Independent Test**: con dos sesiones abiertas, un cumplimiento en una se ve en la otra en
≤ 15 s sin recargar.

### Tests for User Story 7 ⚠️

- [ ] T076 [P] [US7] Escribir `tests/PetCare.Tests/Web/RefrescoTests.cs`: el parcial de `GET /?handler=Refresco` refleja el cumplimiento o la baja de otro cuidador (AC-47, AC-47d) y el paso a "pendiente" o "no cumplido" al avanzar el reloj (AC-47b, AC-47c); la página incluye `hx-trigger="every 10s"` (RNF-04); sin sesión responde 401 con `HX-Redirect` y sin datos (AC-05)
- [ ] T077 [P] [US7] Agregar a `tests/PetCare.Tests/Web/SesionTests.cs` el caso AC-04e (con solo refrescos automáticos desde el día 1 10:00, el refresco del día 31 10:01 se rechaza sin datos)

### Implementation for User Story 7

- [ ] T078 [US7] Agregar a `src/PetCare.Web/Pages/Index.cshtml(.cs)` el handler `Refresco` que devuelve `_EstadoDelDia` y el contenedor con `hx-get` y `hx-trigger="every 10s"` (R6, FR-035) — hace pasar T076
- [ ] T079 [US7] En `src/PetCare.Web/Program.cs`, no renovar la sesión (`ShouldRenew = false`) cuando la solicitud es el handler `Refresco` (FR-005, R9) — hace pasar T077

**Checkpoint**: el estado es confiable mientras otro cuidador está por actuar.

---

## Phase 10: User Story 8 - Recibir alarmas y recordatorios por email (Priority: P3)

**Goal**: emails de vencimiento y recordatorio, sin duplicados, con reintentos y sin depender de
usuarios conectados.

**Independent Test**: con reloj controlado y `FakeEmailSender`, cada email se envía una vez en
su momento y no en los casos excluidos.

### Tests for User Story 8 ⚠️

- [ ] T098 [US8] Crear `tests/PetCare.Tests/Infra/FakeEmailSender.cs` (implementa `IEmailSender`; registra cada envío con destinatarios, asunto y cuerpo; permite programar rechazos o demoras por intento) y agregar a `tests/PetCare.Tests/Infra/PetCareWebFactory.cs` el reemplazo de `IEmailSender` por `FakeEmailSender`; va antes de T081 a T084, que lo usan, y no compila hasta que T085 cree `IEmailSender`
- [ ] T080 [P] [US8] Escribir `tests/PetCare.Tests/Dominio/PlantillasEmailTests.cs` según contracts/emails.md: AC-64 (vencimiento: mascota, tipo, fecha y hora), AC-64b (recordatorio: mascota, tipo, fecha y hora del turno), subtipo en vencimiento (AC-43) y recordatorio (AC-42)
- [ ] T081 [P] [US8] Escribir `tests/PetCare.Tests/Programado/EmailsVencimientoTests.cs` ejecutando ciclos con `FakeTimeProvider`: AC-48 (a todos los cuidadores ≤ 09:20), AC-49 (cumplida a las 09:05, sin email), AC-43 y AC-50 (visita/estudio ≤ 10:05), AC-14, AC-17d y AC-28c (generada vencida, sin email), AC-38b (registro erróneo eliminado a las 10:00 → email ≤ 10:05), AC-38c (con email ya aceptado no hay segundo), AC-20b parte email (baja del deber, sin emails en el día)
- [ ] T082 [P] [US8] Escribir `tests/PetCare.Tests/Programado/EmailsRecordatorioTests.cs`: AC-51 (≤ 15:05 del miércoles), AC-51b (exactamente uno), AC-51c (dado de baja antes, ninguno), AC-52 (alta con menos de 48 h, ninguno), AC-52b (alta con 48 h exactas, uno), AC-42
- [ ] T083 [P] [US8] Escribir `tests/PetCare.Tests/Programado/ReintentosTests.cs` con `FakeEmailSender` programado: AC-62 (rechazos entre 09:15 y 09:17 → aceptado ≤ 09:20), AC-62b (rechazos hasta 09:30 → sin intentos después de las 09:20 y `Perdida`), AC-62c (tras perderse, eliminar un registro erróneo dispara el email ≤ 10:35), AC-62d (sin respuesta 30 s por intento entre 09:15 y 09:17 → aceptado ≤ 09:20), y condición detectada después de 5 min → `Perdida` sin envío
- [ ] T084 [P] [US8] Escribir `tests/PetCare.Tests/Web/ConfiguracionEmailTests.cs`: en `Development` se registra `ConsoleEmailSender`; fuera de `Development`, sin las variables `Smtp__*` la app no arranca e informa la falta (R7, Principio II)

### Implementation for User Story 8

- [ ] T085 [P] [US8] Crear `src/PetCare.Web/Email/IEmailSender.cs`, `src/PetCare.Web/Email/ConsoleEmailSender.cs` (escribe en la consola, nunca envía) y `src/PetCare.Web/Email/SmtpOptions.cs` (`Host`, `Port`, `User`, `Password`, `From`, validadas al arrancar)
- [ ] T086 [P] [US8] Crear `src/PetCare.Web/Email/SmtpEmailSender.cs` con MailKit y `SmtpClient.Timeout = 30000` (FR-041); lanza excepción si el servicio rechaza o no responde
- [ ] T087 [P] [US8] Implementar `src/PetCare.Web/Dominio/PlantillasEmail.cs` con asuntos y cuerpos de contracts/emails.md (FR-054, FR-039) — hace pasar T080
- [ ] T088 [US8] Implementar `src/PetCare.Web/Servicios/NotificacionesService.cs`: crear notificaciones de vencimiento con clave `V:{instanciaId}:{episodio}` según las reglas de episodios de R7 (sin `Aceptada` previa, no episodio 0 de `GeneradaVencida` (FR-037), condición `max(vencimiento, ReactivadaUtc)`) y de recordatorio con clave `R:{deberId}` (alta con ≥ 48 h, deber sin baja, FR-038); nacen `Perdida` si se detectan después de `CondicionUtc + 5 min`
- [ ] T089 [US8] Agregar a `src/PetCare.Web/Servicios/NotificacionesService.cs` el envío: para cada `Pendiente` dentro de la ventana, enviar a todos los cuidadores con `PlantillasEmail`; aceptar → `Aceptada`; error → `Intentos + 1`; fuera de la ventana → `Perdida` (FR-036, FR-041)
- [ ] T090 [US8] Sumar los pasos 2 (crear notificaciones) y 3 (enviar) a `ProcesadorProgramado.EjecutarCicloAsync` y registrar en `Program.cs` `ConsoleEmailSender` en `Development` y `SmtpEmailSender` con `SmtpOptions` desde variables de entorno o user-secrets en los demás entornos — hace pasar T081, T082, T083 y T084

**Checkpoint**: las alarmas llegan aunque nadie tenga la app abierta.

---

## Phase 11: User Story 9 - Recibir aviso de turno de baño (Priority: P3)

**Goal**: aviso diario a las 07:00 por mascota que superó su máximo de días sin baño activo.

**Independent Test**: mascota con máximo 20 y último baño hace 25: un aviso en el día a más
tardar a las 07:05; ninguno en los casos excluidos.

### Tests for User Story 9 ⚠️

- [ ] T091 [P] [US9] Escribir `tests/PetCare.Tests/Dominio/AvisoTurnoBanoTests.cs`: regla de las tres condiciones de FR-040 con días calendario en UTC−3: AC-53 (25 días, sí), AC-53d (exactamente 20, no), AC-55 (sin máximo, no), AC-56 (sin baños y alta hace 25, sí), AC-54, AC-54b y AC-54c (baño activo futuro, de hoy o "no cumplido", no)
- [ ] T092 [P] [US9] Escribir `tests/PetCare.Tests/Programado/AvisoTurnoBanoEnvioTests.cs`: AC-53b (≤ 07:05), AC-53 (exactamente uno en el día), AC-58 (se repite al día siguiente), AC-53c (baja del baño futuro a las 10:00 → ese día no, el siguiente sí), AC-64c y AC-64d (contenido con máximo y último baño, o "no hay baños registrados")

### Implementation for User Story 9

- [ ] T093 [US9] Implementar `src/PetCare.Web/Dominio/AvisoTurnoBano.cs` (`Corresponde(mascota, fechaUltimoBano, tieneBanoActivo, hoy)`) y la plantilla del aviso en `src/PetCare.Web/Dominio/PlantillasEmail.cs` — hace pasar T091
- [ ] T094 [US9] Agregar a `src/PetCare.Web/Servicios/NotificacionesService.cs` la creación diaria del aviso a las 07:00 con clave `A:{mascotaId}:{dia}` y `CondicionUtc` = 07:00 de hoy, usando `ReglasDeber.EsActivo` para los baños activos — hace pasar T092

**Checkpoint**: todas las historias completas.

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: verificaciones finales que cruzan historias.

- [ ] T095 [P] Revisar `src/PetCare.Web/wwwroot/css/site.css` y todas las vistas en 360 px de ancho: los 15 flujos de AC-59 sin scroll horizontal (FR-042, SC-004)
- [ ] T096 [P] Verificar las comprobaciones de la constitución de quickstart.md: sin `DateTime.Now`/`UtcNow` ni `EnsureCreated()`/`Migrate()` en `src/`, sin credenciales en `*.json`
- [ ] T097 Correr `dotnet test` completo y la validación manual de `specs/001-petcare-hogar/quickstart.md`; todo en verde antes de commitear a `main`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup; bloquea todas las historias.
- **Historias (Phases 3–11)**: dependen de Foundational; se recomiendan en orden de prioridad.
- **Polish (Phase 12)**: al final.

### User Story Dependencies

- **US1 (P1)**: solo Foundational.
- **US2 (P1)**: Foundational; usa la página `Index` y la sesión de US1.
- **US3 (P1)**: US2 (necesita instancias para cumplir).
- **US4 (P2)**: US2 y US3 (marcas sobre instancias; AC-38 elimina registros).
- **US5 (P2)**: US2 y US3 (alta en la configuración, cumplimiento de baños); comparte
  `ReglasVencimiento` con US4.
- **US6 (P2)**: US2 y US3 (bajas y conflictos con cumplimientos).
- **US7 (P2)**: US2; sus tests de marcas usan US4 y US5.
- **US8 (P3)**: US3, US4, US5 y US6 (vencimientos, episodios, bajas).
- **US9 (P3)**: US5 (máximo de días, baños) y la infraestructura de emails de US8.

### Within Foundational

- T018 necesita que `dotnet ef` pueda crear `PetCareDbContext`, y eso lo registra T019 en
  `Program.cs`; T009 necesita la migración de T018 y es donde T018 verifica el esquema. Orden:
  **T019 → T018 → T009**.

### Within Each User Story

- Tests primero y fallando (Principio I); después dominio, servicios y páginas.
- Una historia se cierra con todos sus tests en verde y `dotnet test` completo en verde.

### Parallel Opportunities

- Setup: T005, T006, T007.
- Foundational: T008; las entidades T011 a T016; T021 (T009 espera la migración de T018).
- US8: T098 primero (crea `FakeEmailSender`); después, T080 a T084 en paralelo.
- En cada historia, todas las tareas de test marcadas [P] entre sí, y las de dominio [P]
  (por ejemplo T039 y T040, o T085, T086 y T087).
- US4, US5 y US6 pueden avanzar en paralelo una vez terminada US3, coordinando los cambios en
  `EstadoDelDiaQuery.cs` y `_EstadoDelDia.cshtml`.

---

## Parallel Example: User Story 2

```bash
# Tests de US2 en paralelo (archivos distintos):
Task: "T033 GeneradorInstanciasTests en tests/PetCare.Tests/Dominio/GeneradorInstanciasTests.cs"
Task: "T034 MascotasServiceTests en tests/PetCare.Tests/Servicios/MascotasServiceTests.cs"
Task: "T035 DeberesProgramablesTests en tests/PetCare.Tests/Servicios/DeberesProgramablesTests.cs"
Task: "T036 EstadoDelDiaQueryTests en tests/PetCare.Tests/Servicios/EstadoDelDiaQueryTests.cs"

# Reglas puras de US2 en paralelo:
Task: "T039 GeneradorInstancias en src/PetCare.Web/Dominio/GeneradorInstancias.cs"
Task: "T040 ReglasDeber en src/PetCare.Web/Dominio/ReglasDeber.cs"
```

---

## Implementation Strategy

### MVP First

1. Phase 1 (Setup) y Phase 2 (Foundational).
2. US1 → validar acceso al hogar de forma independiente.
3. US2 y US3 → **MVP de producto**: el hogar registra y ve lo hecho (los tres P1).
4. **STOP and VALIDATE** con quickstart.md, pasos 1 a 4.

### Incremental Delivery

1. MVP (US1–US3).
2. US4 → olvidos visibles en pantalla.
3. US5 y US6 → cuidados puntuales y bajas.
4. US7 → actualización entre cuidadores.
5. US8 y US9 → alarmas por email.
6. Polish → AC-59 y comprobaciones de la constitución.

Cada incremento termina con `dotnet test` en verde y un commit (flujo de la constitución).
