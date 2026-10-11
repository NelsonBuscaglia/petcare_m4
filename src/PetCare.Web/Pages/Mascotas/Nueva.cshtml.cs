using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PetCare.Web.Servicios;

namespace PetCare.Web.Pages.Mascotas;

/// <summary>Registro de una mascota del hogar (FR-007).</summary>
public class NuevaModel(MascotasService mascotas) : PageModel
{
    [BindProperty] public string? Nombre { get; set; }

    /// <summary>Motivo del rechazo (FR-051).</summary>
    public string? Error { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await mascotas.RegistrarAsync(Nombre ?? "");
        if (!resultado.Ok)
        {
            Error = resultado.Error;
            return Page();
        }

        return Redirect("/");
    }
}
