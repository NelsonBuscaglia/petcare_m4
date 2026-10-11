namespace PetCare.Web.Dominio;

/// <summary>
/// Hora del hogar: UTC−3 fijo, sin horario de verano (FR-044, R2). Toda hora sale del
/// <see cref="TimeProvider"/> inyectado.
/// </summary>
public sealed class RelojHogar(TimeProvider tiempo)
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-3);

    public DateTime AhoraUtc => tiempo.GetUtcNow().UtcDateTime;

    public DateOnly Hoy => ADiaHora(AhoraUtc).Dia;

    public TimeOnly HoraActual => ADiaHora(AhoraUtc).Hora;

    public static DateTime AUtc(DateOnly dia, TimeOnly hora) =>
        DateTime.SpecifyKind(dia.ToDateTime(hora) - Offset, DateTimeKind.Utc);

    public static (DateOnly Dia, TimeOnly Hora) ADiaHora(DateTime utc)
    {
        var hogar = utc + Offset;
        return (DateOnly.FromDateTime(hogar), TimeOnly.FromDateTime(hogar));
    }
}
