using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;

namespace PetCare.Web.Servicios;

public sealed class BajasInstanciaService(PetCareDbContext db, RelojHogar reloj)
{
    /// <summary>
    /// Da de baja una instancia vigente desde el estado del día (FR-015). Si su deber tiene una
    /// sola instancia, lo da de baja también (FR-016); si no, el deber y sus demás instancias
    /// siguen. Ante operaciones simultáneas sobre la instancia gana la primera (FR-045, R8).
    /// </summary>
    public async Task<Resultado> DarDeBajaAsync(int instanciaId)
    {
        var deber = await db.Instancias.AsNoTracking()
            .Where(i => i.Id == instanciaId)
            .Select(i => i.Deber)
            .SingleOrDefaultAsync();
        if (deber is null) return Resultado.Rechazo(Mensajes.TareaInexistente);

        await using var transaccion = await db.Database.BeginTransactionAsync();
        var dadaDeBaja = await db.Instancias
            .Where(i => i.Id == instanciaId && i.Estado == EstadoInstancia.Vigente)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Estado, EstadoInstancia.DadaDeBaja));
        if (dadaDeBaja == 0)
        {
            var estado = await db.Instancias.Where(i => i.Id == instanciaId).Select(i => i.Estado).SingleAsync();
            return Resultado.Rechazo(estado == EstadoInstancia.Cumplida ? Mensajes.YaCumplida : Mensajes.YaDadaDeBaja);
        }

        if (deber.EsDeInstanciaUnica)
        {
            var ahora = reloj.AhoraUtc;
            await db.Deberes
                .Where(d => d.Id == deber.Id && d.DadoDeBajaUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.DadoDeBajaUtc, ahora));
        }

        await transaccion.CommitAsync();
        return Resultado.Exito;
    }
}
