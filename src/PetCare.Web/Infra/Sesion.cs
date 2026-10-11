using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Infra;

public static class Sesion
{
    /// <summary>
    /// Inicia la sesión del cuidador con una cookie persistente, que sobrevive al cierre del
    /// navegador durante los 30 días (FR-005, R9).
    /// </summary>
    public static Task IniciarAsync(HttpContext http, Cuidador cuidador)
    {
        var identidad = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, cuidador.Id.ToString()),
                new Claim(ClaimTypes.Name, cuidador.Email),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = true });
    }
}
