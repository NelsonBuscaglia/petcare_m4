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

        var secciones = mascotas
            .Select(m => new SeccionMascota(
                m.Id,
                m.Nombre,
                instancias
                    .Where(i => i.MascotaId == m.Id)
                    .OrderBy(i => i.Dia).ThenBy(i => i.Hora)
                    .Select(i => new InstanciaVista(i.Id, i.Tipo, i.Dia, i.Hora))
                    .ToList()))
            .ToList();

        return new EstadoDelDia(secciones);
    }
}
