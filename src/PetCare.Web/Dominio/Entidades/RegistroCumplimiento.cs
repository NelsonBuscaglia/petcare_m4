namespace PetCare.Web.Dominio.Entidades;

public class RegistroCumplimiento
{
    public int Id { get; set; }

    /// <summary>Único: una instancia tiene a lo sumo un registro (FR-028).</summary>
    public int InstanciaId { get; set; }

    public Instancia? Instancia { get; set; }

    /// <summary>Siempre el día en que se registra (FR-052).</summary>
    public DateOnly Dia { get; set; }

    /// <summary>Hora real, solo comida, paseo y medicación (FR-025, FR-026); null en baño y visita/estudio (FR-027).</summary>
    public TimeOnly? Hora { get; set; }

    public DateTime RegistradoUtc { get; set; }
}
