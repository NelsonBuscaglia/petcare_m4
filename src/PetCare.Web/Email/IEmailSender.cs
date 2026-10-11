namespace PetCare.Web.Email;

/// <summary>Servicio de envío de emails (contracts/emails.md).</summary>
public interface IEmailSender
{
    /// <summary>
    /// Entrega el email al servicio de envío. Lanza una excepción si el servicio lo rechaza o no
    /// responde: solo cuenta como enviado si no hubo excepción (FR-041).
    /// </summary>
    Task EnviarAsync(IReadOnlyList<string> destinatarios, string asunto, string cuerpo, CancellationToken ct);
}
