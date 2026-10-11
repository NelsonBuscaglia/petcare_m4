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

    /// <summary>Hora del hogar del aviso diario de turno de baño (FR-040).</summary>
    public static readonly TimeOnly HoraAvisoTurnoBano = new(7, 0);

    /// <summary>Crea las notificaciones nuevas de vencimiento, de recordatorio y de aviso de turno de baño.</summary>
    public async Task CrearAsync(CancellationToken ct)
    {
        await CrearVencimientosAsync(ct);
        await CrearRecordatoriosAsync(ct);
        await CrearAvisosTurnoBanoAsync(ct);
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
    /// Desde las 07:00, un aviso por día para cada mascota que cumple las tres condiciones de
    /// FR-040. Si se detecta después de las 07:05 (servidor caído, o un baño activo que se dio de
    /// baja más tarde), nace perdida y el aviso queda para el día siguiente.
    /// </summary>
    private async Task CrearAvisosTurnoBanoAsync(CancellationToken ct)
    {
        var ahora = reloj.AhoraUtc;
        var hoy = reloj.Hoy;
        var condicion = RelojHogar.AUtc(hoy, HoraAvisoTurnoBano);
        if (ahora < condicion) return;

        var conMaximo = await db.Mascotas.AsNoTracking()
            .Where(m => m.MaxDiasEntreBanos != null)
            .Select(m => m.Id)
            .ToListAsync(ct);

        foreach (var mascotaId in conMaximo)
        {
            var clave = $"A:{mascotaId}:{hoy:yyyy-MM-dd}";
            if (await db.Notificaciones.AnyAsync(n => n.Clave == clave, ct)) continue;
            if (await EvaluarAvisoAsync(mascotaId, hoy, ct) is null) continue;
            Agregar(TipoNotificacion.AvisoTurnoBano, clave, condicion, ahora, mascotaId: mascotaId);
        }
    }

    /// <summary>
    /// Si corresponde el aviso de turno de baño de la mascota (FR-040), devuelve su contenido;
    /// si no, null. Un baño activo es un deber de baño activo según <see cref="ReglasDeber"/>:
    /// futuro, de hoy o "no cumplido".
    /// </summary>
    private async Task<ContenidoEmail?> EvaluarAvisoAsync(int mascotaId, DateOnly hoy, CancellationToken ct)
    {
        var mascota = await db.Mascotas.AsNoTracking().SingleAsync(m => m.Id == mascotaId, ct);

        var banos = await db.Deberes.AsNoTracking()
            .Where(d => d.MascotaId == mascotaId && d.Tipo == TipoDeber.Bano && d.DadoDeBajaUtc == null)
            .ToListAsync(ct);
        var idsBanos = banos.Select(d => d.Id).ToList();
        var vigentes = await db.Instancias.AsNoTracking()
            .Where(i => idsBanos.Contains(i.DeberId) && i.Estado == EstadoInstancia.Vigente)
            .ToListAsync(ct);
        var tieneBanoActivo = banos.Any(d => ReglasDeber.EsActivo(d, vigentes.Where(i => i.DeberId == d.Id), hoy));

        // Último baño registrado: el mayor día de los registros de baño de la mascota (data-model).
        var ultimoBano = await db.RegistrosCumplimiento.AsNoTracking()
            .Where(r => r.Instancia!.Deber!.MascotaId == mascotaId && r.Instancia.Deber.Tipo == TipoDeber.Bano)
            .OrderByDescending(r => r.Dia)
            .Select(r => (DateOnly?)r.Dia)
            .FirstOrDefaultAsync(ct);

        return AvisoTurnoBano.Corresponde(mascota, ultimoBano, tieneBanoActivo, hoy)
            ? PlantillasEmail.AvisoTurnoBano(mascota.Nombre, mascota.MaxDiasEntreBanos!.Value, ultimoBano)
            : null;
    }

    /// <summary>
    /// Intenta enviar cada notificación pendiente a todos los cuidadores del hogar. Solo cuenta
    /// como enviada si el servicio la acepta; si falla, se reintenta en el próximo ciclo mientras
    /// dure la ventana de 5 minutos desde la condición, y al vencer queda perdida (FR-041).
    /// </summary>
    public async Task EnviarPendientesAsync(CancellationToken ct)
    {
        var pendientes = await db.Notificaciones
            .Where(n => n.Estado == EstadoNotificacion.Pendiente)
            .OrderBy(n => n.CondicionUtc)
            .ToListAsync(ct);
        if (pendientes.Count == 0) return;

        var destinatarios = await db.Cuidadores.AsNoTracking().OrderBy(c => c.Id).Select(c => c.Email).ToListAsync(ct);

        foreach (var notificacion in pendientes)
        {
            // La hora se relee en cada vuelta: un intento sin respuesta consume tiempo.
            var ahora = reloj.AhoraUtc;
            if (ahora > notificacion.CondicionUtc + Ventana)
            {
                notificacion.Estado = EstadoNotificacion.Perdida;
                await db.SaveChangesAsync(ct);
                continue;
            }

            // Si la condición dejó de cumplirse (se cumplió o se dio de baja), no se envía.
            var contenido = await ContenidoAsync(notificacion, ct);
            if (contenido is null || destinatarios.Count == 0) continue;

            notificacion.Intentos++;
            notificacion.UltimoIntentoUtc = ahora;
            try
            {
                await emails.EnviarAsync(destinatarios, contenido.Asunto, contenido.Cuerpo, ct);
                notificacion.Estado = EstadoNotificacion.Aceptada;
                notificacion.AceptadaUtc = reloj.AhoraUtc;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "El servicio de email no aceptó la notificación {Clave} (intento {Intento}).",
                    notificacion.Clave, notificacion.Intentos);
            }

            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>El contenido del email, o null si su condición ya no se cumple.</summary>
    private async Task<ContenidoEmail?> ContenidoAsync(Notificacion notificacion, CancellationToken ct)
    {
        switch (notificacion.Tipo)
        {
            case TipoNotificacion.Vencimiento:
                var instancia = await db.Instancias.AsNoTracking()
                    .Include(i => i.Deber!).ThenInclude(d => d.Mascota)
                    .SingleAsync(i => i.Id == notificacion.InstanciaId, ct);
                if (instancia.Estado != EstadoInstancia.Vigente || instancia.Deber!.DadoDeBajaUtc is not null) return null;
                return PlantillasEmail.Vencimiento(
                    instancia.Deber.Mascota!.Nombre, instancia.Deber.Tipo, instancia.Deber.Subtipo, instancia.Dia, instancia.Hora);

            case TipoNotificacion.Recordatorio:
                var deber = await db.Deberes.AsNoTracking()
                    .Include(d => d.Mascota)
                    .SingleAsync(d => d.Id == notificacion.DeberId, ct);
                if (deber.DadoDeBajaUtc is not null) return null;
                return PlantillasEmail.Recordatorio(deber.Mascota!.Nombre, deber.Tipo, deber.Subtipo, deber.Dia!.Value, deber.Hora!.Value);

            case TipoNotificacion.AvisoTurnoBano:
                return await EvaluarAvisoAsync(notificacion.MascotaId!.Value, reloj.Hoy, ct);

            default:
                return null;
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
