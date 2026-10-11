using PetCare.Web.Dominio;

namespace PetCare.Web.Servicios;

/// <summary>Estado del día del hogar: una sección por mascota (FR-033, FR-053).</summary>
public sealed record EstadoDelDia(IReadOnlyList<SeccionMascota> Mascotas)
{
    /// <summary>El hogar no tiene mascotas: se ofrece registrar una (FR-053).</summary>
    public bool SinMascotas => Mascotas.Count == 0;
}

public sealed record SeccionMascota(int MascotaId, string Nombre, IReadOnlyList<InstanciaVista> Instancias);

public sealed record InstanciaVista(int Id, TipoDeber Tipo, DateOnly Dia, TimeOnly Hora);
