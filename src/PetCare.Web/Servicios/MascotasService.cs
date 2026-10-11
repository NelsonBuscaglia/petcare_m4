using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Servicios;

public sealed class MascotasService(PetCareDbContext db, RelojHogar reloj)
{
    /// <summary>
    /// Registra una mascota del hogar con el nombre sin espacios en los extremos (RF-04d);
    /// rechaza un nombre vacío (RF-04c) o uno que ya tiene otra mascota, sin distinguir
    /// mayúsculas (FR-007). Devuelve el id de la mascota.
    /// </summary>
    public async Task<Resultado<int>> RegistrarAsync(string nombre)
    {
        nombre = nombre.Trim();
        if (nombre.Length == 0) return Resultado<int>.Rechazo(Mensajes.MascotaSinNombre);

        var hogarId = (await db.Hogares.AsNoTracking().SingleAsync()).Id;
        var nombreNormalizado = Normalizar(nombre);

        if (await db.Mascotas.AnyAsync(m => m.HogarId == hogarId && m.NombreNormalizado == nombreNormalizado))
            return Resultado<int>.Rechazo(Mensajes.MascotaRepetida);

        var mascota = new Mascota
        {
            HogarId = hogarId,
            Nombre = nombre,
            NombreNormalizado = nombreNormalizado,
            CreadaDia = reloj.Hoy,
        };
        db.Mascotas.Add(mascota);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // El mismo nombre se registró al mismo tiempo: lo frena el índice único.
            return Resultado<int>.Rechazo(Mensajes.MascotaRepetida);
        }

        return Resultado<int>.Exito(mascota.Id);
    }

    /// <summary>
    /// Define o modifica el máximo de días entre baños de la mascota, sin requerir un deber
    /// (FR-008). Recibe siempre un valor: una vez definido no se puede quitar. Rechaza un valor
    /// menor que 1 sin modificar el máximo (RF-05c).
    /// </summary>
    public async Task<Resultado> DefinirMaxDiasEntreBanosAsync(int mascotaId, int maximo)
    {
        if (maximo < 1) return Resultado.Rechazo(Mensajes.MaxBanosInvalido);

        await db.Mascotas
            .Where(m => m.Id == mascotaId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.MaxDiasEntreBanos, maximo));
        return Resultado.Exito;
    }

    public static string Normalizar(string nombre) => nombre.ToUpperInvariant();
}
