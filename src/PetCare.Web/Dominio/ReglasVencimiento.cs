using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Dominio;

/// <summary>
/// Marcas que se calculan al leer, con la hora actual; no se guardan (data-model, R3).
/// </summary>
public static class ReglasVencimiento
{
    /// <summary>Tiempo sin cumplimiento hasta que una comida, paseo o medicación vence (FR-019).</summary>
    public static readonly TimeSpan Tolerancia = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Una comida, paseo o medicación vigente vence a los 15 minutos de su horario ("pendiente",
    /// FR-019); un baño o visita/estudio vigente, apenas se supera su fecha y hora ("no cumplido",
    /// FR-023).
    /// </summary>
    public static bool EstaVencida(Instancia instancia, TipoDeber tipo, DateTime ahoraUtc) =>
        instancia.Estado == EstadoInstancia.Vigente
        && (EsProgramable(tipo)
            ? ahoraUtc >= instancia.ProgramadaUtc + Tolerancia
            : ahoraUtc > instancia.ProgramadaUtc);

    /// <summary>Una comida o paseo no cumplido expira al terminar su día; la medicación no (FR-021, FR-022).</summary>
    public static bool EstaExpirada(Instancia instancia, TipoDeber tipo, DateOnly hoy) =>
        instancia.Estado == EstadoInstancia.Vigente
        && tipo is TipoDeber.Comida or TipoDeber.Paseo
        && instancia.Dia < hoy;

    private static bool EsProgramable(TipoDeber tipo) => tipo is TipoDeber.Comida or TipoDeber.Paseo or TipoDeber.Medicacion;
}
