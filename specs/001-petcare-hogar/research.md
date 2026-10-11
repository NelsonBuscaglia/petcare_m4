# Research: PetCare Hogar

**Fase 0 de** [plan.md](./plan.md) · **Spec**: [spec.md](./spec.md)

El stack está fijado por `AGENTS.md` y el pedido del usuario, así que no hubo incógnitas de
tecnología. Esta investigación resuelve cómo cumplir con ese stack los requerimientos más
delicados: tiempo, alarmas, concurrencia, sesión y actualización automática.

## R1. Estructura de la solución

- **Decision**: una aplicación web (`src/PetCare.Web`) y un proyecto de tests
  (`tests/PetCare.Tests`) en una solución `PetCare.sln`. Dentro de la web, carpetas por
  responsabilidad: `Dominio/` (reglas puras, sin EF ni HTTP), `Datos/`, `Servicios/`, `Email/`,
  `Programado/` y `Pages/`.
- **Rationale**: un único hogar y un único proceso; `AGENTS.md` ya nombra `src/PetCare.Web` y
  el comando `dotnet ef ... --project src/PetCare.Web`. Separar `Dominio/` permite testear
  vencimientos y generación de instancias sin base de datos (Principio I).
- **Alternatives considered**: proyectos separados `Domain`/`Infrastructure`/`Web` (más
  ceremonia sin beneficio para un hogar); un proceso aparte para el scheduler (dos procesos que
  coordinar sobre SQLite).

## R2. Tiempo y zona horaria (FR-044, RNF-06)

- **Decision**: toda hora sale del `TimeProvider` inyectado (`AGENTS.md`). Un servicio
  `RelojHogar` convierte a hora del hogar con un offset fijo de −03:00 (sin `TimeZoneInfo`,
  porque no hay horario de verano) y expone `Ahora` (UTC), `Hoy` (`DateOnly` UTC−3) y
  conversiones día+hora ↔ instante UTC. En tests se usa `FakeTimeProvider`
  (`Microsoft.Extensions.TimeProvider.Testing`).
- **Persistencia**: los instantes se guardan como `DateTime` UTC; los días y horas del hogar
  como `DateOnly`/`TimeOnly`. El proveedor SQLite de EF Core no compara ni ordena
  `DateTimeOffset` en consultas, así que no se usa.
- **Alternatives considered**: `TimeZoneInfo` "America/Argentina/Buenos_Aires" (depende de la
  base tz del sistema y podría introducir cambios de horario que el PRD excluye); guardar todo
  en hora local (ambigüedad al comparar con el reloj del servidor).

## R3. Estados de instancia: guardados vs. calculados (FR-019, FR-021, FR-023, FR-035)

- **Decision**: se guarda solo el estado que nace de una acción: `Vigente`, `Cumplida` o
  `DadaDeBaja`. Las marcas "pendiente", "no cumplido" y "expirada" se **calculan** al leer,
  con la hora actual:
  - comida/paseo/medicación vencida: `ahora ≥ programada + 15 min` (AC-29, AC-29b);
  - baño o visita/estudio vencido: `ahora > programada` (AC-47c);
  - comida/paseo expirada: `Dia < hoy` y sigue `Vigente` (AC-30).
- **Rationale**: el estado mostrado es siempre exacto en el momento de la consulta, sin
  depender de que un proceso haya corrido; cada refresco automático (R6) ya trae las marcas
  nuevas, como exige RF-31.
- **Alternatives considered**: que el scheduler actualice marcas en la base (agrega latencia y
  estados que pueden quedar desfasados si el proceso se atrasa).

## R4. Generación de instancias (FR-014, FR-017, FR-018, FR-020)

- **Decision**: una función pura `GeneradorInstancias` calcula las instancias de un deber para
  un día. Se invoca: (a) al dar de alta un deber, para el día del alta (FR-018); (b) en cada
  ciclo del procesador programado (R5), para el día actual, de forma **idempotente** (índice
  único `DeberId + ProgramadaUtc`). Así, el arranque del servidor completa lo que falte
  (FR-020) sin lógica especial.
- Cada instancia guarda `GeneradaVencida` si al crearse ya estaba vencida (base de FR-037).
- **Alternatives considered**: generar de antemano varios días (complica las bajas y los
  deberes "hasta una fecha").

## R5. Procesador programado (FR-017, FR-036 a FR-041)

- **Decision**: un `BackgroundService` con `PeriodicTimer` de 30 s (creado desde el
  `TimeProvider`) llama a `ProcesadorProgramado.EjecutarCicloAsync()`. Cada ciclo:
  1. genera las instancias del día que falten (R4);
  2. crea notificaciones nuevas cuya condición se cumplió (vencimientos, recordatorios de 48 h,
     avisos de turno de baño de las 07:00), cada una con su `CondicionUtc`;
  3. intenta enviar las notificaciones pendientes (R7).
- **Rationale**: ciclo de 30 s deja margen dentro de la ventana de 5 min (RNF-01, RNF-05). La
  lógica vive en una clase testeable que los tests ejecutan con `FakeTimeProvider`, sin
  esperar tiempo real.
- **Alternatives considered**: Quartz/Hangfire (dependencias no listadas en el stack);
  temporizadores por instancia (frágiles ante reinicios).

## R6. Actualización automática (FR-035, RNF-04)

- **Decision**: el estado del día se renderiza como parcial y HTMX lo pide cada 10 s
  (`hx-get` al handler `Refresco` con `hx-trigger="every 10s"`), reemplazando el contenido.
- **Rationale**: 10 s de intervalo + tiempo de respuesta queda bajo los 15 s de RNF-04, que el
  PRD acepta como polling (Riesgos y Dependencias). No hace falta SignalR ni JavaScript propio.
- **Alternatives considered**: SignalR/WebSockets (fuera del stack; el PRD descarta la
  actualización instantánea); intervalo de 5 s (el doble de carga sin necesidad).

## R7. Notificaciones por email (FR-036 a FR-041, FR-054)

- **Decision**: patrón *outbox*. Cada email es una fila `Notificacion` con una `Clave` única
  que garantiza no duplicar:
  - vencimiento: `V:{instanciaId}:{episodio}`;
  - recordatorio: `R:{deberId}`;
  - aviso de turno de baño: `A:{mascotaId}:{dia}`.
- **Reintentos**: se reintenta en cada ciclo mientras `ahora ≤ CondicionUtc + 5 min`; al
  vencer la ventana pasa a `Perdida` (RF-32c). Un intento cuenta como fallido si MailKit lanza
  error o no responde en 30 s (`SmtpClient.Timeout = 30000`, RF-32e). Solo `Aceptada` cuenta
  como enviado (RF-32d).
- **Emails detectados tarde**: si la condición se detecta cuando ya pasaron 5 min (servidor
  caído), la notificación nace `Perdida` y no se envía (Fuera de Alcance del PRD).
- **Episodios de vencimiento**: cada instancia tiene `EpisodioVencimiento` (empieza en 0) y
  `ReactivadaUtc`. Eliminar un cumplimiento incrementa el episodio y fija `ReactivadaUtc`. Se
  crea una notificación de vencimiento si: la instancia está vencida, no existe ninguna
  notificación `Aceptada` para la instancia (AC-38c), no existe notificación para el episodio
  actual, y no es el episodio 0 de una instancia `GeneradaVencida` (FR-037). La condición es
  `max(vencimiento, ReactivadaUtc)` (AC-38b, AC-62c).
- **Emisores**: `IEmailSender` con dos implementaciones: `ConsoleEmailSender` (desarrollo,
  escribe en la consola, `AGENTS.md`) y `SmtpEmailSender` (MailKit). La configuración SMTP se
  lee de variables de entorno (`Smtp__Host`, `Smtp__Port`, `Smtp__User`, `Smtp__Password`,
  `Smtp__From`) o de user-secrets en desarrollo local (Principio II); si falta en producción,
  la app no arranca y lo informa.
- **Alternatives considered**: enviar en línea dentro del request (depende de que haya usuarios
  conectados, contra RF-32); marcar `EmailEnviado` en la instancia (no distingue intentos
  perdidos de aceptados).

## R8. Concurrencia (FR-028, FR-045, FR-048)

- **Decision**: actualizaciones condicionales en una transacción. Cumplir hace
  `UPDATE Instancias SET Estado = Cumplida WHERE Id = @id AND Estado = Vigente`
  (`ExecuteUpdateAsync`) y solo inserta el registro si afectó 1 fila; si afectó 0, relee el
  estado para informar "ya fue cumplida" o "ya fue dada de baja". La baja usa el mismo patrón.
  Índice único en `RegistroCumplimiento.InstanciaId` como red de seguridad.
- **Registros simultáneos**: el hogar tiene clave fija `Id = 1`; si dos registros intentan
  crearlo, el segundo choca con la clave primaria y se reevalúa como "hogar existente"
  (FR-048).
- **Rationale**: SQLite serializa escrituras; el `WHERE Estado = Vigente` convierte la carrera
  en "gana el primero" sin bloqueos explícitos.
- **Alternatives considered**: tokens de concurrencia (`rowversion` no existe en SQLite; habría
  que simularlo); bloqueos en memoria (no sobreviven a varios procesos).

## R9. Autenticación y sesión (FR-005, FR-006, FR-047, FR-050)

- **Decision**: autenticación por cookie con `ExpireTimeSpan = 30 días` y
  `SlidingExpiration = false`; en `OnValidatePrincipal` se fuerza `ShouldRenew = true` en toda
  solicitud que no sea el refresco automático (identificado por el handler `Refresco`), así el
  plazo se cuenta desde la última solicitud real (RF-02c). La cookie usa el `TimeProvider`
  inyectado (`CookieAuthenticationOptions.TimeProvider`) para poder testear el vencimiento. Al
  iniciar sesión se emite la cookie con `AuthenticationProperties.IsPersistent = true`, para que
  sobreviva al cierre del navegador durante los 30 días (FR-005).
- Contraseñas con `PasswordHasher<Cuidador>`; mínimo 8 caracteres (FR-047). Emails comparados
  por una columna normalizada (`ToUpperInvariant`) con índice único.
- Toda página exige usuario autenticado salvo registro e inicio de sesión. Una solicitud HTMX
  sin sesión recibe 401 sin datos y el encabezado `HX-Redirect` al login (AC-05, AC-04e); en
  ambos casos el login recibe `motivo=sin-sesion` y muestra "Iniciá sesión para continuar."
  (FR-051, AC-05k). Estos eventos viven en `Infra/EventosCookie.cs` y se implementan después de
  sus tests (Principio I).
- **Alternatives considered**: `SlidingExpiration = true` (solo renueva pasada la mitad del
  plazo, no cumple "30 días desde la última solicitud", y renovaría con el polling).

## R10. Código de hogar (FR-046)

- **Decision**: `RandomNumberGenerator.GetItems` sobre el alfabeto
  `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (sin 0, O, 1, I, L), 8 caracteres; se compara en mayúsculas.
- **Rationale**: generador criptográfico (riesgo de aleatoriedad del PRD); ~8,5·10¹¹
  combinaciones.

## R11. Estrategia de tests (Principio I)

- **Decision**: xUnit en tres niveles, todos test-first:
  - **Dominio**: reglas puras (generación de instancias, vencimientos, código, contenido de
    emails) con `FakeTimeProvider`.
  - **Servicios y procesador**: contra SQLite en memoria (`DataSource=:memory:` con la conexión
    abierta durante el test), con el esquema creado aplicando las **migraciones** en el fixture
    del test. Esto no contradice `AGENTS.md`, que prohíbe `Migrate()` *al arrancar la app*; la
    app nunca lo llama.
  - **Web**: `WebApplicationFactory<Program>` reemplazando `TimeProvider`, `IEmailSender`
    (un `FakeEmailSender` que registra envíos y puede simular rechazos o demoras) y la base.
- Los criterios de aceptación del PRD (AC-xx) se usan como nombres de tests, para trazar cada
  test a su requerimiento.

## R12. Front-end

- **Decision**: Razor Pages con HTML y CSS propios, mobile-first (ancho mínimo 360 px, sin
  scroll horizontal, FR-042). HTMX 2.x copiado en `wwwroot/lib/htmx` (sin CDN). El token
  antiforgery se envía en `hx-headers` desde el layout.
- **Rationale**: sin build de front-end; HTMX alcanza para formularios parciales y polling.
