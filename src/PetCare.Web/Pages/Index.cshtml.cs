using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Servicios;

namespace PetCare.Web.Pages;

/// <summary>
/// Estado del día: la pantalla que se muestra al iniciar sesión (FR-033), con los handlers HTMX
/// que devuelven la sección de una mascota (contracts/ui-routes.md).
/// </summary>
public class IndexModel(EstadoDelDiaQuery consulta, CumplimientosService cumplimientos, PetCareDbContext db, RelojHogar reloj)
    : PageModel
{
    public EstadoDelDia Estado { get; private set; } = new([]);

    public async Task OnGetAsync() => Estado = await consulta.ObtenerAsync();

    /// <summary>Primer toque: el formulario con la hora propuesta (FR-025, FR-027, FR-043).</summary>
    public async Task<IActionResult> OnGetFormCumplirAsync(int instanciaId)
    {
        var instancia = await db.Instancias.AsNoTracking()
            .Where(i => i.Id == instanciaId)
            .Select(i => new { i.Id, i.Deber!.MascotaId, i.Deber.Tipo, i.Hora })
            .SingleOrDefaultAsync();
        if (instancia is null) return NotFound();

        var ahora = reloj.HoraActual;
        var esProgramable = instancia.Tipo is TipoDeber.Comida or TipoDeber.Paseo or TipoDeber.Medicacion;
        return Partial("_FormCumplir", new FormCumplirVista(
            instancia.Id, instancia.MascotaId, instancia.Tipo, instancia.Hora,
            esProgramable ? new TimeOnly(ahora.Hour, ahora.Minute) : null));
    }

    /// <summary>Segundo toque: registra el cumplimiento y devuelve la sección de la mascota (FR-024).</summary>
    public async Task<IActionResult> OnPostCumplirAsync(int instanciaId, TimeOnly? hora)
    {
        var mascotaId = await db.Instancias.Where(i => i.Id == instanciaId).Select(i => (int?)i.Deber!.MascotaId).SingleOrDefaultAsync();
        var resultado = await cumplimientos.CumplirAsync(instanciaId, hora);
        if (mascotaId is null) return MensajeGeneral(resultado.Error!);

        return await SeccionAsync(mascotaId.Value, resultado.Error);
    }

    /// <summary>Elimina un registro de hoy y devuelve la sección de la mascota (FR-030, FR-032).</summary>
    public async Task<IActionResult> OnPostEliminarRegistroAsync(int registroId)
    {
        var mascotaId = await db.RegistrosCumplimiento.Where(r => r.Id == registroId)
            .Select(r => (int?)r.Instancia!.Deber!.MascotaId).SingleOrDefaultAsync();
        if (mascotaId is null)
        {
            // Otro cuidador ya lo eliminó: se recarga la pantalla para mostrar el estado real.
            Response.Headers["HX-Refresh"] = "true";
            return new EmptyResult();
        }

        var resultado = await cumplimientos.EliminarAsync(registroId);
        return await SeccionAsync(mascotaId.Value, resultado.Error);
    }

    private async Task<IActionResult> SeccionAsync(int mascotaId, string? error)
    {
        var seccion = (await consulta.ObtenerAsync()).Mascotas.Single(m => m.MascotaId == mascotaId);
        return Partial("_SeccionMascota", new SeccionMascotaVista(seccion, error));
    }

    /// <summary>Un rechazo que no corresponde a ninguna mascota se muestra arriba del estado del día (FR-051).</summary>
    private IActionResult MensajeGeneral(string error)
    {
        Response.Headers["HX-Retarget"] = "#mensajes";
        Response.Headers["HX-Reswap"] = "innerHTML";
        return Content($"<p class=\"error\">{System.Net.WebUtility.HtmlEncode(error)}</p>", "text/html");
    }
}
