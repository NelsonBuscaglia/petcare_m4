namespace PetCare.Web.Dominio.Entidades;

public class Hogar
{
    /// <summary>Siempre <c>1</c>: existe un único hogar (FR-048, R8).</summary>
    public int Id { get; set; }

    /// <summary>8 caracteres generados al crear el hogar (FR-046, R10); se guarda en mayúsculas.</summary>
    public string Codigo { get; set; } = "";

    public DateTime CreadoUtc { get; set; }
}
