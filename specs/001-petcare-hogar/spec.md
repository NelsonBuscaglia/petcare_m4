# Feature Specification: PetCare Hogar — seguimiento colaborativo del cuidado de mascotas

**Feature Branch**: `001-petcare-hogar`

**Created**: 2026-10-10

**Status**: Draft

**Input**: User description: "Generá el spec a partir de PRD.md, que está ubicado en la raiz del repositorio."

**Fuente**: `PRD.md` (PRD-001). Cada requerimiento de este spec indica entre paréntesis el RF/RNF
del PRD que cubre, y cada escenario de aceptación el AC del PRD del que proviene. Los términos
en *cursiva* se definen en el Glosario del PRD, que se aplica tal cual a este spec.

## Clarifications

### Session 2026-10-10

- Q: Si el servicio de email falla al mandar una alarma, ¿el sistema reintenta? → A: Reintenta
  hasta lograr la entrega o hasta que pasen los 5 minutos del RNF-01; se considera enviado solo
  si el servicio lo aceptó.
- Q: Si un cuidador da de baja una instancia y otro registra su cumplimiento casi a la vez,
  ¿qué pasa? → A: Gana la primera operación que llega; la segunda se rechaza informando el
  motivo ("ya fue dada de baja" o "ya fue cumplida").
- Q: ¿Cuánto dura la sesión de un cuidador antes de pedirle que vuelva a iniciarla? → A: Vence
  tras 30 días sin uso; cada uso renueva el plazo.
- Las tres respuestas se incorporaron a `PRD.md` (RF-02c, RF-14d, RF-14e, RF-21d, RF-21e,
  RF-32c, RF-32d; AC-04c a AC-04e, AC-32c a AC-32e, AC-62 a AC-62c). Al hacerlo se precisó que
  la actualización automática del estado del día no renueva la sesión.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sumarse al hogar compartido (Priority: P1)

Una persona se registra con email y contraseña. Si es la primera, el sistema crea el hogar y le
muestra el *código de hogar*; las siguientes se suman ingresando ese código. Una vez dentro,
cada cuidador inicia y cierra sesión, y nadie sin sesión puede ver ni modificar datos del hogar.

**Why this priority**: es la puerta de entrada; sin hogar compartido y acceso protegido no hay
visibilidad común entre cuidadores, que es el problema central del PRD.

**Independent Test**: registrar a una primera persona, verificar que se crea el hogar y se
muestra el código; registrar a una segunda con el código y verificar que ve los mismos datos;
verificar que sin sesión toda consulta o modificación es rechazada.

**Acceptance Scenarios**:

1. **Given** que no existe ningún cuidador, **When** una persona se registra con email y
   contraseña, **Then** el sistema crea el hogar y le muestra el código de hogar (AC-01).
2. **Given** un hogar existente con la mascota A, **When** una segunda persona se registra con
   el código correcto, **Then** al iniciar sesión ve una sección para la mascota A (AC-02).
3. **Given** un hogar existente, **When** una persona intenta registrarse con un código
   incorrecto o sin código, **Then** el registro se rechaza y no ve ningún dato (AC-02b, AC-02c).
4. **Given** un cuidador con email ana@ejemplo.com, **When** otra persona intenta registrarse con
   Ana@Ejemplo.com, **Then** el registro se rechaza (AC-02d, AC-02e).
5. **Given** un cuidador registrado, **When** inicia sesión con su email en otra combinación de
   mayúsculas y su contraseña, **Then** ve el estado del día; con contraseña incorrecta, se
   rechaza (AC-03, AC-03b, AC-04).
6. **Given** un cuidador que cerró sesión o una persona sin sesión, **When** pide ver o modificar
   cualquier dato del hogar o el código de hogar, **Then** el sistema lo rechaza sin devolver
   datos ni aplicar cambios (AC-04b, AC-05 a AC-05j).
7. **Given** un segundo cuidador con sesión, **When** consulta el código de hogar, **Then** ve el
   mismo código que se mostró a la primera persona (AC-01b).
8. **Given** un cuidador cuya última solicitud fue hace más de 30 días, **When** intenta ver el
   estado del día, **Then** se le pide iniciar sesión sin mostrar datos; si usó la aplicación en
   ese lapso, la sesión sigue vigente; los refrescos automáticos de una pantalla abierta no la
   renuevan (AC-04c, AC-04d, AC-04e).

---

### User Story 2 - Cargar mascotas y deberes programables y ver el estado del día (Priority: P1)

Un cuidador registra las mascotas del hogar y, desde la *configuración de deberes* de cada una,
da de alta *deberes programables* (comida, paseo o medicación) a hora fija o cada N horas, solo
para hoy o todos los días (y, para medicación, para siempre o hasta una fecha). El *estado del
día* muestra, por mascota, las *instancias vigentes* resultantes.

**Why this priority**: junto con la US3 forma el MVP: sin deberes e instancias visibles no hay
nada que cumplir ni compartir.

**Independent Test**: registrar una mascota, darle de alta un paseo cada 4 horas desde las 08:00
todos los días y verificar en el estado del día las cuatro instancias de hoy y de mañana.

**Acceptance Scenarios**:

1. **Given** un hogar sin mascotas, **When** se registra una mascota, **Then** el estado del día
   muestra una sección con su nombre; un segundo registro con el mismo nombre (sin distinguir
   mayúsculas) se rechaza (AC-06, AC-06b, AC-06c).
2. **Given** "paseo cada 4 horas desde las 08:00, todos los días", **When** se consulta el estado
   a las 07:00, **Then** se ven exactamente las instancias de 08:00, 12:00, 16:00 y 20:00 (AC-10).
3. **Given** el alta de un deber cada N horas, **When** se ingresa N = 0 o N = 24, **Then** el
   alta se rechaza (AC-10b, AC-10c).
4. **Given** "paseo, hora fija 09:00, todos los días" dado de alta hoy, **When** se consulta
   mañana a las 08:00, **Then** aparece la instancia de las 09:00 de mañana; un deber "solo para
   hoy" no aparece mañana (AC-11, AC-12).
5. **Given** "medicación 09:00, todos los días, hasta el día 10", **When** se consulta el día 10
   y el día 11, **Then** el día 10 aparece su instancia y el día 11 no se genera ninguna nueva
   (AC-13, AC-13b); una fecha de fin anterior a hoy se rechaza (AC-13d), y una medicación "para
   siempre" sigue generando instancias indefinidamente (AC-15).
6. **Given** un deber de la mascota A, **When** se consulta el estado de la mascota B, **Then** no
   aparece ninguna instancia de ese deber (AC-09).
7. **Given** una mascota con deberes activos, **When** se consulta su configuración de deberes,
   **Then** se ve cada deber activo una sola vez con los datos con que fue dado de alta, y no los
   que ya no son activos (AC-19b a AC-19f).
8. **Given** que hoy a las 13:00 se da de alta "comida cada 4 horas desde las 08:00, solo para
   hoy", **When** se consulta el estado, **Then** se generaron también las instancias de horarios
   ya pasados (AC-14b).

---

### User Story 3 - Registrar y corregir cumplimientos (Priority: P1)

Desde el estado del día, un cuidador registra que una instancia se cumplió (con la hora real,
editable, para comida/paseo/medicación; solo fecha para baño o visita/estudio). Si se equivocó,
elimina el registro del día y la instancia vuelve a estar vigente. El estado del día resume
paseos del día, última comida y último paseo.

**Why this priority**: es el valor central del producto: evita tareas duplicadas y deja
constancia compartida de lo hecho.

**Independent Test**: con una instancia vigente de paseo, registrar su cumplimiento y verificar
que desaparece del estado del día y que la cantidad de paseos aumenta en 1; eliminar el registro
y verificar que la instancia vuelve.

**Acceptance Scenarios**:

1. **Given** una instancia vigente de paseo, **When** se registra su cumplimiento, **Then** deja
   de aparecer y la cantidad de paseos del día aumenta en 1 (AC-32).
2. **Given** dos cuidadores viendo la misma instancia, **When** ambos registran su cumplimiento,
   **Then** el segundo es rechazado con aviso de que ya fue cumplida y se cuenta un solo paseo
   (AC-32b).
3. **Given** dos cuidadores viendo la misma instancia, **When** uno la da de baja y el otro,
   antes de ver el cambio, registra su cumplimiento o también la da de baja (o a la inversa),
   **Then** se aplica la primera operación y la segunda se rechaza informando si la instancia ya
   fue dada de baja o ya fue cumplida (AC-32c, AC-32d, AC-32e).
4. **Given** una instancia vigente de comida, **When** se registra a las 10:30 aceptando la hora
   propuesta o cambiándola a 10:00, **Then** la última comida muestra hoy con esa hora; una hora
   posterior a la actual (11:00) se rechaza (AC-35, AC-35b, AC-35c).
5. **Given** una instancia vigente de baño, **When** se registra su cumplimiento, **Then** no se
   pide hora (AC-36).
6. **Given** un registro de hoy cargado por error, **When** se elimina, **Then** la instancia
   vuelve a estar vigente (con su marca si corresponde) y los contadores se ajustan; eliminar un
   registro de ayer se rechaza (AC-37, AC-37b, AC-38).
7. **Given** una mascota con cumplimientos de días anteriores o sin ninguno, **When** se consulta
   el estado del día, **Then** se ven la fecha y hora de la última comida y del último paseo, o
   que no hay ninguno (AC-39, AC-39c, AC-39d).
8. **Given** una instancia vigente de paseo, **When** se registra su cumplimiento aceptando la
   hora por defecto, **Then** se completa en 5 toques/clics o menos desde la pantalla principal
   (AC-60).

---

### User Story 4 - Ver vencimientos y arrastre de tareas olvidadas (Priority: P2)

El estado del día marca como "pendiente" las instancias de comida, paseo o medicación con más de
15 minutos de atraso. Comida y paseo no cumplidos expiran al terminar su día; la medicación no
cumplida sigue vigente en los días siguientes hasta que se cumpla o se dé de baja.

**Why this priority**: hace visible el olvido, el segundo problema del PRD, aun sin emails.

**Independent Test**: con relojes controlados, verificar la marca "pendiente" a los 15 minutos,
la expiración de comida/paseo al cambio de día y el arrastre de la medicación.

**Acceptance Scenarios**:

1. **Given** "medicación, hora fija 09:00" sin cumplir, **When** se consulta a las 09:10 y a las
   09:15, **Then** a las 09:10 no está "pendiente" y a las 09:15 sí (AC-29, AC-29b, AC-44).
2. **Given** una comida diaria de las 08:00 no cumplida ayer, **When** se consulta hoy a las
   07:00, **Then** no aparece la de ayer y aparece la de hoy sin marca (AC-30).
3. **Given** una medicación diaria no cumplida ayer, **When** se consulta hoy a las 08:00, **Then**
   aparecen la de ayer "pendiente" y la de hoy sin marca; cumplir la de ayer no afecta la de hoy
   (AC-31, AC-31b, AC-13c).
4. **Given** el cambio de día en UTC−3, **When** se consulta a las 00:05 y a las 23:59 (UTC−3),
   **Then** las instancias del nuevo día aparecen recién después de las 00:00 UTC−3 (AC-28,
   AC-28b, AC-61).
5. **Given** el servidor caído entre las 23:00 de ayer y las 10:00 de hoy, **When** vuelve a
   funcionar, **Then** se generan las instancias faltantes de hoy, ya marcadas como "pendiente"
   si corresponde (AC-28c).
6. **Given** que hoy a las 10:30 se da de alta "paseo 10:00, solo para hoy", **When** se consulta
   el estado, **Then** la instancia ya aparece "pendiente" (AC-14).

---

### User Story 5 - Agendar baños y visitas/estudios (Priority: P2)

Un cuidador agenda *deberes puntuales* de baño o visita/estudio (con subtipo veterinario,
estudio, vacuna o intervención médica) en una fecha y hora exactas. Aparecen en el estado del
día cuando llega su día, y si se supera su fecha y hora sin cumplirse quedan marcados como "no
cumplido" hasta que se cumplan o se den de baja. Además, cada mascota puede tener un máximo de
días entre baños.

**Why this priority**: amplía el seguimiento a cuidados de salud e higiene, menos frecuentes
pero de mayor costo si se olvidan.

**Independent Test**: agendar un baño para dentro de unos días, verificar que no aparece hasta
ese día, que aparece sin marca antes de la hora y "no cumplido" después.

**Acceptance Scenarios**:

1. **Given** un baño agendado el lunes para el viernes 15:00, **When** se consulta el viernes a
   las 09:00, **Then** aparece sin marca "no cumplido" (AC-17); un turno para dentro de 3 días no
   aparece en el estado del día (AC-39b); un baño de hoy a las 15:00 se ve junto a las demás
   instancias del día y, al cumplirlo, solo él desaparece (AC-33, AC-33b).
2. **Given** el alta de un baño o visita/estudio, **When** la fecha es anterior a hoy, **Then** se
   rechaza; si es hoy con hora pasada, se acepta y aparece "no cumplido" (AC-17b, AC-17c, AC-17d).
3. **Given** el alta de una visita/estudio, **When** se elige el subtipo, **Then** las únicas
   opciones son veterinario, estudio, vacuna e intervención médica, y el subtipo se muestra en el
   estado del día (AC-18, AC-40).
4. **Given** una visita/estudio de hace 3 días sin cumplir, **When** se consulta hoy, **Then**
   aparece "no cumplido"; al registrar su cumplimiento, desaparece (AC-45, AC-46, AC-16).
5. **Given** una mascota sin deberes, **When** se define y luego se modifica el máximo de días
   entre baños, **Then** la configuración de deberes muestra el valor vigente (AC-08, AC-07).

---

### User Story 6 - Dar de baja deberes e instancias (Priority: P2)

Un cuidador da de baja en forma definitiva un deber desde la configuración de deberes (y con él
sus instancias sin cumplir), o una sola instancia desde el estado del día. Si el deber tenía una
única instancia, darla de baja da de baja también el deber.

**Why this priority**: permite terminar tratamientos y corregir planes; sin esto, los deberes
obsoletos generarían alarmas falsas.

**Independent Test**: dar de baja una instancia de un paseo recurrente y verificar que el deber
sigue y que mañana vuelven las cuatro instancias; dar de baja un deber y verificar que desaparece
de la configuración y del estado del día.

**Acceptance Scenarios**:

1. **Given** una visita/estudio de mañana o un paseo diario, **When** hoy se da de baja el deber,
   **Then** deja de aparecer en la configuración y mañana no hay instancias (AC-20d, AC-24).
2. **Given** una medicación diaria con las instancias de ayer y hoy sin cumplir, **When** se da de
   baja el deber, **Then** ninguna aparece y no se envía email de vencimiento por ellas (AC-20b).
3. **Given** un paseo diario cumplido hoy a las 09:05, **When** a las 12:00 se da de baja el
   deber, **Then** el paseo sigue contado en el día (AC-25).
4. **Given** un baño de hoy o una comida "solo para hoy", **When** se da de baja su instancia,
   **Then** se da de baja también el deber (AC-20, AC-20c).
5. **Given** un paseo cada 4 horas diario, **When** se da de baja la instancia de las 12:00,
   **Then** quedan las de 08:00, 16:00 y 20:00, el deber sigue activo y mañana vuelven las cuatro
   (AC-23, AC-23b).

---

### User Story 7 - Ver cambios de otros cuidadores sin recargar (Priority: P2)

Mientras un cuidador tiene abierto el estado del día, la pantalla se actualiza sola cuando otro
cuidador hace algo o cuando una instancia pasa a "pendiente" o "no cumplido".

**Why this priority**: el objetivo del PRD exige que el estado sea confiable cuando otro
cuidador está por actuar, no solo al recargar.

**Independent Test**: con dos sesiones abiertas, registrar un cumplimiento en una y verificar
que la otra lo refleja en 15 segundos o menos sin recargar.

**Acceptance Scenarios**:

1. **Given** dos cuidadores mirando el estado del día, **When** uno registra un cumplimiento o da
   de baja una instancia, **Then** el otro lo ve reflejado en 15 segundos o menos (AC-47, AC-47d).
2. **Given** un cuidador que no interactúa con la pantalla, **When** una instancia pasa a
   "pendiente" o "no cumplido", **Then** su pantalla lo muestra en 15 segundos o menos (AC-47b,
   AC-47c).

---

### User Story 8 - Recibir alarmas y recordatorios por email (Priority: P3)

Todos los cuidadores reciben un email cuando una instancia vence, un recordatorio 48 horas antes
de un baño o visita/estudio, y un aviso diario a las 07:00 cuando hay que pedir turno de baño
para una mascota. Los emails se envían aunque nadie tenga la aplicación abierta.

**Why this priority**: es el segundo objetivo del PRD, pero depende de que existan deberes,
instancias y vencimientos (US2, US4, US5).

**Independent Test**: con relojes controlados y un buzón de prueba, verificar cada tipo de email
en su momento, una sola vez, y su ausencia en los casos excluidos.

**Acceptance Scenarios**:

1. **Given** una medicación de las 09:00 sin cumplir y nadie con la aplicación abierta, **When**
   llegan las 09:15, **Then** todos los cuidadores reciben un email de vencimiento a más tardar a
   las 09:20; si se cumplió a las 09:05, no se envía (AC-48, AC-49).
2. **Given** una visita/estudio "vacuna" de hoy a las 10:00 sin cumplir, **When** se superan las
   10:00, **Then** a más tardar a las 10:05 se envía el email de vencimiento con el subtipo
   (AC-43, AC-50).
3. **Given** una instancia que ya estaba vencida al generarse, **When** termina el día, **Then**
   no se envió email de vencimiento por ella (AC-14, AC-17d, AC-28c).
4. **Given** un registro erróneo que evitó el email, **When** se elimina, **Then** se envía el
   email a más tardar 5 minutos después; si ya se había enviado, no se repite (AC-38b, AC-38c).
5. **Given** un baño o visita/estudio agendado con 48 horas o más de anticipación, **When** faltan
   48 horas, **Then** se envía exactamente un recordatorio (con subtipo si aplica) a más tardar 5
   minutos después; no se envía si se agendó con menos anticipación o se dio de baja antes
   (AC-51, AC-51b, AC-51c, AC-52, AC-52b, AC-42).
6. **Given** que el servicio de email rechaza envíos, **When** vuelve a aceptarlos dentro de los 5
   minutos posteriores a la condición, **Then** el email se entrega; si sigue rechazando toda la
   ventana, el email se pierde sin más reintentos, y se envía si la instancia vuelve a vencer
   tras eliminar un cumplimiento (AC-62, AC-62b, AC-62c).

---

### User Story 9 - Recibir aviso de turno de baño (Priority: P3)

Si una mascota tiene un máximo de días entre baños, ya lo superó desde su último baño (o desde su
alta) y no tiene ningún baño activo, todos los cuidadores reciben a las 07:00 un email que indica
que hay que pedir turno, todos los días mientras siga en esa situación.

**Why this priority**: es una ayuda preventiva; el valor principal ya está cubierto por las
historias anteriores.

**Independent Test**: con una mascota con máximo de 20 días y último baño hace 25, verificar un
único aviso en el día a más tardar a las 07:05, y su ausencia en cada caso excluido.

**Acceptance Scenarios**:

1. **Given** máximo 20 días, último baño hace 25 y sin baño activo, **When** llegan las 07:00,
   **Then** se envía un aviso a más tardar a las 07:05, uno solo en el día, y se repite al día
   siguiente si nada cambia (AC-53, AC-53b, AC-58).
2. **Given** el mismo caso pero con un baño activo (futuro, de hoy o "no cumplido"), **When**
   termina el día, **Then** no se envió aviso; si ese baño se da de baja, el aviso se envía al día
   siguiente (AC-54, AC-54b, AC-54c, AC-53c).
3. **Given** último baño hace exactamente 20 días, o sin máximo definido, **When** termina el
   día, **Then** no se envió aviso (AC-53d, AC-55).
4. **Given** una mascota dada de alta hace 25 días sin baños y máximo 20, **When** llegan las
   07:00, **Then** se envía el aviso (AC-56).

---

### Edge Cases

- Dos cuidadores registran a la vez el cumplimiento de la misma instancia: solo uno queda
  registrado y el otro recibe aviso de que ya fue cumplida (FR-028, FR-029).
- Un deber se da de alta con horarios ya pasados del día: las instancias se generan igual, se
  marcan como vencidas y no disparan email de vencimiento (FR-018, FR-019, FR-037).
- El servidor está caído durante el cambio de día o al vencer instancias: al volver se generan
  las instancias faltantes; los emails cuya condición se cumplió mientras estaba caído se pierden
  (FR-020; Fuera de Alcance del PRD).
- Medicación con fecha de fin cuya última dosis no se cumplió: el deber sigue activo y su
  instancia sigue "pendiente" después de la fecha de fin, sin generar nuevas (AC-13c, AC-19e).
- Se elimina un registro de cumplimiento de una instancia que ya había generado email de
  vencimiento: no se envía un segundo email (FR-036).
- Un cuidador da de baja una instancia mientras otro registra su cumplimiento: se aplica la
  primera operación y la segunda se rechaza informando el motivo (FR-045).
- El servicio de email falla: se reintenta hasta 5 minutos desde la condición; si no se logra,
  ese email se pierde y la instancia sigue marcada en pantalla (FR-041).
- Se da de baja la instancia de un deber de instancia única: se da de baja también el deber
  (FR-016).
- Se intenta eliminar un registro de cumplimiento de un día anterior: se rechaza (FR-032).
- El cambio de día y todos los horarios se evalúan en UTC−3 sin horario de verano, aunque el
  servidor o el navegador estén en otra zona horaria (FR-044).

## Requirements *(mandatory)*

### Functional Requirements

**Registro, sesión y acceso**

- **FR-001** (RF-01a, RF-01b, RF-01d, RF-01e): El sistema DEBE permitir registrarse con email y
  contraseña; al registrarse la primera persona DEBE crear el hogar, generar el código de hogar y
  mostrárselo.
- **FR-002** (RF-01c): Si el hogar ya existe, el sistema DEBE asociar a la nueva persona solo si
  ingresa el código de hogar correcto, y rechazar el registro en caso contrario.
- **FR-003** (RF-01f): El sistema DEBE rechazar el registro con un email que ya pertenece a un
  cuidador, comparando sin distinguir mayúsculas de minúsculas.
- **FR-004** (RF-01g): Un cuidador con sesión iniciada DEBE poder consultar el código de hogar.
- **FR-005** (RF-02, RF-02b, RF-02c): Un cuidador DEBE poder iniciar sesión con email (sin distinguir
  mayúsculas) y contraseña, y cerrar su sesión. La sesión DEBE vencer tras 30 días sin uso, y
  cada uso DEBE renovar ese plazo; vencida, el cuidador DEBE volver a iniciar sesión. Las
  actualizaciones automáticas del estado del día (FR-035) no cuentan como uso.
- **FR-006** (RF-03, RF-03b): El sistema DEBE impedir que una persona sin sesión iniciada vea o
  modifique cualquier dato del hogar (mascotas, deberes, instancias, registros de cumplimiento,
  máximo de días entre baños, código de hogar), incluidas las actualizaciones automáticas.

**Mascotas**

- **FR-007** (RF-04, RF-04b): El sistema DEBE permitir registrar mascotas indicando su nombre y
  rechazar un nombre repetido en el hogar, sin distinguir mayúsculas de minúsculas.
- **FR-008** (RF-05, RF-05b): El sistema DEBE permitir definir y modificar, desde la
  configuración de deberes, un máximo de días entre baños por mascota, sin requerir un deber.

**Deberes**

- **FR-009** (RF-06a, RF-06b, RF-06c): El sistema DEBE permitir dar de alta deberes programables
  (comida, paseo, medicación) de una única mascota, a hora fija o cada N horas desde una hora de
  inicio, con N entero entre 1 y 23.
- **FR-010** (RF-08, RF-09, RF-09b): Al dar de alta un deber programable, el cuidador DEBE elegir
  "solo para hoy" o "todos los días"; para medicación diaria, además, "para siempre" o "hasta una
  fecha" inclusive, no anterior a hoy.
- **FR-011** (RF-10, RF-10b, RF-10c, RF-11): El sistema DEBE permitir dar de alta deberes
  puntuales (baño, visita/estudio) con fecha y hora exactas, sin recurrencia, rechazando fechas
  anteriores a hoy y aceptando hoy con hora pasada; las visitas/estudios DEBEN llevar subtipo
  veterinario, estudio, vacuna o intervención médica.
- **FR-012** (RF-11b, RF-11c): La configuración de deberes de cada mascota DEBE mostrar sus
  deberes activos con los datos de alta y permitir dar de alta deberes desde ella.
- **FR-013** (RF-12a, RF-12b, RF-15, RF-16): El sistema DEBE permitir dar de baja un deber en
  forma definitiva desde la configuración de deberes, dando de baja sus instancias sin
  cumplimiento, dejando de generar nuevas y conservando los registros de cumplimiento existentes.

**Instancias**

- **FR-014** (RF-07): En la modalidad cada N horas, el sistema DEBE generar una instancia por
  cada horario de la serie hora de inicio + k·N dentro del mismo día, independiente de cuándo se
  cumplió la anterior.
- **FR-015** (RF-14, RF-14c): El sistema DEBE permitir dar de baja cualquier instancia vigente
  desde el estado del día, conservando el deber y sus demás instancias.
- **FR-016** (RF-14b): Al dar de baja la instancia de un deber de instancia única (baño,
  visita/estudio, o programable de hora fija "solo para hoy"), el sistema DEBE dar de baja
  también el deber.
- **FR-017** (RF-17, RNF-05): El sistema DEBE generar las instancias del día de cada deber diario
  dentro de los 5 minutos posteriores al inicio del día, sin depender de usuarios conectados.
- **FR-018** (RF-17b): Al dar de alta un deber programable, el sistema DEBE generar en ese
  momento las instancias del día, incluidas las de horarios ya pasados.
- **FR-019** (RF-18, RF-29): El sistema DEBE considerar vencida y marcar como "pendiente" una
  instancia de comida, paseo o medicación cuando pasan 15 minutos desde su horario sin
  cumplimiento, hasta que se cumpla, se dé de baja, se dé de baja su deber o expire.
- **FR-020** (RF-17c): Al volver a estar en ejecución, el sistema DEBE generar las instancias del
  día actual que falten.
- **FR-021** (RF-19): El sistema DEBE dar por expirada, al terminar su día, toda instancia no
  cumplida de comida o paseo, sin trasladarla al día siguiente.
- **FR-022** (RF-20): Las instancias de medicación no cumplidas DEBEN seguir vigentes en los días
  posteriores hasta que se cumplan, se den de baja o se dé de baja su deber.
- **FR-023** (RF-30a, RF-30b): El sistema DEBE marcar como "no cumplido" toda instancia vigente de
  baño o visita/estudio cuya fecha y hora se superaron, y mantener la marca en los días
  posteriores hasta que se cumpla o se dé de baja la instancia o el deber.

**Registro de cumplimiento**

- **FR-024** (RF-21): El sistema DEBE permitir registrar desde el estado del día el cumplimiento
  de cualquier instancia vigente.
- **FR-025** (RF-23): Para comida, paseo o medicación, el sistema DEBE pedir la hora real de
  cumplimiento, proponiendo la hora actual y permitiendo editarla.
- **FR-026** (RF-23b): El sistema DEBE rechazar una hora real de cumplimiento posterior a la
  actual.
- **FR-027** (RF-24): Para baño o visita/estudio, el sistema DEBE guardar solo la fecha de
  cumplimiento, sin pedir hora.
- **FR-028** (RF-21b): El sistema DEBE rechazar el cumplimiento de una instancia ya cumplida, sin
  crear un segundo registro, aun cuando dos cuidadores lo intenten a la vez.
- **FR-029** (RF-21c): Al rechazar según FR-028, el sistema DEBE informar al cuidador que la
  instancia ya fue cumplida.
- **FR-045** (RF-14d, RF-14e, RF-21d, RF-21e): El sistema DEBE rechazar el registro de cumplimiento o la baja de
  una instancia que ya no está vigente (porque otro cuidador la cumplió o la dio de baja, o se
  dio de baja su deber), sin modificarla, e informar el motivo ("ya fue cumplida" o "ya fue
  dada de baja"). Ante operaciones simultáneas sobre la misma instancia, se aplica la primera.
- **FR-030** (RF-25, RF-26): El sistema DEBE permitir eliminar un registro de cumplimiento del
  día actual, dejando nuevamente vigente la instancia sin afectar al deber.
- **FR-031** (Fuera de Alcance del PRD): El sistema NO DEBE permitir editar un registro de
  cumplimiento ni registrar cumplimientos que no correspondan a una instancia.
- **FR-032** (RF-25): El sistema DEBE rechazar la eliminación de registros de cumplimiento de días
  anteriores.

**Estado del día**

- **FR-033** (RF-22, RF-28a): El estado del día DEBE mostrar, por mascota, sus instancias
  vigentes: comida, paseo y medicación del día; medicación arrastrada de días anteriores; y baño
  o visita/estudio agendados para hoy o días anteriores, con el subtipo de las visitas/estudios.
  Es la pantalla que se muestra al iniciar sesión.
- **FR-034** (RF-27a, RF-27b, RF-27c): El estado del día DEBE mostrar, por mascota, la cantidad de
  paseos realizados en el día y la fecha y hora real del último cumplimiento de comida y de
  paseo, aunque sea de un día anterior, o que no hay ninguno.
- **FR-035** (RF-31, RNF-04): El estado del día DEBE actualizarse sin recargar, dentro de los 15
  segundos posteriores, ante cualquier cambio de otro cuidador o el paso de una instancia a
  "pendiente" o "no cumplido".

**Emails**

- **FR-036** (RF-32, RF-33): El sistema DEBE enviar a todos los cuidadores un único email de
  vencimiento por instancia cuando ésta pasa a vencida (comida, paseo, medicación, baño o
  visita/estudio), sin depender de usuarios conectados; nunca más de uno por instancia, aunque se
  elimine su cumplimiento y vuelva a vencer.
- **FR-037** (RF-32b): El sistema NO DEBE enviar email de vencimiento por una instancia que ya
  estaba vencida al generarse.
- **FR-038** (RF-34): El sistema DEBE enviar a todos los cuidadores un único email recordatorio 48
  horas antes de un baño o visita/estudio dado de alta con al menos 48 horas de anticipación y no
  dado de baja.
- **FR-039** (RF-28c, RF-28d): Los emails de vencimiento y recordatorio de visitas/estudios DEBEN
  indicar el subtipo.
- **FR-040** (RF-35): Todos los días a las 07:00, el sistema DEBE enviar a todos los cuidadores un
  email de aviso de turno de baño por cada mascota que tenga máximo de días entre baños definido,
  haya superado ese máximo desde el último baño registrado (o desde su alta si no hay ninguno) y
  no tenga ningún deber de baño activo.
- **FR-041** (RNF-01, RF-32c, RF-32d): Todo email de vencimiento, recordatorio o aviso de turno de baño DEBE
  entregarse al servicio de envío dentro de los 5 minutos posteriores a que se cumpla su
  condición, mientras el sistema esté en ejecución. Si el servicio rechaza o no responde, el
  sistema DEBE reintentar dentro de esa ventana de 5 minutos y dejar de reintentar al vencerla;
  un email cuenta como enviado (FR-036, FR-038) solo si el servicio lo aceptó.

**Generales**

- **FR-042** (RNF-02): Todas las funciones DEBEN poder usarse desde un navegador móvil de 360
  píxeles de ancho o más, sin scroll horizontal y sin instalar una app.
- **FR-043** (RNF-03): Registrar un cumplimiento aceptando la hora propuesta DEBE requerir 5
  toques/clics o menos desde el estado del día.
- **FR-044** (RNF-06): Todas las horas de los deberes y el inicio y fin de cada día DEBEN
  interpretarse en UTC−3, sin horario de verano.

### Key Entities *(include if feature involves data)*

- **Hogar**: único en el sistema; tiene un código de hogar y agrupa a cuidadores y mascotas.
- **Cuidador**: persona registrada con email (único, sin distinguir mayúsculas) y contraseña;
  pertenece al hogar y ve y modifica todos sus datos.
- **Mascota**: pertenece al hogar; tiene nombre (único en el hogar, sin distinguir mayúsculas),
  fecha de alta y, opcionalmente, un máximo de días entre baños.
- **Deber de cuidado**: pertenece a una mascota; tiene tipo (comida, paseo, medicación, baño,
  visita/estudio), subtipo si es visita/estudio, modalidad (hora fija o cada N horas desde una
  hora de inicio) y recurrencia (solo para hoy, todos los días, hasta una fecha) si es
  programable, o fecha y hora si es puntual; fecha de alta y estado (activo o dado de baja).
- **Instancia**: ocurrencia de un deber en un horario concreto; estados vigente, cumplida,
  expirada o dada de baja; se marca vencida ("pendiente" o "no cumplido") según su tipo.
- **Registro de cumplimiento**: constancia única de que una instancia se cumplió; guarda fecha y,
  para comida, paseo y medicación, la hora real.
- **Constancia de email enviado**: registro de cada email de vencimiento, recordatorio o aviso
  de turno de baño que el servicio de envío aceptó; necesaria para no repetir envíos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los emails de vencimiento, recordatorio y aviso de turno de baño se
  entregan al servicio de envío en 5 minutos o menos desde que se cumple su condición, sin
  ningún cuidador conectado, salvo que el servicio rechace o no responda durante toda esa
  ventana (RNF-01).
- **SC-002**: Un cambio hecho por un cuidador se ve en la pantalla de otro cuidador con la
  aplicación abierta en 15 segundos o menos, en el 100% de los casos (RNF-04).
- **SC-003**: Registrar un cumplimiento con la hora propuesta lleva 5 toques/clics o menos desde
  la pantalla principal (RNF-03).
- **SC-004**: Los 15 flujos del AC-59 del PRD se completan en un navegador de 360 píxeles de
  ancho sin scroll horizontal (RNF-02).
- **SC-005**: Las instancias de los deberes diarios están disponibles en 5 minutos o menos desde
  el inicio del día (00:00 UTC−3) (RNF-05).
- **SC-006**: Ninguna instancia queda con más de un registro de cumplimiento ni recibe más de un
  email de vencimiento, aun con registros simultáneos de varios cuidadores.
- **SC-007**: Un cuidador puede saber qué falta hacer hoy para cada mascota solo mirando el estado
  del día, sin preguntar a otro integrante del hogar: todos los criterios de aceptación del PRD
  (AC-01 a AC-61) se verifican.

## Assumptions

- El conjunto completo de criterios de aceptación es el de la sección "Criterios de Aceptación"
  de `PRD.md`; los escenarios de este spec los agrupan por historia y citan su identificador.
- Lo listado en "Fuera de Alcance" del PRD queda excluido de este spec (Principio III de la
  constitución), incluidos: múltiples hogares, roles, recuperación de contraseña, edición de
  deberes, mascotas o registros, historial, reenvío de emails perdidos durante caídas, y
  reenvío de emails que el servicio no aceptó tras 5 minutos de reintentos.
- Los días entre baños (FR-040) se cuentan como días calendario en UTC−3: con máximo 20, el
  aviso corresponde a partir del día 21 desde el último baño (AC-53d).
- El "último baño registrado" es el último registro de cumplimiento de un deber de baño de esa
  mascota.
- Las credenciales del servicio de envío de emails se proveen por configuración del entorno; en
  desarrollo los emails no se envían (constitución, Principio II).
- Volumen esperado: un único hogar con pocos cuidadores y mascotas; no se requiere escalar a
  muchos hogares (Riesgos del PRD).
- Dependencias: un servicio de envío de emails y un proceso programado que funcione sin usuarios
  conectados para generar instancias y disparar emails (Riesgos y Dependencias del PRD).
