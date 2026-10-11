using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;

namespace PetCare.Web.Pages.Hogar;

/// <summary>Código de hogar para el cuidador con sesión (FR-004).</summary>
public class CodigoModel(PetCareDbContext db) : PageModel
{
    public string Codigo { get; private set; } = "";

    public async Task OnGetAsync() => Codigo = (await db.Hogares.AsNoTracking().SingleAsync()).Codigo;
}
