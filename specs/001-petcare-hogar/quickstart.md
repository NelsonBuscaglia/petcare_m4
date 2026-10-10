# Quickstart: validar PetCare Hogar

**Parte de** [plan.md](./plan.md). Guía para comprobar de punta a punta que la feature
funciona. Los detalles de datos están en [data-model.md](./data-model.md) y los de pantallas y
emails en [contracts/](./contracts/).

## Requisitos

- .NET SDK 10 (`dotnet --list-sdks` muestra `10.x`).
- Sin credenciales SMTP: en desarrollo los emails se escriben en la consola.

## Preparar y correr

```bash
dotnet tool restore                                     # instala dotnet-ef
dotnet restore
dotnet ef database update --project src/PetCare.Web     # crea o actualiza petcare.db
dotnet run --project src/PetCare.Web                    # app + procesador programado
```

## Tests

```bash
dotnet test                                                  # toda la suite
dotnet test --filter "FullyQualifiedName~AC_29b"             # un criterio de aceptación
```

Los tests llevan el identificador del criterio del PRD en su nombre (por ejemplo,
`AC_29b_medicacion_vencida_a_los_15_minutos`), así que cada AC se puede correr por separado.
Resultado esperado: todos en verde, sin depender de la hora real (usan `FakeTimeProvider`).

## Validación manual (navegador del celular o vista móvil de 360 px)

1. **Hogar** (US1): registrarse sin código → se muestra un código de 8 caracteres. En otra
   ventana privada, registrarse con ese código en minúsculas → entra al mismo hogar.
2. **Estado vacío** (FR-053): sin mascotas, la pantalla principal lo indica y ofrece registrar
   una.
3. **Deberes** (US2): registrar "Firulais" y darle un "paseo cada 4 horas desde las 08:00,
   todos los días" → el estado del día muestra 08:00, 12:00, 16:00 y 20:00.
4. **Cumplimiento** (US3): tocar "Hecho" y "Confirmar" en una instancia → desaparece y "paseos
   de hoy" sube en 1. En la otra ventana, sin recargar, el cambio aparece en ≤ 15 s (US7).
5. **Vencimiento y email** (US4, US8): dar de alta "medicación, hora fija" para dentro de
   2 minutos → a los 15 minutos de esa hora aparece "pendiente" sin recargar, y en la consola de
   la app se escribe el email de vencimiento con la mascota, el tipo y el horario.
6. **Acceso** (FR-006): cerrar sesión y abrir `/` → redirige al login sin mostrar datos.

## Comprobaciones de la constitución

- `git grep -nE "DateTime\.(Now|UtcNow)" -- src` no devuelve resultados (Principio I y
  `AGENTS.md`).
- `git grep -niE "password|smtp" -- "*.json"` no muestra credenciales (Principio II).
- No hay `EnsureCreated()` ni `Migrate()` en `src/` (`AGENTS.md`).
