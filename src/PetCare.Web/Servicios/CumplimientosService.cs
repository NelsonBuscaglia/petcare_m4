using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Servicios;

public sealed class CumplimientosService(PetCareDbContext db, RelojHogar reloj)
{
    /// <summary>
    /// Registra el cumplimiento de una instancia vigente (FR-024). Devuelve el id del registro.
    /// Ante dos operaciones simultáneas sobre la misma instancia gana la primera (FR-028, FR-045, R8).
    /// </summary>
    /// <param name="hora">Hora real editada; si falta, la actual. Solo para comida, paseo y medicación.</param>
    public async Task<Resultado<int>> CumplirAsync(int instanciaId, TimeOnly? hora)
    {
        var instancia = await db.Instancias.AsNoTracking()
            .Include(i => i.Deber)
            .SingleOrDefaultAsync(i => i.Id == instanciaId);
        if (instancia is null) return Resultado<int>.Rechazo(Mensajes.TareaInexistente); // FR-031

        TimeOnly? horaReal = null;
        if (instancia.Deber!.EsProgramable)
        {
            var ahora = reloj.HoraActual;
            horaReal = hora ?? new TimeOnly(ahora.Hour, ahora.Minute);
            // La fecha del registro es hoy, así que la hora no puede pasar la actual (FR-026, FR-052).
            if (horaReal > ahora) return Resultado<int>.Rechazo(Mensajes.HoraFutura);
        }
        // Baño y visita/estudio guardan solo la fecha (FR-027).

        await using var transaccion = await db.Database.BeginTransactionAsync();
        var marcada = await db.Instancias
            .Where(i => i.Id == instanciaId && i.Estado == EstadoInstancia.Vigente)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Estado, EstadoInstancia.Cumplida));
        if (marcada == 0) return await RechazoPorEstadoAsync(instanciaId);

        var registro = new RegistroCumplimiento
        {
            InstanciaId = instanciaId,
            Dia = reloj.Hoy,
            Hora = horaReal,
            RegistradoUtc = reloj.AhoraUtc,
        };
        db.RegistrosCumplimiento.Add(registro);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return Resultado<int>.Exito(registro.Id);
    }

    /// <summary>
    /// Elimina un registro con fecha de hoy y deja vigente su instancia, con un nuevo episodio de
    /// vencimiento (FR-030, FR-032, R7). Si el registro ya no existe, no hay nada que eliminar.
    /// </summary>
    public async Task<Resultado> EliminarAsync(int registroId)
    {
        var registro = await db.RegistrosCumplimiento.AsNoTracking().SingleOrDefaultAsync(r => r.Id == registroId);
        if (registro is null) return Resultado.Exito;
        if (registro.Dia != reloj.Hoy) return Resultado.Rechazo(Mensajes.SoloRegistrosDeHoy);

        await using var transaccion = await db.Database.BeginTransactionAsync();
        await db.RegistrosCumplimiento.Where(r => r.Id == registroId).ExecuteDeleteAsync();
        var ahora = reloj.AhoraUtc;
        await db.Instancias
            .Where(i => i.Id == registro.InstanciaId && i.Estado == EstadoInstancia.Cumplida)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Estado, EstadoInstancia.Vigente)
                .SetProperty(i => i.EpisodioVencimiento, i => i.EpisodioVencimiento + 1)
                .SetProperty(i => i.ReactivadaUtc, ahora));
        await transaccion.CommitAsync();
        return Resultado.Exito;
    }

    /// <summary>La instancia ya no estaba vigente: se informa si la cumplieron o la dieron de baja (FR-029, FR-045).</summary>
    private async Task<Resultado<int>> RechazoPorEstadoAsync(int instanciaId)
    {
        var estado = await db.Instancias.Where(i => i.Id == instanciaId).Select(i => i.Estado).SingleAsync();
        return Resultado<int>.Rechazo(estado == EstadoInstancia.DadaDeBaja ? Mensajes.YaDadaDeBaja : Mensajes.YaCumplida);
    }
}
