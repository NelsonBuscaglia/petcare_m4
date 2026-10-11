namespace PetCare.Web.Dominio.Entidades;

/// <summary>Email a enviar a todos los cuidadores: outbox con reintentos (R7).</summary>
public class Notificacion
{
    public int Id { get; set; }

    public TipoNotificacion Tipo { get; set; }

    /// <summary>
    /// Única, evita duplicados: <c>V:{instanciaId}:{episodio}</c>, <c>R:{deberId}</c> o
    /// <c>A:{mascotaId}:{dia}</c> (R7).
    /// </summary>
    public string Clave { get; set; } = "";

    /// <summary>Solo vencimiento.</summary>
    public int? InstanciaId { get; set; }

    /// <summary>Solo recordatorio.</summary>
    public int? DeberId { get; set; }

    /// <summary>Solo aviso de turno de baño.</summary>
    public int? MascotaId { get; set; }

    /// <summary>Momento en que se cumplió la condición de disparo; abre la ventana de 5 minutos (FR-041).</summary>
    public DateTime CondicionUtc { get; set; }

    /// <summary>Solo <c>Aceptada</c> cuenta como enviado (FR-036, FR-038, FR-040).</summary>
    public EstadoNotificacion Estado { get; set; }

    public int Intentos { get; set; }

    public DateTime? UltimoIntentoUtc { get; set; }

    public DateTime? AceptadaUtc { get; set; }
}
