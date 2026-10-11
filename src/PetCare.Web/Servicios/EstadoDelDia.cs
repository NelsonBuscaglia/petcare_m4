using PetCare.Web.Dominio;

namespace PetCare.Web.Servicios;

/// <summary>Estado del día del hogar: una sección por mascota (FR-033, FR-053).</summary>
public sealed record EstadoDelDia(IReadOnlyList<SeccionMascota> Mascotas)
{
    /// <summary>El hogar no tiene mascotas: se ofrece registrar una (FR-053).</summary>
    public bool SinMascotas => Mascotas.Count == 0;
}

public sealed record SeccionMascota(
    int MascotaId,
    string Nombre,
    IReadOnlyList<InstanciaVista> Instancias,
    ResumenMascota Resumen,
    IReadOnlyList<RegistroVista> RegistrosDeHoy)
{
    /// <summary>Sin instancias vigentes la sección se muestra igual, con ese aviso (FR-053).</summary>
    public bool SinTareasPendientes => Instancias.Count == 0;
}

public sealed record InstanciaVista(int Id, TipoDeber Tipo, DateOnly Dia, TimeOnly Hora);

/// <summary>Paseos de hoy y último cumplimiento de comida y de paseo, o null si no hay (FR-034).</summary>
public sealed record ResumenMascota(int PaseosDeHoy, Momento? UltimaComida, Momento? UltimoPaseo);

/// <summary>Fecha y hora real de un cumplimiento.</summary>
public sealed record Momento(DateOnly Dia, TimeOnly? Hora);

/// <summary>Registro de hoy, que se puede eliminar (FR-030).</summary>
public sealed record RegistroVista(int Id, TipoDeber Tipo, TimeOnly? Hora);
