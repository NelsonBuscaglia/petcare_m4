using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Servicios;

/// <summary>
/// Agrega las instancias que le faltan a un deber programable en un día. Es idempotente: no
/// repite las que ya existen (R4). Lo usan el alta de deberes y el procesador programado.
/// </summary>
public static class InstanciasDelDia
{
    /// <summary>Tolerancia antes de que una comida, paseo o medicación quede vencida (FR-019).</summary>
    private static readonly TimeSpan Tolerancia = TimeSpan.FromMinutes(15);

    /// <summary>Agrega al contexto, sin guardar, las instancias faltantes del deber en el día.</summary>
    public static async Task AgregarFaltantesAsync(PetCareDbContext db, Deber deber, DateOnly dia, DateTime ahoraUtc)
    {
        var aGenerar = GeneradorInstancias.Generar(deber, dia);
        if (aGenerar.Count == 0) return;

        var existentes = deber.Id == 0
            ? []
            : await db.Instancias.Where(i => i.DeberId == deber.Id && i.Dia == dia).Select(i => i.ProgramadaUtc).ToListAsync();

        foreach (var (diaInstancia, hora) in aGenerar)
        {
            var programadaUtc = RelojHogar.AUtc(diaInstancia, hora);
            if (existentes.Contains(programadaUtc)) continue;

            db.Instancias.Add(new Instancia
            {
                Deber = deber,
                Dia = diaInstancia,
                Hora = hora,
                ProgramadaUtc = programadaUtc,
                Estado = EstadoInstancia.Vigente,
                GeneradaUtc = ahoraUtc,
                // Ya vencida al generarse: no dispara email de vencimiento (FR-018, FR-037).
                GeneradaVencida = ahoraUtc >= programadaUtc + Tolerancia,
            });
        }
    }
}
