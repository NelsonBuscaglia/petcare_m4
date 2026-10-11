using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Servicios;

namespace PetCare.Web.Pages.Mascotas;

/// <summary>Configuración de deberes de una mascota (FR-012).</summary>
public class DeberesModel(PetCareDbContext db, DeberesService deberes) : PageModel
{
    public Mascota Mascota { get; private set; } = null!;

    public IReadOnlyList<Deber> Activos { get; private set; } = [];

    /// <summary>Motivo del rechazo del alta (FR-051).</summary>
    public string? Error { get; private set; }

    // Alta de un deber programable (contracts/ui-routes.md, AltaProgramable).
    [BindProperty] public TipoDeber Tipo { get; set; } = TipoDeber.Comida;
    [BindProperty] public Modalidad Modalidad { get; set; } = Modalidad.HoraFija;
    [BindProperty] public TimeOnly? HoraInicio { get; set; }
    [BindProperty] public int? IntervaloHoras { get; set; }
    [BindProperty] public Recurrencia Recurrencia { get; set; } = Recurrencia.TodosLosDias;
    [BindProperty] public DateOnly? HastaDia { get; set; }

    public async Task<IActionResult> OnGetAsync(int id) => await CargarAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAltaProgramableAsync(int id)
    {
        if (!await CargarAsync(id)) return NotFound();
        if (HoraInicio is null) return BadRequest();

        var resultado = await deberes.AltaProgramableAsync(
            new AltaProgramable(id, Tipo, Modalidad, HoraInicio.Value, IntervaloHoras, Recurrencia, HastaDia));
        if (!resultado.Ok)
        {
            Error = resultado.Error;
            return Page();
        }

        return Redirect($"/Mascotas/{id}/Deberes");
    }

    private async Task<bool> CargarAsync(int id)
    {
        var mascota = await db.Mascotas.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);
        if (mascota is null) return false;

        Mascota = mascota;
        Activos = await deberes.DeberesActivosAsync(id);
        return true;
    }
}
