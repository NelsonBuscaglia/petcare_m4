using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Pages;

/// <summary>Textos en español para mostrar datos del dominio en las páginas.</summary>
public static class Formato
{
    public static string Tipo(TipoDeber tipo) => tipo switch
    {
        TipoDeber.Comida => "Comida",
        TipoDeber.Paseo => "Paseo",
        TipoDeber.Medicacion => "Medicación",
        TipoDeber.Bano => "Baño",
        TipoDeber.VisitaEstudio => "Visita/estudio",
        _ => tipo.ToString(),
    };

    public static string Subtipo(SubtipoVisita subtipo) => subtipo switch
    {
        SubtipoVisita.Veterinario => "veterinario",
        SubtipoVisita.Estudio => "estudio",
        SubtipoVisita.Vacuna => "vacuna",
        SubtipoVisita.IntervencionMedica => "intervención médica",
        _ => subtipo.ToString(),
    };

    /// <summary>El tipo con el subtipo de las visitas/estudios, por ejemplo "Visita/estudio (vacuna)" (RF-28a).</summary>
    public static string Tipo(TipoDeber tipo, SubtipoVisita? subtipo) =>
        subtipo is { } s ? $"{Tipo(tipo)} ({Subtipo(s)})" : Tipo(tipo);

    public static string Hora(TimeOnly hora) => hora.ToString("HH:mm");

    public static string Fecha(DateOnly dia) => dia.ToString("dd/MM/yyyy");

    /// <summary>Fecha y, si la hay, hora real de un cumplimiento (FR-034).</summary>
    public static string Momento(Servicios.Momento momento) =>
        momento.Hora is { } hora ? $"{Fecha(momento.Dia)} {Hora(hora)}" : Fecha(momento.Dia);

    /// <summary>Los datos con que se dio de alta un deber (FR-012).</summary>
    public static string Programacion(Deber deber)
    {
        if (!deber.EsProgramable) return $"el {Fecha(deber.Dia!.Value)} a las {Hora(deber.Hora!.Value)}";

        var partes = new List<string>
        {
            deber.Modalidad == Modalidad.CadaNHoras
                ? $"cada {deber.IntervaloHoras} horas desde las {Hora(deber.HoraInicio!.Value)}"
                : $"a las {Hora(deber.HoraInicio!.Value)}",
            deber.Recurrencia == Recurrencia.SoloHoy ? $"solo el {Fecha(deber.CreadoDia)}" : "todos los días",
        };

        if (deber.Tipo == TipoDeber.Medicacion && deber.Recurrencia == Recurrencia.TodosLosDias)
            partes.Add(deber.HastaDia is { } hasta ? $"hasta el {Fecha(hasta)}" : "para siempre");

        return string.Join(", ", partes);
    }
}
