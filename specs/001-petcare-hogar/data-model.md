# Data Model: PetCare Hogar

**Fase 1 de** [plan.md](./plan.md) · decisiones en [research.md](./research.md)

Convenciones: `Utc` = instante `DateTime` en UTC; `Dia` = `DateOnly` y `Hora` = `TimeOnly` en
hora del hogar (UTC−3, R2). Todos los cambios de esquema van por migraciones de EF Core.

## Hogar

| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | PK, siempre `1`: existe un único hogar (FR-048, R8) |
| Codigo | string(8) | Generado al crear el hogar (FR-046, R10); se guarda en mayúsculas |
| CreadoUtc | DateTime | |

## Cuidador

| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | PK |
| HogarId | int | FK → Hogar |
| Email | string | Tal como se ingresó |
| EmailNormalizado | string | `ToUpperInvariant`; **único** (FR-003) |
| PasswordHash | string | `PasswordHasher<Cuidador>`; contraseña ≥ 8 caracteres (FR-047) |
| CreadoUtc | DateTime | |

## Mascota

| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | PK |
| HogarId | int | FK → Hogar |
| Nombre | string | Obligatorio |
| NombreNormalizado | string | **Único** por hogar (FR-007) |
| CreadaDia | DateOnly | Base del aviso de baño sin baños registrados (FR-040) |
| MaxDiasEntreBanos | int? | Opcional; una vez definido se puede modificar pero no quitar (FR-008, Fuera de Alcance) |

## Deber

Un deber es **programable** (comida, paseo, medicación) o **puntual** (baño, visita/estudio).

| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | PK |
| MascotaId | int | FK → Mascota (FR-009: una única mascota) |
| Tipo | enum `TipoDeber` | Comida, Paseo, Medicacion, Bano, VisitaEstudio |
| Subtipo | enum `SubtipoVisita`? | Solo VisitaEstudio, obligatorio (FR-011, RF-11d): Veterinario, Estudio, Vacuna, IntervencionMedica |
| Modalidad | enum? | Solo programables: HoraFija, CadaNHoras (FR-009) |
| HoraInicio | TimeOnly? | Hora fija, o inicio de la serie cada N horas |
| IntervaloHoras | int? | Solo CadaNHoras: entero 1–23 (FR-009) |
| Recurrencia | enum? | Solo programables: SoloHoy, TodosLosDias (FR-010) |
| HastaDia | DateOnly? | Solo medicación diaria; null = para siempre; inclusive; ≥ hoy al alta (FR-010) |
| Dia, Hora | DateOnly?, TimeOnly? | Solo puntuales; `Dia ≥ hoy` al alta, hora pasada permitida si es hoy (FR-011) |
| CreadoUtc, CreadoDia | DateTime, DateOnly | `CreadoUtc` decide si corresponde recordatorio (FR-038) |
| DadoDeBajaUtc | DateTime? | Baja definitiva (FR-013); nunca vuelve a null |

**Deber activo** (calculado, FR-012 y FR-040 c): no dado de baja **y** (tiene una instancia
vigente no expirada **o** es diario y todavía puede generar instancias: `HastaDia` null o
`HastaDia > hoy`). Cubre AC-19b a AC-19f y AC-54c.

**Deber de instancia única** (FR-016): puntual, o programable `HoraFija` + `SoloHoy`.

## Instancia

| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | PK |
| DeberId | int | FK → Deber |
| Dia, Hora | DateOnly, TimeOnly | Horario en hora del hogar |
| ProgramadaUtc | DateTime | **Único** junto con `DeberId` (generación idempotente, R4) |
| Estado | enum | `Vigente`, `Cumplida`, `DadaDeBaja` (solo estados por acción, R3) |
| GeneradaUtc | DateTime | |
| GeneradaVencida | bool | Ya estaba vencida al generarse (FR-037) |
| EpisodioVencimiento | int | Empieza en 0; +1 al eliminar su cumplimiento (R7) |
| ReactivadaUtc | DateTime? | Momento de la última eliminación de su cumplimiento |

**Marcas calculadas al leer** (R3):

| Marca | Tipos | Condición |
|---|---|---|
| "pendiente" (vencida) | comida, paseo, medicación | `Vigente` y `ahora ≥ ProgramadaUtc + 15 min` (FR-019) |
| "no cumplido" (vencida) | baño, visita/estudio | `Vigente` y `ahora > ProgramadaUtc` (FR-023) |
| expirada | comida, paseo | `Vigente` y `Dia < hoy` → no se muestra (FR-021) |

**Transiciones**:

```text
Vigente ──cumplir (FR-024)──────────────▶ Cumplida
Cumplida ──eliminar registro de hoy (FR-030)──▶ Vigente   (EpisodioVencimiento+1, ReactivadaUtc=ahora)
Vigente ──baja de instancia (FR-015) / baja de deber (FR-013)──▶ DadaDeBaja   (definitivo)
Cumplida/DadaDeBaja ──cumplir o dar de baja──▶ rechazo con motivo (FR-028, FR-045)
```

## RegistroCumplimiento

| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | PK |
| InstanciaId | int | FK → Instancia; **único** (FR-028) |
| Dia | DateOnly | Siempre el día en que se registra (FR-052) |
| Hora | TimeOnly? | Hora real, solo comida/paseo/medicación; no posterior a la actual (FR-025, FR-026); null en baño y visita/estudio (FR-027) |
| RegistradoUtc | DateTime | |

Se elimina físicamente (FR-030) solo si `Dia == hoy` (FR-032). Al dar de baja un deber, sus
registros se conservan (FR-013).

**Datos derivados del estado del día** (FR-034): paseos del día = registros de hoy de deberes
de paseo de la mascota; última comida/último paseo = registro de mayor (`Dia`, `Hora`).
**Último baño** (FR-040) = mayor `Dia` de los registros de deberes de baño de la mascota.

## Notificacion (outbox de emails)

| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | PK |
| Tipo | enum | Vencimiento, Recordatorio, AvisoTurnoBano |
| Clave | string | **Única**: `V:{instanciaId}:{episodio}`, `R:{deberId}`, `A:{mascotaId}:{dia}` (R7) |
| InstanciaId / DeberId / MascotaId | int? | Según el tipo |
| CondicionUtc | DateTime | Momento en que se cumplió la condición de disparo |
| Estado | enum | `Pendiente`, `Aceptada`, `Perdida` |
| Intentos | int | |
| UltimoIntentoUtc, AceptadaUtc | DateTime? | |

**Transiciones**: `Pendiente → Aceptada` cuando el servicio acepta (FR-041);
`Pendiente → Perdida` al pasar `CondicionUtc + 5 min` sin aceptación; se crea directamente
`Perdida` si la condición se detecta fuera de la ventana (servidor caído). Solo `Aceptada`
cuenta como enviado (FR-036, FR-038, FR-040). Destinatarios: todos los cuidadores del hogar al
momento del envío.

## Índices y restricciones

- `Cuidador.EmailNormalizado` único; `Mascota (HogarId, NombreNormalizado)` único.
- `Instancia (DeberId, ProgramadaUtc)` único; índice por `(Estado, Dia)` para el estado del día.
- `RegistroCumplimiento.InstanciaId` único; índice por `Dia`.
- `Notificacion.Clave` única; índice por `Estado`.
- Check: `IntervaloHoras BETWEEN 1 AND 23` (FR-009).
