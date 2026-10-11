using Microsoft.Extensions.Time.Testing;
using PetCare.Web.Email;

namespace PetCare.Tests.Infra;

/// <summary>Un email que el servicio aceptó, con el instante del reloj de prueba en que se envió.</summary>
public sealed record EmailEnviado(IReadOnlyList<string> Destinatarios, string Asunto, string Cuerpo, DateTimeOffset Momento);

/// <summary>
/// Servicio de email de prueba: registra cada envío aceptado y permite programar, según el
/// instante de cada intento, que el servicio rechace o que no responda (FR-041).
/// </summary>
public sealed class FakeEmailSender(FakeTimeProvider tiempo) : IEmailSender
{
    private readonly List<EmailEnviado> _enviados = [];
    private readonly List<DateTimeOffset> _intentos = [];

    /// <summary>Los emails aceptados, en orden.</summary>
    public IReadOnlyList<EmailEnviado> Enviados => _enviados;

    /// <summary>El instante de cada intento, aceptado o no.</summary>
    public IReadOnlyList<DateTimeOffset> Intentos => _intentos;

    /// <summary>Si devuelve true para el instante del intento, el servicio rechaza el envío.</summary>
    public Func<DateTimeOffset, bool> Rechaza { get; set; } = _ => false;

    /// <summary>
    /// Si devuelve true para el instante del intento, el servicio no responde: el intento
    /// consume 30 s del reloj de prueba y falla, como un timeout de MailKit.
    /// </summary>
    public Func<DateTimeOffset, bool> NoResponde { get; set; } = _ => false;

    public Task EnviarAsync(IReadOnlyList<string> destinatarios, string asunto, string cuerpo, CancellationToken ct)
    {
        var ahora = tiempo.GetUtcNow();
        _intentos.Add(ahora);

        if (NoResponde(ahora))
        {
            tiempo.Advance(TimeSpan.FromSeconds(30));
            throw new TimeoutException("El servicio de email no respondió en 30 segundos.");
        }

        if (Rechaza(ahora)) throw new InvalidOperationException("El servicio de email rechazó el envío.");

        _enviados.Add(new EmailEnviado(destinatarios, asunto, cuerpo, ahora));
        return Task.CompletedTask;
    }
}
