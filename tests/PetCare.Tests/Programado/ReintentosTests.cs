using PetCare.Tests.Infra;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Programado;

/// <summary>
/// Reintentos de envío dentro de la ventana de 5 minutos desde la condición, y emails que se
/// pierden fuera de ella (FR-041, R7).
/// </summary>
public class ReintentosTests : IDisposable
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly Escenario _hogar = new();
    private readonly int _firulais;

    public ReintentosTests()
    {
        _hogar.Cuidador("ana@ejemplo.com");
        _firulais = _hogar.Mascota();
    }

    public void Dispose() => _hogar.Dispose();

    private static DateTimeOffset A(int hora, int minuto = 0, int segundo = 0) =>
        new(RelojHogar.AUtc(Hoy, new TimeOnly(hora, minuto, segundo)));

    /// <summary>Medicación diaria de las 09:00 dada de alta a las 07:00; devuelve el deber.</summary>
    private async Task<int> MedicacionDeLas9()
    {
        _hogar.Fijar(Hoy, 7);
        return await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00");
    }

    private EstadoNotificacion EstadoDelVencimiento()
    {
        using var db = _hogar.Base.Contexto();
        return db.Notificaciones.Single(n => n.Tipo == TipoNotificacion.Vencimiento).Estado;
    }

    [Fact]
    public async Task AC_62_si_el_servicio_rechaza_hasta_las_09_17_el_email_se_acepta_a_mas_tardar_a_las_09_20()
    {
        await MedicacionDeLas9();
        _hogar.Emails.Rechaza = t => t < A(9, 17);

        await _hogar.CiclosHasta(Hoy, 9, 20);

        var email = Assert.Single(_hogar.Emails.Enviados);
        Assert.InRange(email.Momento, A(9, 17), A(9, 20));
        Assert.Contains(_hogar.Emails.Intentos, t => t < A(9, 17));
        Assert.Equal(EstadoNotificacion.Aceptada, EstadoDelVencimiento());
    }

    [Fact]
    public async Task AC_62b_si_el_servicio_rechaza_toda_la_ventana_no_se_reintenta_despues_de_las_09_20_y_se_pierde()
    {
        await MedicacionDeLas9();
        _hogar.Emails.Rechaza = t => t <= A(9, 30);

        await _hogar.CiclosHasta(Hoy, 23, 59);

        Assert.Empty(_hogar.Emails.Enviados);
        Assert.NotEmpty(_hogar.Emails.Intentos);
        Assert.All(_hogar.Emails.Intentos, t => Assert.True(t <= A(9, 20), $"intento a las {t:HH:mm:ss} UTC"));
        Assert.Equal(EstadoNotificacion.Perdida, EstadoDelVencimiento());
    }

    [Fact]
    public async Task AC_62c_tras_perderse_eliminar_un_registro_erroneo_dispara_el_email_a_mas_tardar_a_las_10_35()
    {
        var medicacion = await MedicacionDeLas9();
        _hogar.Emails.Rechaza = t => t <= A(9, 30);
        await _hogar.CiclosHasta(Hoy, 10);
        var porError = await _hogar.CumplirOk((await _hogar.Instancia(medicacion, Hoy, "09:00")).Id);
        await _hogar.CiclosHasta(Hoy, 10, 30);

        await _hogar.Eliminar(porError);
        await _hogar.CiclosHasta(Hoy, 10, 35);

        var email = Assert.Single(_hogar.Emails.Enviados);
        Assert.InRange(email.Momento, A(10, 30), A(10, 35));
    }

    [Fact]
    public async Task AC_62d_si_el_servicio_no_responde_hasta_las_09_17_el_email_se_acepta_a_mas_tardar_a_las_09_20()
    {
        await MedicacionDeLas9();
        _hogar.Emails.NoResponde = t => t < A(9, 17);

        await _hogar.CiclosHasta(Hoy, 9, 20);

        var email = Assert.Single(_hogar.Emails.Enviados);
        Assert.InRange(email.Momento, A(9, 17), A(9, 20));
        Assert.Contains(_hogar.Emails.Intentos, t => t < A(9, 17));
    }

    [Fact]
    public async Task R7_una_condicion_detectada_despues_de_5_minutos_se_pierde_sin_intentar_el_envio()
    {
        await MedicacionDeLas9();
        await _hogar.CiclosHasta(Hoy, 9);

        // Servidor caído entre las 09:00 y las 09:25: la condición (09:15) se detecta tarde.
        _hogar.Fijar(Hoy, 9, 25);
        await _hogar.CiclosHasta(Hoy, 10);

        Assert.Empty(_hogar.Emails.Intentos);
        Assert.Equal(EstadoNotificacion.Perdida, EstadoDelVencimiento());
    }
}
