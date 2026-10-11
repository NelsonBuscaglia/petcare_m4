using System.Net;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Web;

/// <summary>Vencimiento de la sesión a los 30 días de la última solicitud (FR-005, R9).</summary>
public class SesionTests : IDisposable
{
    private static readonly DateOnly Dia1 = new(2026, 11, 1);

    private readonly PetCareWebFactory _app = new();

    public void Dispose() => _app.Dispose();

    private static DateOnly Dia(int numero) => Dia1.AddDays(numero - 1);

    private void Fijar(int dia, int hora, int minuto = 0) => _app.Fijar(Dia(dia), new TimeOnly(hora, minuto));

    /// <summary>Ana se registra (y queda con sesión) el día 1 a las 10:00.</summary>
    private async Task<ClienteWeb> AnaConSesionDesdeElDia1()
    {
        Fijar(1, 10);
        var ana = _app.Cliente();
        await ana.RegistrarAsync("ana@ejemplo.com", "secreta123");
        return ana;
    }

    [Fact]
    public async Task AC_04c_tras_30_dias_sin_solicitudes_pide_iniciar_sesion_sin_mostrar_datos()
    {
        var ana = await AnaConSesionDesdeElDia1();
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync("/")).StatusCode);

        Fijar(31, 10, 1);
        var response = await ana.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Cuenta/Login?motivo=sin-sesion", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AC_04d_una_solicitud_el_dia_20_mantiene_la_sesion_hasta_el_dia_45()
    {
        var ana = await AnaConSesionDesdeElDia1();

        Fijar(20, 10);
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync("/")).StatusCode);

        Fijar(45, 10);
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task AC_04e_los_refrescos_automaticos_no_renuevan_la_sesion()
    {
        var ana = await AnaConSesionDesdeElDia1();
        foreach (var dia in new[] { 5, 15, 25, 30 })
        {
            Fijar(dia, 10);
            Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync("/?handler=Refresco", htmx: true)).StatusCode);
        }

        Fijar(31, 10, 1);
        var response = await ana.GetAsync("/?handler=Refresco", htmx: true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("/Cuenta/Login?motivo=sin-sesion", Assert.Single(response.Headers.GetValues("HX-Redirect")));
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FR_005_la_cookie_de_sesion_es_persistente_y_vence_30_dias_despues()
    {
        await AnaConSesionDesdeElDia1();
        Fijar(2, 8);

        var response = await _app.Cliente().IniciarSesionAsync("ana@ejemplo.com", "secreta123");

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(".AspNetCore.Cookies="));
        var expires = cookie.Split(';', StringSplitOptions.TrimEntries)
            .SingleOrDefault(p => p.StartsWith("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(expires); // sin "expires" la cookie se pierde al cerrar el navegador
        Assert.Equal(_app.Tiempo.GetUtcNow().AddDays(30), DateTimeOffset.Parse(expires["expires=".Length..]));
    }
}
