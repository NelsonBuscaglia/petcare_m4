using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Dominio;

/// <summary>
/// Calcula qué instancias corresponden a un deber programable en un día (FR-014, R4). Es una
/// función pura: no lee la base ni el reloj. Los deberes puntuales tienen una única instancia,
/// que se crea al darlos de alta.
/// </summary>
public static class GeneradorInstancias
{
    public static IReadOnlyList<(DateOnly Dia, TimeOnly Hora)> Generar(Deber deber, DateOnly dia)
    {
        if (deber.Modalidad is null || deber.HoraInicio is not { } inicio || !CorrespondeAlDia(deber, dia))
            return [];

        if (deber.Modalidad == Modalidad.HoraFija)
            return [(dia, inicio)];

        // Cada N horas: inicio + k·N mientras siga siendo el mismo día (RF-07).
        var intervalo = TimeSpan.FromHours(deber.IntervaloHoras!.Value);
        var instancias = new List<(DateOnly, TimeOnly)>();
        for (var hora = inicio.ToTimeSpan(); hora < TimeSpan.FromDays(1); hora += intervalo)
            instancias.Add((dia, TimeOnly.FromTimeSpan(hora)));
        return instancias;
    }

    private static bool CorrespondeAlDia(Deber deber, DateOnly dia) => deber.Recurrencia switch
    {
        // Solo el día en que se dio de alta (RF-08).
        Recurrencia.SoloHoy => dia == deber.CreadoDia,
        // Desde el alta, para siempre o hasta la fecha de fin inclusive (RF-08, RF-09).
        Recurrencia.TodosLosDias => dia >= deber.CreadoDia && (deber.HastaDia is null || dia <= deber.HastaDia),
        _ => false,
    };
}
