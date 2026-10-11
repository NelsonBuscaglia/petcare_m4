using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Servicios;

namespace PetCare.Web.Pages.Mascotas;

/// <summary>Configuración de deberes de una mascota (FR-008, FR-012).</summary>
public class DeberesModel(PetCareDbContext db, DeberesService deberes, MascotasService mascotas) : PageModel
{
    public Mascota Mascota { get; private set; } = null!;

    public IReadOnlyList<Deber> Activos { get; private set; } = [];

    /// <summary>Motivos de rechazo de cada formulario (FR-051).</summary>
    public string? Error { get; private set; }
    public string? ErrorPuntual { get; private set; }
    public string? ErrorMaxBanos { get; private set; }

    // Alta de un deber programable (contracts/ui-routes.md, AltaProgramable).
    [BindProperty] public TipoDeber Tipo { get; set; } = TipoDeber.Comida;
    [BindProperty] public Modalidad Modalidad { get; set; } = Modalidad.HoraFija;
    [BindProperty] public TimeOnly? HoraInicio { get; set; }
    [BindProperty] public int? IntervaloHoras { get; set; }
    [BindProperty] public Recurrencia Recurrencia { get; set; } = Recurrencia.TodosLosDias;
    [BindProperty] public DateOnly? HastaDia { get; set; }

    // Alta de un baño o visita/estudio (contracts/ui-routes.md, AltaPuntual); comparte Tipo.
    [BindProperty] public SubtipoVisita? Subtipo { get; set; }
    [BindProperty] public DateOnly? Dia { get; set; }
    [BindProperty] public TimeOnly? Hora { get; set; }

    // Máximo de días entre baños (contracts/ui-routes.md, MaxBanos): llega como texto para
    // poder rechazar con su motivo un valor que no sea entero (RF-05c).
    [BindProperty] public string? MaxDiasEntreBanos { get; set; }

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

    public async Task<IActionResult> OnPostAltaPuntualAsync(int id)
    {
        if (!await CargarAsync(id)) return NotFound();
        if (Dia is null || Hora is null || Tipo is not (TipoDeber.Bano or TipoDeber.VisitaEstudio)) return BadRequest();

        var resultado = await deberes.AltaPuntualAsync(new AltaPuntual(id, Tipo, Subtipo, Dia.Value, Hora.Value));
        if (!resultado.Ok)
        {
            ErrorPuntual = resultado.Error;
            return Page();
        }

        return Redirect($"/Mascotas/{id}/Deberes");
    }

    public async Task<IActionResult> OnPostMaxBanosAsync(int id)
    {
        if (!await CargarAsync(id)) return NotFound();

        var resultado = int.TryParse(MaxDiasEntreBanos?.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var maximo)
            ? await mascotas.DefinirMaxDiasEntreBanosAsync(id, maximo)
            : Resultado.Rechazo(Mensajes.MaxBanosInvalido);
        if (!resultado.Ok)
        {
            ErrorMaxBanos = resultado.Error;
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
