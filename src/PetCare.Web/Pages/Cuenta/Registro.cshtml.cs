using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PetCare.Web.Infra;
using PetCare.Web.Servicios;

namespace PetCare.Web.Pages.Cuenta;

public class RegistroModel(RegistroService registro) : PageModel
{
    [BindProperty] public string? Email { get; set; }

    [BindProperty] public string? Password { get; set; }

    [BindProperty] public string? CodigoHogar { get; set; }

    /// <summary>Motivo del rechazo (FR-051).</summary>
    public string? Error { get; private set; }

    /// <summary>Código del hogar recién creado, que se le muestra a la primera persona (FR-001).</summary>
    public string? CodigoCreado { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await registro.RegistrarAsync(Email ?? "", Password ?? "", CodigoHogar);
        if (!resultado.Ok)
        {
            Error = resultado.Error;
            return Page();
        }

        await Sesion.IniciarAsync(HttpContext, resultado.Cuidador!);

        if (resultado.CodigoCreado is null) return Redirect("/");

        CodigoCreado = resultado.CodigoCreado;
        return Page();
    }
}
