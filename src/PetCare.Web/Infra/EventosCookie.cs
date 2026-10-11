using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace PetCare.Web.Infra;

/// <summary>
/// Renovación de la sesión y respuesta a las solicitudes sin sesión
/// (contracts/ui-routes.md; FR-005, FR-006, FR-051; R9).
/// </summary>
public sealed class EventosCookie : CookieAuthenticationEvents
{
    public const string LoginSinSesion = "/Cuenta/Login?motivo=sin-sesion";

    /// <summary>Cada solicitud renueva el plazo de 30 días: cuenta desde la última solicitud (RF-02c).</summary>
    public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        context.ShouldRenew = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sin sesión: la navegación normal va al login con el motivo; una solicitud HTMX recibe 401
    /// sin cuerpo y <c>HX-Redirect</c>, para que HTMX lleve al login en vez de insertar la página.
    /// </summary>
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Headers["HX-Request"] == "true")
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["HX-Redirect"] = LoginSinSesion;
        }
        else
        {
            context.Response.Redirect(LoginSinSesion);
        }

        return Task.CompletedTask;
    }
}
