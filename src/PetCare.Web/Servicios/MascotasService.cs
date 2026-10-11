using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Servicios;

public sealed class MascotasService(PetCareDbContext db, RelojHogar reloj)
{
    /// <summary>
    /// Registra una mascota del hogar; rechaza un nombre que ya tiene otra mascota, sin
    /// distinguir mayúsculas (FR-007). Devuelve el id de la mascota.
    /// </summary>
    public async Task<Resultado<int>> RegistrarAsync(string nombre)
    {
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

    public static string Normalizar(string nombre) => nombre.ToUpperInvariant();
}
