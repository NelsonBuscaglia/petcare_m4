using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

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

/// <summary>Al arrancar, informa en español qué datos SMTP faltan (Principio II).</summary>
public sealed class ValidacionSmtpOptions : IValidateOptions<SmtpOptions>
{
    public ValidateOptionsResult Validate(string? name, SmtpOptions opciones)
    {
        var faltan = new List<string>();
        if (string.IsNullOrWhiteSpace(opciones.Host)) faltan.Add("Smtp__Host");
        if (opciones.Port is null) faltan.Add("Smtp__Port");
        if (string.IsNullOrWhiteSpace(opciones.User)) faltan.Add("Smtp__User");
        if (string.IsNullOrWhiteSpace(opciones.Password)) faltan.Add("Smtp__Password");
        if (string.IsNullOrWhiteSpace(opciones.From)) faltan.Add("Smtp__From");

        return faltan.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"Falta la configuración SMTP: {string.Join(", ", faltan)}. " +
                "Definila con variables de entorno o con user-secrets; nunca en appsettings*.json.");
    }
}
