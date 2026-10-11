using PetCare.Tests.Infra;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Programado;

/// <summary>Recordatorio 48 horas antes de un baño o visita/estudio (FR-038, FR-041).</summary>
public class EmailsRecordatorioTests : IDisposable
{
    private static readonly DateOnly Lunes = new(2026, 10, 12);
    private static readonly DateOnly Martes = new(2026, 10, 13);
    private static readonly DateOnly Miercoles = new(2026, 10, 14);
    private static readonly DateOnly Jueves = new(2026, 10, 15);
    private static readonly DateOnly Viernes = new(2026, 10, 16);

    private readonly Escenario _hogar = new();
    private readonly int _firulais;

    public EmailsRecordatorioTests()
    {
        _hogar.Cuidador("ana@ejemplo.com");
        _hogar.Cuidador("beto@ejemplo.com");
        _firulais = _hogar.Mascota();
    }

    public void Dispose() => _hogar.Dispose();

    private static DateTimeOffset A(DateOnly dia, int hora, int minuto = 0) => new(RelojHogar.AUtc(dia, new TimeOnly(hora, minuto)));

    private IReadOnlyList<EmailEnviado> Recordatorios() => _hogar.Emails.Enviados.Where(e => e.Asunto.Contains("en 48 horas")).ToList();

    /// <summary>Ciclos alrededor de la hora del recordatorio y después hasta el turno del viernes.</summary>
    private async Task HastaElTurnoDelViernes()
    {
        _hogar.Fijar(Miercoles, 14, 55);
        await _hogar.CiclosHasta(Miercoles, 15, 10);
        _hogar.Fijar(Jueves, 12);
        await _hogar.Ciclo();
        _hogar.Fijar(Viernes, 14, 55);
        await _hogar.CiclosHasta(Viernes, 15);
    }

    [Fact]
    public async Task AC_51_el_recordatorio_del_bano_llega_a_todos_el_miercoles_a_mas_tardar_a_las_15_05()
    {
        _hogar.Fijar(Lunes, 10);
        await _hogar.Puntual(_firulais, TipoDeber.Bano, Viernes, "15:00");

        _hogar.Fijar(Miercoles, 14, 55);
        await _hogar.CiclosHasta(Miercoles, 15, 5);

        var email = Assert.Single(Recordatorios());
        Assert.Equal(["ana@ejemplo.com", "beto@ejemplo.com"], email.Destinatarios.Order());
        Assert.InRange(email.Momento, A(Miercoles, 15), A(Miercoles, 15, 5));
        Assert.Contains("16/10/2026 15:00", email.Cuerpo);
    }

    [Fact]
    public async Task AC_51b_hasta_el_turno_se_envia_exactamente_un_recordatorio()
    {
        _hogar.Fijar(Lunes, 10);
        await _hogar.Puntual(_firulais, TipoDeber.Bano, Viernes, "15:00");

        await HastaElTurnoDelViernes();

        Assert.Single(Recordatorios());
    }

    [Fact]
    public async Task AC_51c_un_bano_dado_de_baja_el_martes_no_genera_recordatorio()
    {
        _hogar.Fijar(Lunes, 10);
        var bano = await _hogar.Puntual(_firulais, TipoDeber.Bano, Viernes, "15:00");
        _hogar.Fijar(Martes, 10);
        await _hogar.DarDeBajaDeber(bano);

        await HastaElTurnoDelViernes();

        Assert.Empty(Recordatorios());
    }

    [Fact]
    public async Task AC_52_un_bano_dado_de_alta_con_menos_de_48_horas_no_genera_recordatorio()
    {
        _hogar.Fijar(Jueves, 10);
        await _hogar.Puntual(_firulais, TipoDeber.Bano, Viernes, "15:00");

        await _hogar.CiclosHasta(Jueves, 10, 10);
        _hogar.Fijar(Viernes, 14, 55);
        await _hogar.CiclosHasta(Viernes, 15);

        Assert.Empty(Recordatorios());
    }

    [Fact]
    public async Task AC_52b_un_bano_dado_de_alta_con_48_horas_exactas_genera_un_recordatorio()
    {
        _hogar.Fijar(Miercoles, 15);
        await _hogar.Puntual(_firulais, TipoDeber.Bano, Viernes, "15:00");

        await _hogar.CiclosHasta(Miercoles, 15, 10);
        _hogar.Fijar(Viernes, 14, 55);
        await _hogar.CiclosHasta(Viernes, 15);

        var email = Assert.Single(Recordatorios());
        Assert.InRange(email.Momento, A(Miercoles, 15), A(Miercoles, 15, 5));
    }

    [Fact]
    public async Task AC_42_el_recordatorio_de_una_visita_indica_el_subtipo()
    {
        _hogar.Fijar(Lunes, 10);
        await _hogar.Puntual(_firulais, TipoDeber.VisitaEstudio, Viernes, "15:00", SubtipoVisita.Vacuna);

        _hogar.Fijar(Miercoles, 14, 55);
        await _hogar.CiclosHasta(Miercoles, 15, 5);

        var email = Assert.Single(Recordatorios());
        Assert.Contains("vacuna", email.Cuerpo);
        Assert.InRange(email.Momento, A(Miercoles, 15), A(Miercoles, 15, 5));
    }
}
