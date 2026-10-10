# AGENTS.md

## Propósito
Web app mobile-first para que los integrantes de un hogar vean y registren el cuidado de sus mascotas
(paseo, comida, medicación, baño, veterinario), con alarmas por email ante tareas olvidadas o vencidas.

## Stack
- .NET 10 LTS, C#, ASP.NET Core Razor Pages (sin Blazor).
- HTMX
- SQLite + EF Core 10
- Autenticación por cookie + `PasswordHasher<T>`.
- MailKit
- xUnit

## Cómo correr
```bash
dotnet tool restore                                     # instala dotnet-ef
dotnet restore
dotnet ef database update --project src/PetCare.Web     # crea o actualiza la base SQLite
dotnet run --project src/PetCare.Web                    # levanta la app y el scheduler
dotnet test                                             # todos los tests
dotnet test --filter "FullyQualifiedName~NombreDelTest" # un solo test
```

## Qué NO hacer
- No usar `DateTime.Now`/`UtcNow`: toda hora sale del `TimeProvider` inyectado, o no se pueden testear vencimientos y cambios de día.
- No tocar la base por fuera de las migraciones: nada de `EnsureCreated()`, `Migrate()` al arrancar ni borrar `petcare.db`. Los cambios de esquema van con `dotnet ef migrations add`.
- No mandar emails reales en desarrollo (se escriben en la consola) ni poner credenciales SMTP en `appsettings*.json`.
