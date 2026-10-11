namespace PetCare.Web.Dominio.Entidades;

/// <summary>Deber programable (comida, paseo, medicación) o puntual (baño, visita/estudio).</summary>
public class Deber
{
    public int Id { get; set; }

    /// <summary>Una única mascota (FR-009).</summary>
    public int MascotaId { get; set; }

    public Mascota? Mascota { get; set; }

    public TipoDeber Tipo { get; set; }

    /// <summary>Solo VisitaEstudio, obligatorio (FR-011, RF-11d).</summary>
    public SubtipoVisita? Subtipo { get; set; }

    /// <summary>Solo programables: hora fija o cada N horas (FR-009).</summary>
    public Modalidad? Modalidad { get; set; }

    /// <summary>Hora fija, o inicio de la serie cada N horas.</summary>
    public TimeOnly? HoraInicio { get; set; }

    /// <summary>Solo CadaNHoras: entero 1–23 (FR-009).</summary>
    public int? IntervaloHoras { get; set; }

    /// <summary>Solo programables: solo para hoy o todos los días (FR-010).</summary>
    public Recurrencia? Recurrencia { get; set; }

    /// <summary>Solo medicación diaria; null = para siempre; inclusive; no anterior a hoy al alta (FR-010).</summary>
    public DateOnly? HastaDia { get; set; }

    /// <summary>Solo puntuales: día agendado, no anterior a hoy al alta (FR-011).</summary>
    public DateOnly? Dia { get; set; }

    /// <summary>Solo puntuales: hora agendada; si es hoy, puede haber pasado (FR-011).</summary>
    public TimeOnly? Hora { get; set; }

    /// <summary>Decide si corresponde recordatorio (FR-038).</summary>
    public DateTime CreadoUtc { get; set; }

    public DateOnly CreadoDia { get; set; }

    /// <summary>Baja definitiva (FR-013); nunca vuelve a null.</summary>
    public DateTime? DadoDeBajaUtc { get; set; }

    /// <summary>Comida, paseo o medicación; baño y visita/estudio son puntuales (FR-009, FR-011).</summary>
    public bool EsProgramable => Tipo is TipoDeber.Comida or TipoDeber.Paseo or TipoDeber.Medicacion;

    /// <summary>Tiene una sola instancia: puntual, o programable de hora fija "solo para hoy" (FR-016).</summary>
    public bool EsDeInstanciaUnica =>
        !EsProgramable || (Modalidad == Dominio.Modalidad.HoraFija && Recurrencia == Dominio.Recurrencia.SoloHoy);
}
