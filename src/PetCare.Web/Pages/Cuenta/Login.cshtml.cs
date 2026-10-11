using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Infra;
using PetCare.Web.Servicios;

namespace PetCare.Web.Pages.Cuenta;

public class LoginModel(PetCareDbContext db) : PageModel
{
    [BindProperty] public string? Email { get; set; }

    [BindProperty] public string? Password { get; set; }

    public string? Error { get; private set; }

    /// <summary>Con <c>motivo=sin-sesion</c> se informa por qué se pide iniciar sesión (FR-051).</summary>
    public void OnGet(string? motivo)
    {
        if (motivo == "sin-sesion") Error = Mensajes.SinSesion;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var emailNormalizado = RegistroService.Normalizar(Email ?? "");
        var cuidador = await db.Cuidadores.AsNoTracking()
            .SingleOrDefaultAsync(c => c.EmailNormalizado == emailNormalizado);

        if (cuidador is null || !RegistroService.VerificarPassword(cuidador, Password ?? ""))
        {
            // El mismo mensaje para email inexistente y contraseña incorrecta (FR-050).
            Error = Mensajes.LoginFallido;
            return Page();
        }

        await Sesion.IniciarAsync(HttpContext, cuidador);
        return Redirect("/");
    }
}
