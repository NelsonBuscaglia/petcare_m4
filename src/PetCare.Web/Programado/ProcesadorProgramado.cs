using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Servicios;

namespace PetCare.Web.Programado;

/// <summary>
/// Un ciclo del proceso en segundo plano (R5). Se ejecuta cada 30 s sin depender de usuarios
/// conectados; los tests lo llaman directamente con un reloj controlado.
/// </summary>
public sealed class ProcesadorProgramado(
    PetCareDbContext db,
    RelojHogar reloj,
    NotificacionesService notificaciones,
    ILogger<ProcesadorProgramado> logger)
{
    public async Task EjecutarCicloAsync(CancellationToken ct)
    {
        await GenerarInstanciasDelDiaAsync(ct);

        // Paso 2: notificaciones nuevas cuya condición se cumplió (FR-036, FR-038).
        await notificaciones.CrearAsync(ct);

        // Paso 3: envío de las pendientes, con reintentos dentro de la ventana (FR-041).
        await notificaciones.EnviarPendientesAsync(ct);
    }

    /// <summary>
    /// Paso 1: genera las instancias de hoy que falten de los deberes diarios activos (FR-017,
    /// FR-020, R4). Es idempotente: tras un reinicio completa lo que no se generó.
    /// </summary>
    private async Task GenerarInstanciasDelDiaAsync(CancellationToken ct)
    {
        var hoy = reloj.Hoy;
        var deberesDiarios = await db.Deberes
            .Where(d => d.DadoDeBajaUtc == null
                && d.Recurrencia == Recurrencia.TodosLosDias
                && d.CreadoDia <= hoy
                && (d.HastaDia == null || d.HastaDia >= hoy))
            .ToListAsync(ct);

        foreach (var deber in deberesDiarios)
            await InstanciasDelDia.AgregarFaltantesAsync(db, deber, hoy, reloj.AhoraUtc);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Un alta simultánea ya generó alguna de estas instancias; el próximo ciclo completa el resto.
            logger.LogWarning(ex, "No se pudieron guardar las instancias del {Dia}; se reintenta en el próximo ciclo.", hoy);
            db.ChangeTracker.Clear();
        }
    }
}
