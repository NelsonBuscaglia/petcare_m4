using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;

namespace PetCare.Web.Servicios;

/// <summary>Arma el estado del día del hogar, que ven todos los cuidadores por igual.</summary>
public sealed class EstadoDelDiaQuery(PetCareDbContext db, RelojHogar reloj)
{
    public async Task<EstadoDelDia> ObtenerAsync()
    {
        var hoy = reloj.Hoy;

        var mascotas = await db.Mascotas.AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new { m.Id, m.Nombre })
            .ToListAsync();

        // Instancias vigentes del día, de deberes no dados de baja (FR-033).
        var instancias = await db.Instancias.AsNoTracking()
            .Where(i => i.Estado == EstadoInstancia.Vigente && i.Dia == hoy && i.Deber!.DadoDeBajaUtc == null)
            .Select(i => new { i.Id, i.Deber!.MascotaId, i.Deber.Tipo, i.Dia, i.Hora })
            .ToListAsync();

        // Registros de hoy, aunque su deber se haya dado de baja (FR-013, AC-25).
        var registrosDeHoy = await db.RegistrosCumplimiento.AsNoTracking()
            .Where(r => r.Dia == hoy)
            .Select(r => new { r.Id, r.Instancia!.Deber!.MascotaId, r.Instancia.Deber.Tipo, r.Hora })
            .ToListAsync();

        var secciones = new List<SeccionMascota>();
        foreach (var mascota in mascotas)
        {
            var deHoy = registrosDeHoy.Where(r => r.MascotaId == mascota.Id).ToList();
            var resumen = new ResumenMascota(
                deHoy.Count(r => r.Tipo == TipoDeber.Paseo),
                await UltimoCumplimientoAsync(mascota.Id, TipoDeber.Comida),
                await UltimoCumplimientoAsync(mascota.Id, TipoDeber.Paseo));

            secciones.Add(new SeccionMascota(
                mascota.Id,
                mascota.Nombre,
                instancias
                    .Where(i => i.MascotaId == mascota.Id)
                    .OrderBy(i => i.Dia).ThenBy(i => i.Hora)
                    .Select(i => new InstanciaVista(i.Id, i.Tipo, i.Dia, i.Hora))
                    .ToList(),
                resumen,
                deHoy
                    .OrderBy(r => r.Hora).ThenBy(r => r.Id)
                    .Select(r => new RegistroVista(r.Id, r.Tipo, r.Hora))
                    .ToList()));
        }

        return new EstadoDelDia(secciones);
    }

    /// <summary>El registro de mayor día y hora del tipo, aunque sea de un día anterior (FR-034).</summary>
    private Task<Momento?> UltimoCumplimientoAsync(int mascotaId, TipoDeber tipo) =>
        db.RegistrosCumplimiento.AsNoTracking()
            .Where(r => r.Instancia!.Deber!.MascotaId == mascotaId && r.Instancia.Deber.Tipo == tipo)
            .OrderByDescending(r => r.Dia).ThenByDescending(r => r.Hora)
            .Select(r => new Momento(r.Dia, r.Hora))
            .FirstOrDefaultAsync();
}
