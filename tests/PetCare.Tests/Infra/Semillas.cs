using PetCare.Web.Dominio.Entidades;

namespace PetCare.Tests.Infra;

/// <summary>
/// Datos de partida escritos directo en la base, para que cada test dependa solo del servicio
/// que prueba.
/// </summary>
public static class Semillas
{
    public static void Hogar(BaseDeDatosDePrueba baseDePrueba)
    {
        using var db = baseDePrueba.Contexto();
        db.Hogares.Add(new Hogar { Id = 1, Codigo = "ABCD2345", CreadoUtc = baseDePrueba.Reloj.AhoraUtc });
        db.SaveChanges();
    }

    public static int Mascota(BaseDeDatosDePrueba baseDePrueba, string nombre)
    {
        using var db = baseDePrueba.Contexto();
        var mascota = new Mascota
        {
            HogarId = 1,
            Nombre = nombre,
            NombreNormalizado = nombre.ToUpperInvariant(),
            CreadaDia = baseDePrueba.Reloj.Hoy,
        };
        db.Mascotas.Add(mascota);
        db.SaveChanges();
        return mascota.Id;
    }
}
