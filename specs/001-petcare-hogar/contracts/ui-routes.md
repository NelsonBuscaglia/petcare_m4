# Contrato de UI: páginas y handlers

**Parte de** [plan.md](../plan.md). La aplicación expone páginas Razor (HTML) y handlers HTMX
que devuelven parciales. No hay API JSON pública.

## Reglas comunes

- Todas las rutas exigen sesión salvo `/Cuenta/Registro` y `/Cuenta/Login`. Sin sesión:
  navegación normal → redirección a `/Cuenta/Login?motivo=sin-sesion`; solicitud HTMX
  (`HX-Request: true`) → `401` sin cuerpo de datos y encabezado
  `HX-Redirect: /Cuenta/Login?motivo=sin-sesion` (FR-006, AC-05, AC-04e). Con
  `motivo=sin-sesion`, el login muestra "Iniciá sesión para continuar." (FR-051, AC-05k).
- Toda solicitud que modifica datos es `POST` con token antiforgery (enviado por HTMX en
  `hx-headers`).
- Todo rechazo de validación vuelve a mostrar el formulario o parcial con el **motivo
  concreto** (FR-051), salvo el login (FR-050). Los rechazos no modifican datos.
- Toda solicitud renueva la sesión salvo `GET /?handler=Refresco` (FR-005, R9).

## Cuenta

| Ruta | Método | Entrada | Resultado |
|---|---|---|---|
| `/Cuenta/Registro` | GET | — | Formulario: email, contraseña, código de hogar (opcional si no existe el hogar) |
| `/Cuenta/Registro` | POST | `Email`, `Password`, `CodigoHogar?` | Sin hogar: crea hogar y cuidador, inicia sesión y muestra el código (FR-001). Con hogar: valida primero el código (FR-049), después email duplicado (FR-003) y contraseña ≥ 8 (FR-047) |
| `/Cuenta/Login` | GET/POST | `Email`, `Password` | Éxito → `/`. Fallo → "El email o la contraseña son incorrectos" (FR-050) |
| `/Cuenta/Logout` | POST | — | Cierra la sesión (FR-005) |
| `/Hogar/Codigo` | GET | — | Muestra el código de hogar (FR-004) |

## Estado del día (pantalla principal)

| Ruta | Método | Entrada | Resultado |
|---|---|---|---|
| `/` | GET | — | Estado del día completo: una sección por mascota (FR-033, FR-034, FR-053) |
| `/?handler=Refresco` | GET (HTMX, `every 10s`) | — | Parcial del estado del día; **no renueva la sesión** (FR-035) |
| `/?handler=Cumplir` | POST (HTMX) | `InstanciaId`, `Hora?` | Registra el cumplimiento (FR-024 a FR-028, FR-052) o rechaza con motivo (FR-029, FR-045, FR-026); devuelve la sección de la mascota |
| `/?handler=FormCumplir` | GET (HTMX) | `InstanciaId` | Parcial con la hora propuesta (hora actual) para comida/paseo/medicación; para baño y visita/estudio no pide hora (FR-025, FR-027) |
| `/?handler=BajaInstancia` | POST (HTMX) | `InstanciaId` | Da de baja la instancia, y el deber si es de instancia única (FR-015, FR-016), o rechaza con motivo (FR-045) |
| `/?handler=EliminarRegistro` | POST (HTMX) | `RegistroId` | Elimina un registro con fecha de hoy (FR-030) o rechaza (FR-032) |

**Flujo de cumplimiento en ≤ 5 toques** (FR-043): 1) "Hecho" en la instancia → 2) "Confirmar"
en el parcial con la hora propuesta. Dos toques en el caso por defecto.

## Mascotas y configuración de deberes

| Ruta | Método | Entrada | Resultado |
|---|---|---|---|
| `/Mascotas/Nueva` | GET/POST | `Nombre` | Registra la mascota con el nombre sin espacios al principio ni al final (FR-007) o rechaza nombre vacío o repetido con motivo |
| `/Mascotas/{id}/Deberes` | GET | — | Configuración de deberes: deberes activos con sus datos y máximo de días entre baños (FR-008, FR-012) |
| `/Mascotas/{id}/Deberes?handler=MaxBanos` | POST | `MaxDiasEntreBanos` | Define o modifica el máximo (FR-008) |
| `/Mascotas/{id}/Deberes?handler=AltaProgramable` | POST | `Tipo`, `Modalidad`, `HoraInicio`, `IntervaloHoras?`, `Recurrencia`, `HastaDia?` | Alta y generación inmediata de las instancias de hoy (FR-009, FR-010, FR-018) |
| `/Mascotas/{id}/Deberes?handler=AltaPuntual` | POST | `Tipo`, `Subtipo?`, `Dia`, `Hora` | Alta de baño o visita/estudio y su instancia (FR-011) |
| `/Mascotas/{id}/Deberes?handler=Baja` | POST | `DeberId` | Baja definitiva del deber y de sus instancias sin cumplir (FR-013) |

## Mensajes de rechazo (FR-051)

| Situación | Mensaje |
|---|---|
| Código de hogar incorrecto o faltante | "El código de hogar es incorrecto." / "El hogar ya existe: para sumarte necesitás su código." |
| Email ya registrado (con código correcto) | "Ese email ya está registrado." |
| Contraseña corta | "La contraseña debe tener al menos 8 caracteres." |
| Login fallido | "El email o la contraseña son incorrectos." |
| Sin sesión (o sesión vencida) | "Iniciá sesión para continuar." (en la pantalla de login) |
| Nombre de mascota vacío o con solo espacios | "El nombre de la mascota no puede estar vacío." |
| Nombre de mascota repetido | "Ya existe una mascota con ese nombre." |
| N fuera de rango | "N debe ser un número entero entre 1 y 23." |
| Fecha pasada | "La fecha no puede ser anterior a hoy." |
| Visita/estudio sin subtipo | "Elegí el subtipo: veterinario, estudio, vacuna o intervención médica." |
| Hora real futura | "La hora no puede ser posterior a la actual." |
| Instancia ya cumplida / dada de baja | "Esta tarea ya fue cumplida." / "Esta tarea ya fue dada de baja." |
| Eliminar registro de otro día | "Solo se pueden eliminar registros de hoy." |
| Cumplir una instancia inexistente | "Esa tarea no existe." (FR-031) |
