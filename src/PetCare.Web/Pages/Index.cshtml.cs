using Microsoft.AspNetCore.Mvc.RazorPages;
using PetCare.Web.Servicios;

namespace PetCare.Web.Pages;

/// <summary>Estado del día: la pantalla que se muestra al iniciar sesión (FR-033).</summary>
public class IndexModel(EstadoDelDiaQuery consulta) : PageModel
{
    public EstadoDelDia Estado { get; private set; } = new([]);

    public async Task OnGetAsync() => Estado = await consulta.ObtenerAsync();
}
