using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Dominio;

public static class AvisoTurnoBano
{
    /// <summary>
    /// Corresponde avisar que hay que pedir turno de baño si la mascota tiene máximo de días entre
    /// baños, ya pasaron más días que ese máximo desde el último baño (o desde su alta si no hay
    /// ninguno), contando días calendario en UTC−3, y no tiene ningún baño activo (FR-040).
    /// </summary>
    public static bool Corresponde(Mascota mascota, DateOnly? fechaUltimoBano, bool tieneBanoActivo, DateOnly hoy)
    {
        if (mascota.MaxDiasEntreBanos is not { } maximo || tieneBanoActivo) return false;

        var desde = fechaUltimoBano ?? mascota.CreadaDia;
        return hoy.DayNumber - desde.DayNumber > maximo;
    }
}
