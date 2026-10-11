using PetCare.Web.Dominio;

namespace PetCare.Tests.Dominio;

/// <summary>Contenido de los emails (contracts/emails.md; FR-039, FR-054).</summary>
public class PlantillasEmailTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);
    private static readonly DateOnly Viernes = new(2026, 10, 16);

    [Fact]
    public void AC_64_el_vencimiento_indica_mascota_tipo_fecha_y_hora()
    {
        var email = PlantillasEmail.Vencimiento("Firulais", TipoDeber.Medicacion, null, Hoy, new TimeOnly(9, 0));

        // Ejemplo literal del contrato de emails.
        Assert.Equal("PetCare: Firulais – Medicación vencida", email.Asunto);
        Assert.Equal("Firulais tiene una medicación vencida, programada para el 10/10/2026 09:00.", email.Cuerpo);
    }

    [Theory]
    [InlineData(TipoDeber.Comida, "Comida vencida", "una comida vencida")]
    [InlineData(TipoDeber.Paseo, "Paseo vencido", "un paseo vencido")]
    [InlineData(TipoDeber.Bano, "Baño vencido", "un baño vencido")]
    public void AC_64_el_vencimiento_nombra_cada_tipo(TipoDeber tipo, string enAsunto, string enCuerpo)
    {
        var email = PlantillasEmail.Vencimiento("Firulais", tipo, null, Hoy, new TimeOnly(8, 0));

        Assert.Equal($"PetCare: Firulais – {enAsunto}", email.Asunto);
        Assert.Contains($"Firulais tiene {enCuerpo}", email.Cuerpo);
        Assert.Contains("10/10/2026 08:00", email.Cuerpo);
    }

    [Fact]
    public void AC_43_el_vencimiento_de_una_visita_indica_el_subtipo()
    {
        var email = PlantillasEmail.Vencimiento("Firulais", TipoDeber.VisitaEstudio, SubtipoVisita.Vacuna, Hoy, new TimeOnly(10, 0));

        Assert.Contains("vacuna", email.Asunto);
        Assert.Contains("vacuna", email.Cuerpo);
        Assert.Contains("10/10/2026 10:00", email.Cuerpo);
    }

    [Fact]
    public void AC_64b_el_recordatorio_indica_mascota_tipo_y_la_fecha_y_hora_del_turno()
    {
        var email = PlantillasEmail.Recordatorio("Firulais", TipoDeber.Bano, null, Viernes, new TimeOnly(15, 0));

        Assert.Equal("PetCare: Firulais – Baño en 48 horas", email.Asunto);
        Assert.Contains("Firulais", email.Cuerpo);
        Assert.Contains("baño", email.Cuerpo);
        Assert.Contains("16/10/2026 15:00", email.Cuerpo);
    }

    [Fact]
    public void AC_42_el_recordatorio_de_una_visita_indica_el_subtipo()
    {
        var email = PlantillasEmail.Recordatorio("Firulais", TipoDeber.VisitaEstudio, SubtipoVisita.Vacuna, Viernes, new TimeOnly(15, 0));

        Assert.Contains("vacuna", email.Asunto);
        Assert.Contains("vacuna", email.Cuerpo);
        Assert.Contains("16/10/2026 15:00", email.Cuerpo);
    }
}
