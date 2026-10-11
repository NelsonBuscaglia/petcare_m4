namespace PetCare.Web.Email;

/// <summary>Para desarrollo: escribe el email en la consola y nunca lo envía (AGENTS.md, Principio II).</summary>
public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task EnviarAsync(IReadOnlyList<string> destinatarios, string asunto, string cuerpo, CancellationToken ct)
    {
        logger.LogInformation(
            "Email (no se envía en desarrollo)\nPara: {Destinatarios}\nAsunto: {Asunto}\n{Cuerpo}",
            string.Join(", ", destinatarios), asunto, cuerpo);
        return Task.CompletedTask;
    }
}
