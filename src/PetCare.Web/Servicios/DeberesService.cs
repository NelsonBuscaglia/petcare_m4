using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Servicios;

/// <summary>Datos del alta de un deber programable (contracts/ui-routes.md, AltaProgramable).</summary>
public sealed record AltaProgramable(
    int MascotaId,
    TipoDeber Tipo,
    Modalidad Modalidad,
    TimeOnly HoraInicio,
    int? IntervaloHoras,
    Recurrencia Recurrencia,
    DateOnly? HastaDia);

public sealed class DeberesService(PetCareDbContext db, RelojHogar reloj)
{
    /// <summary>
    /// Da de alta un deber programable y genera en el momento las instancias de hoy, incluidas
    /// las de horarios ya pasados (FR-009, FR-010, FR-018). Devuelve el id del deber.
    /// </summary>
    public async Task<Resultado<int>> AltaProgramableAsync(AltaProgramable alta)
    {
        var hoy = reloj.Hoy;

        if (alta.Modalidad == Modalidad.CadaNHoras && alta.IntervaloHoras is not (>= 1 and <= 23))
            return Resultado<int>.Rechazo(Mensajes.IntervaloFueraDeRango);

        // La fecha de fin solo existe para la medicación diaria (FR-010).
        var hastaDia = alta.Tipo == TipoDeber.Medicacion && alta.Recurrencia == Recurrencia.TodosLosDias
            ? alta.HastaDia
            : null;
        if (hastaDia < hoy) return Resultado<int>.Rechazo(Mensajes.FechaPasada);

        var deber = new Deber
        {
            MascotaId = alta.MascotaId,
            Tipo = alta.Tipo,
            Modalidad = alta.Modalidad,
            HoraInicio = alta.HoraInicio,
            IntervaloHoras = alta.Modalidad == Modalidad.CadaNHoras ? alta.IntervaloHoras : null,
            Recurrencia = alta.Recurrencia,
            HastaDia = hastaDia,
            CreadoUtc = reloj.AhoraUtc,
            CreadoDia = hoy,
        };
        db.Deberes.Add(deber);
        await InstanciasDelDia.AgregarFaltantesAsync(db, deber, hoy, reloj.AhoraUtc);

        // Deber e instancias se guardan juntos.
        await db.SaveChangesAsync();
        return Resultado<int>.Exito(deber.Id);
    }

    /// <summary>Deberes activos de la mascota, en el orden en que se dieron de alta (FR-012).</summary>
    public async Task<IReadOnlyList<Deber>> DeberesActivosAsync(int mascotaId)
    {
        var deberes = await db.Deberes.AsNoTracking()
            .Where(d => d.MascotaId == mascotaId && d.DadoDeBajaUtc == null)
            .OrderBy(d => d.Id)
            .ToListAsync();

        var ids = deberes.Select(d => d.Id).ToList();
        var vigentes = await db.Instancias.AsNoTracking()
            .Where(i => ids.Contains(i.DeberId) && i.Estado == EstadoInstancia.Vigente)
            .ToListAsync();

        var hoy = reloj.Hoy;
        return deberes
            .Where(d => ReglasDeber.EsActivo(d, vigentes.Where(i => i.DeberId == d.Id), hoy))
            .ToList();
    }
}
