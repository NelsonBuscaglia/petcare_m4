namespace PetCare.Web.Dominio.Entidades;

/// <summary>Ocurrencia de un deber en un día y hora concretos.</summary>
public class Instancia
{
    public int Id { get; set; }

    public int DeberId { get; set; }

    public Deber? Deber { get; set; }

    /// <summary>Día en hora del hogar.</summary>
    public DateOnly Dia { get; set; }

    /// <summary>Hora en hora del hogar.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Único junto con <see cref="DeberId"/>: generación idempotente (R4).</summary>
    public DateTime ProgramadaUtc { get; set; }

    /// <summary>Solo los estados que nacen de una acción; las marcas se calculan al leer (R3).</summary>
    public EstadoInstancia Estado { get; set; }

    public DateTime GeneradaUtc { get; set; }

    /// <summary>Ya estaba vencida al generarse (FR-037).</summary>
    public bool GeneradaVencida { get; set; }

    /// <summary>Empieza en 0; +1 al eliminar su cumplimiento (R7).</summary>
    public int EpisodioVencimiento { get; set; }

    /// <summary>Momento de la última eliminación de su cumplimiento (R7).</summary>
    public DateTime? ReactivadaUtc { get; set; }
}
