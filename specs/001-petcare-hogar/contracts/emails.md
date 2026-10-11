# Contrato de emails

**Parte de** [plan.md](../plan.md). Destinatarios: todos los cuidadores del hogar al momento
del envío. Idioma: español. Formato: texto plano. Horas en hora del hogar (UTC−3), formato
`dd/MM/yyyy HH:mm`.

## Vencimiento (FR-036, FR-039, FR-054)

- **Asunto**: `PetCare: {Mascota} – {Tipo} vencido` o `vencida`, según el género del tipo: "vencida"
  para comida, medicación y visita/estudio; "vencido" para paseo y baño.
- **Cuerpo, campos obligatorios**: nombre de la mascota; tipo de deber (y subtipo si es
  visita/estudio); fecha y hora de la instancia.
- **Disparo**: la instancia pasa a vencida (R7); nunca más de una `Aceptada` por instancia.

Ejemplo:

```text
Asunto: PetCare: Firulais – Medicación vencida
Firulais tiene una medicación vencida, programada para el 10/10/2026 09:00.
```

## Recordatorio (FR-038, FR-039, FR-054)

- **Asunto**: `PetCare: {Mascota} – {Tipo} en 48 horas`
- **Cuerpo**: nombre de la mascota; tipo de deber (y subtipo); fecha y hora agendadas.
- **Disparo**: 48 h antes del turno, si el deber se dio de alta con ≥ 48 h de anticipación y
  sigue sin baja. Uno por deber.

## Aviso de turno de baño (FR-040, FR-054)

- **Asunto**: `PetCare: hay que pedir turno de baño para {Mascota}`
- **Cuerpo**: nombre de la mascota; máximo de días entre baños; fecha del último baño
  registrado o "no hay baños registrados".
- **Disparo**: 07:00 de cada día, por mascota que cumpla las tres condiciones de FR-040. Uno
  por mascota y día.

## Envío (FR-041)

| Aspecto | Contrato |
|---|---|
| Interfaz | `IEmailSender.EnviarAsync(destinatarios, asunto, cuerpo, ct)`; lanza excepción si el servicio rechaza |
| Desarrollo | `ConsoleEmailSender`: escribe el email en la consola; nunca envía (AGENTS.md) |
| Producción | `SmtpEmailSender` (MailKit), timeout de 30 s por intento |
| Configuración | Variables de entorno `Smtp__Host`, `Smtp__Port`, `Smtp__User`, `Smtp__Password`, `Smtp__From`, o user-secrets en local; nunca en `appsettings*.json` |
| Ventana | Reintentos hasta `CondicionUtc + 5 min`; después, `Perdida` |
