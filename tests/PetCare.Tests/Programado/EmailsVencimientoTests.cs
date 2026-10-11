using PetCare.Tests.Infra;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Programado;

/// <summary>
/// Emails de vencimiento enviados por el procesador programado, sin nadie con la aplicación
/// abierta (FR-036, FR-037, FR-041). Los ciclos corren cada 30 s con el reloj de prueba.
/// </summary>
public class EmailsVencimientoTests : IDisposable
{
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly Escenario _hogar = new();
    private readonly int _firulais;

    public EmailsVencimientoTests()
    {
        _hogar.Cuidador("ana@ejemplo.com");
        _hogar.Cuidador("beto@ejemplo.com");
        _firulais = _hogar.Mascota();
    }

    public void Dispose() => _hogar.Dispose();

    private static DateTimeOffset A(DateOnly dia, int hora, int minuto = 0) => new(RelojHogar.AUtc(dia, new TimeOnly(hora, minuto)));

    private IReadOnlyList<EmailEnviado> Vencimientos() => _hogar.Emails.Enviados.Where(e => e.Asunto.Contains("vencid")).ToList();

    [Fact]
    public async Task AC_48_la_medicacion_vencida_se_avisa_a_todos_los_cuidadores_a_mas_tardar_a_las_09_20()
    {
        _hogar.Fijar(Hoy, 7);
        await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00");

        await _hogar.CiclosHasta(Hoy, 9, 20);

        var email = Assert.Single(Vencimientos());
        Assert.Equal(["ana@ejemplo.com", "beto@ejemplo.com"], email.Destinatarios.Order());
        Assert.InRange(email.Momento, A(Hoy, 9, 15), A(Hoy, 9, 20));
        Assert.Contains("Firulais", email.Asunto);
    }

    [Fact]
    public async Task AC_49_una_medicacion_cumplida_a_las_09_05_no_genera_email()
    {
        _hogar.Fijar(Hoy, 7);
        var medicacion = await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00");
        await _hogar.CiclosHasta(Hoy, 9, 5);
        await _hogar.CumplirOk((await _hogar.Instancia(medicacion, Hoy, "09:00")).Id);

        await _hogar.CiclosHasta(Hoy, 9, 20);

        Assert.Empty(Vencimientos());
    }

    [Fact]
    public async Task AC_43_AC_50_la_visita_vencida_se_avisa_con_su_subtipo_a_mas_tardar_a_las_10_05()
    {
        _hogar.Fijar(Hoy, 7);
        await _hogar.Puntual(_firulais, TipoDeber.VisitaEstudio, Hoy, "10:00", SubtipoVisita.Vacuna);

        await _hogar.CiclosHasta(Hoy, 10, 5);

        var email = Assert.Single(Vencimientos());
        Assert.Equal(2, email.Destinatarios.Count);
        Assert.InRange(email.Momento, A(Hoy, 10, 0), A(Hoy, 10, 5));
        Assert.Contains("vacuna", email.Cuerpo);
    }

    [Fact]
    public async Task AC_14_un_paseo_dado_de_alta_ya_vencido_no_genera_email_en_el_dia()
    {
        _hogar.Fijar(Hoy, 10, 30);
        await _hogar.Programable(_firulais, TipoDeber.Paseo, "10:00", recurrencia: Recurrencia.SoloHoy);

        await _hogar.CiclosHasta(Hoy, 23, 59);

        Assert.Empty(Vencimientos());
    }

    [Fact]
    public async Task AC_17d_un_bano_de_hoy_dado_de_alta_con_la_hora_pasada_no_genera_email()
    {
        _hogar.Fijar(Hoy, 10, 30);
        await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy, "10:00");

        await _hogar.CiclosHasta(Hoy, 23, 59);

        Assert.Empty(Vencimientos());
    }

    [Fact]
    public async Task AC_28c_la_instancia_generada_ya_vencida_tras_un_corte_no_genera_email()
    {
        _hogar.Fijar(Ayer, 7);
        await _hogar.Programable(_firulais, TipoDeber.Comida, "08:00");
        await _hogar.CiclosHasta(Ayer, 23);

        // Servidor caído hasta las 10:00 de hoy.
        _hogar.Fijar(Hoy, 10);
        await _hogar.CiclosHasta(Hoy, 23, 59);

        Assert.DoesNotContain(Vencimientos(), e => e.Cuerpo.Contains("10/10/2026 08:00"));
    }

    [Fact]
    public async Task AC_38b_al_eliminar_el_registro_erroneo_se_envia_el_email_a_mas_tardar_a_las_10_05()
    {
        _hogar.Fijar(Hoy, 7);
        var comida = await _hogar.Programable(_firulais, TipoDeber.Comida, "08:00");
        await _hogar.CiclosHasta(Hoy, 8, 10);
        var porError = await _hogar.CumplirOk((await _hogar.Instancia(comida, Hoy, "08:00")).Id);
        await _hogar.CiclosHasta(Hoy, 10);
        Assert.Empty(Vencimientos());

        await _hogar.Eliminar(porError);
        await _hogar.CiclosHasta(Hoy, 10, 5);

        var email = Assert.Single(Vencimientos());
        Assert.InRange(email.Momento, A(Hoy, 10, 0), A(Hoy, 10, 5));
    }

    [Fact]
    public async Task AC_38c_si_ya_se_envio_el_email_no_se_repite_al_eliminar_un_registro_erroneo()
    {
        _hogar.Fijar(Hoy, 7);
        var comida = await _hogar.Programable(_firulais, TipoDeber.Comida, "08:00");
        await _hogar.CiclosHasta(Hoy, 9);
        Assert.Single(Vencimientos());
        var porError = await _hogar.CumplirOk((await _hogar.Instancia(comida, Hoy, "08:00")).Id);

        await _hogar.CiclosHasta(Hoy, 10);
        await _hogar.Eliminar(porError);
        await _hogar.CiclosHasta(Hoy, 23, 59);

        Assert.Single(Vencimientos());
    }

    [Fact]
    public async Task AC_20b_parte_email_dar_de_baja_la_medicacion_a_las_08_evita_todo_email_del_dia()
    {
        _hogar.Fijar(Ayer, 10);
        var medicacion = await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00");
        _hogar.Fijar(Hoy, 0, 1);
        await _hogar.CiclosHasta(Hoy, 8);

        await _hogar.DarDeBajaDeber(medicacion);
        await _hogar.CiclosHasta(Hoy, 23, 59);

        // La de ayer nació vencida (sin email) y la de hoy se dio de baja antes de vencer.
        Assert.Empty(Vencimientos());
    }
}
