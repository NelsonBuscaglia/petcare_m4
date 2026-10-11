using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Dominio;

public static class ReglasDeber
{
    /// <summary>
    /// Un deber está activo si no fue dado de baja y, además, tiene una instancia vigente no
    /// expirada o es diario y todavía puede generar instancias (data-model; FR-012, FR-040 c).
    /// </summary>
    /// <param name="instancias">Las instancias del deber.</param>
    public static bool EsActivo(Deber deber, IEnumerable<Instancia> instancias, DateOnly hoy)
    {
        if (deber.DadoDeBajaUtc is not null) return false;

        var tieneInstanciaVigente = instancias.Any(i =>
            i.Estado == EstadoInstancia.Vigente && !ReglasVencimiento.EstaExpirada(i, deber.Tipo, hoy));
        var puedeGenerar = deber.Recurrencia == Recurrencia.TodosLosDias && (deber.HastaDia is null || deber.HastaDia > hoy);

        return tieneInstanciaVigente || puedeGenerar;
    }
}
