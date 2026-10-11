namespace PetCare.Web.Dominio;

/// <summary>Asunto y cuerpo de un email, en texto plano.</summary>
public sealed record ContenidoEmail(string Asunto, string Cuerpo);

/// <summary>
/// Contenido de los emails (contracts/emails.md; FR-039, FR-054). Fechas y horas en hora del
/// hogar, con formato <c>dd/MM/yyyy HH:mm</c>.
/// </summary>
public static class PlantillasEmail
{
    /// <summary>Indica la mascota, el tipo (con subtipo) y la fecha y hora de la instancia vencida.</summary>
    public static ContenidoEmail Vencimiento(string mascota, TipoDeber tipo, SubtipoVisita? subtipo, DateOnly dia, TimeOnly hora)
    {
        var deber = Nombre(tipo, subtipo);
        var (vencido, programado) = deber.Femenino ? ("vencida", "programada") : ("vencido", "programado");
        return new ContenidoEmail(
            $"PetCare: {mascota} – {deber.Mayuscula} {vencido}",
            $"{mascota} tiene {deber.Articulo} {deber.Minuscula} {vencido}, {programado} para el {FechaHora(dia, hora)}.");
    }

    /// <summary>Indica la mascota, el tipo (con subtipo) y la fecha y hora agendadas.</summary>
    public static ContenidoEmail Recordatorio(string mascota, TipoDeber tipo, SubtipoVisita? subtipo, DateOnly dia, TimeOnly hora)
    {
        var deber = Nombre(tipo, subtipo);
        var agendado = deber.Femenino ? "agendada" : "agendado";
        return new ContenidoEmail(
            $"PetCare: {mascota} – {deber.Mayuscula} en 48 horas",
            $"{mascota} tiene {deber.Articulo} {deber.Minuscula} {agendado} para el {FechaHora(dia, hora)}.");
    }

    /// <summary>Indica la mascota, su máximo de días entre baños y la fecha del último baño, o que no hay ninguno.</summary>
    public static ContenidoEmail AvisoTurnoBano(string mascota, int maximoDias, DateOnly? fechaUltimoBano) =>
        new(
            $"PetCare: hay que pedir turno de baño para {mascota}",
            $"Hay que pedir turno de baño para {mascota}: su máximo es de {maximoDias} días entre baños y "
                + (fechaUltimoBano is { } fecha ? $"el último baño registrado fue el {fecha:dd/MM/yyyy}." : "no hay baños registrados."));

    private static string FechaHora(DateOnly dia, TimeOnly hora) => $"{dia:dd/MM/yyyy} {hora:HH:mm}";

    private sealed record NombreDeber(string Mayuscula, string Minuscula, bool Femenino)
    {
        public string Articulo => Femenino ? "una" : "un";
    }

    /// <summary>El nombre del tipo, con el subtipo de las visitas/estudios (RF-28c, RF-28d) y su género.</summary>
    private static NombreDeber Nombre(TipoDeber tipo, SubtipoVisita? subtipo)
    {
        var (nombre, femenino) = tipo switch
        {
            TipoDeber.Comida => ("comida", true),
            TipoDeber.Paseo => ("paseo", false),
            TipoDeber.Medicacion => ("medicación", true),
            TipoDeber.Bano => ("baño", false),
            _ => ("visita/estudio", true),
        };
        if (tipo == TipoDeber.VisitaEstudio && subtipo is { } s) nombre += $" ({Subtipo(s)})";
        return new NombreDeber(char.ToUpperInvariant(nombre[0]) + nombre[1..], nombre, femenino);
    }

    private static string Subtipo(SubtipoVisita subtipo) => subtipo switch
    {
        SubtipoVisita.Veterinario => "veterinario",
        SubtipoVisita.Estudio => "estudio",
        SubtipoVisita.Vacuna => "vacuna",
        _ => "intervención médica",
    };
}
