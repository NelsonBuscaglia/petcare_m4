using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace PetCare.Web.Email;

/// <summary>
/// Envío real por SMTP con MailKit. Un intento que el servicio rechaza o que no responde en
/// 30 segundos lanza una excepción y cuenta como fallido (FR-041, RF-32e).
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> opciones, TimeProvider tiempo) : IEmailSender
{
    public static readonly TimeSpan TiempoMaximoPorIntento = TimeSpan.FromSeconds(30);

    public async Task EnviarAsync(IReadOnlyList<string> destinatarios, string asunto, string cuerpo, CancellationToken ct)
    {
        // Los datos son obligatorios y se validan al arrancar la app (SmtpOptions).
        var smtp = opciones.Value;
        var mensaje = new MimeMessage
        {
            Subject = asunto,
            Body = new TextPart("plain") { Text = cuerpo },
        };
        mensaje.From.Add(MailboxAddress.Parse(smtp.From!));
        foreach (var destinatario in destinatarios)
            mensaje.To.Add(MailboxAddress.Parse(destinatario));

        // Límite para el intento completo (conexión, autenticación y envío), además del de cada operación.
        using var limite = new CancellationTokenSource(TiempoMaximoPorIntento, tiempo);
        using var cancelacion = CancellationTokenSource.CreateLinkedTokenSource(ct, limite.Token);

        using var cliente = new SmtpClient { Timeout = (int)TiempoMaximoPorIntento.TotalMilliseconds };
        await cliente.ConnectAsync(smtp.Host!, smtp.Port!.Value, SecureSocketOptions.Auto, cancelacion.Token);
        await cliente.AuthenticateAsync(smtp.User!, smtp.Password!, cancelacion.Token);
        await cliente.SendAsync(mensaje, cancelacion.Token);
        await cliente.DisconnectAsync(true, cancelacion.Token);
    }
}
