using System.ComponentModel.DataAnnotations;

namespace PetCare.Web.Email;

/// <summary>
/// Configuración del servicio SMTP. Se lee de variables de entorno (<c>Smtp__Host</c>, …) o de
/// user-secrets, nunca de <c>appsettings*.json</c>; si falta algún dato, la app no arranca y lo
/// informa (Principio II, R7).
/// </summary>
public sealed class SmtpOptions
{
    public const string Seccion = "Smtp";

    [Required] public string? Host { get; set; }

    [Required] public int? Port { get; set; }

    [Required] public string? User { get; set; }

    [Required] public string? Password { get; set; }

    [Required] public string? From { get; set; }
}
