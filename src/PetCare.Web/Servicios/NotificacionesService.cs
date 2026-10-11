using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Email;

namespace PetCare.Web.Servicios;

/// <summary>
/// Outbox de emails (R7): cada email es una <see cref="Notificacion"/> con una clave única que
/// evita duplicados. Primero se crean las notificaciones cuya condición se cumplió; después se
/// intentan enviar dentro de la ventana de 5 minutos.
/// </summary>
public sealed class NotificacionesService(
    PetCareDbContext db,
    RelojHogar reloj,
    IEmailSender emails,
    ILogger<NotificacionesService> logger)
{
    /// <summary>Plazo desde la condición para entregar el email al servicio de envío (FR-041, RNF-01).</summary>
    public static readonly TimeSpan Ventana = TimeSpan.FromMinutes(5);

    /// <summary>Crea las notificaciones nuevas de vencimiento y de recordatorio.</summary>
    public async Task CrearAsync(CancellationToken ct)
    {
        await CrearVencimientosAsync(ct);
        await CrearRecordatoriosAsync(ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Una notificación por instancia vencida y episodio (FR-036, FR-037): no si ya hubo una
    /// aceptada para la instancia, ni en el episodio 0 de una instancia que nació vencida. La
    /// condición es el vencimiento o, si se eliminó un cumplimiento después, ese momento.
    /// </summary>
    private async Task CrearVencimientosAsync(CancellationToken ct)
    {
        var ahora = reloj.AhoraUtc;
        var hoy = reloj.Hoy;
        var vigentes = await db.Instancias.AsNoTracking()
            .Include(i => i.Deber)
            .Where(i => i.Estado == EstadoInstancia.Vigente && i.Dia <= hoy && i.Deber!.DadoDeBajaUtc == null)
            .ToListAsync(ct);
        var vencidas = vigentes
            .Where(i => ReglasVencimiento.EstaVencida(i, i.Deber!.Tipo, ahora) && !ReglasVencimiento.EstaExpirada(i, i.Deber.Tipo, hoy))
            .Where(i => !(i.GeneradaVencida && i.EpisodioVencimiento == 0))
            .ToList();
        if (vencidas.Count == 0) return;

        var ids = vencidas.Select(i => i.Id).ToList();
        var existentes = await db.Notificaciones.AsNoTracking()
            .Where(n => n.Tipo == TipoNotificacion.Vencimiento && ids.Contains(n.InstanciaId!.Value))
            .Select(n => new { n.InstanciaId, n.Clave, n.Estado })
            .ToListAsync(ct);

        foreach (var instancia in vencidas)
        {
            var clave = $"V:{instancia.Id}:{instancia.EpisodioVencimiento}";
            var yaAceptada = existentes.Any(n => n.InstanciaId == instancia.Id && n.Estado == EstadoNotificacion.Aceptada);
            if (yaAceptada || existentes.Any(n => n.Clave == clave)) continue;

            var vencimiento = MomentoDeVencimiento(instancia);
            var condicion = instancia.ReactivadaUtc is { } reactivada && reactivada > vencimiento ? reactivada : vencimiento;
            Agregar(TipoNotificacion.Vencimiento, clave, condicion, ahora, instanciaId: instancia.Id);
        }
    }

    /// <summary>
    /// Un recordatorio 48 horas antes de un baño o visita/estudio sin baja, si se dio de alta con
    /// al menos 48 horas de anticipación (FR-038).
    /// </summary>
    private async Task CrearRecordatoriosAsync(CancellationToken ct)
    {
        var ahora = reloj.AhoraUtc;
        var hoy = reloj.Hoy;
        var puntuales = await db.Deberes.AsNoTracking()
            .Where(d => d.DadoDeBajaUtc == null
                && (d.Tipo == TipoDeber.Bano || d.Tipo == TipoDeber.VisitaEstudio)
                && d.Dia >= hoy)
            .ToListAsync(ct);

        foreach (var deber in puntuales)
        {
            var condicion = RelojHogar.AUtc(deber.Dia!.Value, deber.Hora!.Value) - TimeSpan.FromHours(48);
            if (deber.CreadoUtc > condicion || ahora < condicion) continue;

            var clave = $"R:{deber.Id}";
            if (await db.Notificaciones.AnyAsync(n => n.Clave == clave, ct)) continue;
            Agregar(TipoNotificacion.Recordatorio, clave, condicion, ahora, deberId: deber.Id);
        }
    }

    /// <summary>
    /// Agrega la notificación; si la condición se detecta cuando ya pasó la ventana (servidor
    /// caído), nace perdida y no se envía (Fuera de Alcance del PRD).
    /// </summary>
    private void Agregar(TipoNotificacion tipo, string clave, DateTime condicionUtc, DateTime ahora,
        int? instanciaId = null, int? deberId = null, int? mascotaId = null)
    {
        db.Notificaciones.Add(new Notificacion
        {
            Tipo = tipo,
            Clave = clave,
            InstanciaId = instanciaId,
            DeberId = deberId,
            MascotaId = mascotaId,
            CondicionUtc = condicionUtc,
            Estado = ahora <= condicionUtc + Ventana ? EstadoNotificacion.Pendiente : EstadoNotificacion.Perdida,
        });
    }

    /// <summary>Comida, paseo y medicación vencen a los 15 minutos; baño y visita/estudio, a su hora (FR-019, FR-023).</summary>
    private static DateTime MomentoDeVencimiento(Instancia instancia) =>
        instancia.Deber!.EsProgramable ? instancia.ProgramadaUtc + ReglasVencimiento.Tolerancia : instancia.ProgramadaUtc;
}
