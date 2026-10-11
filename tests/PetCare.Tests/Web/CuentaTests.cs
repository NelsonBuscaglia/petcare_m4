using System.Net;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Web;

public class CuentaTests : IDisposable
{
    private const string LoginSinSesion = "/Cuenta/Login?motivo=sin-sesion";

    private readonly PetCareWebFactory _app = new();

    public void Dispose() => _app.Dispose();

    private string CodigoGuardado()
    {
        using var db = _app.Base.Contexto();
        return db.Hogares.Single().Codigo;
    }

    /// <summary>Ana crea el hogar desde su navegador y queda con sesión iniciada.</summary>
    private async Task<ClienteWeb> AnaCreaElHogar()
    {
        var ana = _app.Cliente();
        var response = await ana.RegistrarAsync("ana@ejemplo.com", "secreta123");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ana;
    }

    private static async Task<string> Html(HttpResponseMessage response) => await response.Content.ReadAsStringAsync();

    private static void AssertRedirigeAlLoginSinSesion(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(LoginSinSesion, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AC_01_al_crear_el_hogar_la_primera_persona_ve_el_codigo()
    {
        var ana = _app.Cliente();

        var response = await ana.RegistrarAsync("ana@ejemplo.com", "secreta123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(CodigoGuardado(), await Html(response));
    }

    [Fact]
    public async Task AC_01b_el_segundo_cuidador_ve_el_mismo_codigo_de_hogar()
    {
        await AnaCreaElHogar();
        var beto = _app.Cliente();

        var registro = await beto.RegistrarAsync("beto@ejemplo.com", "secreta123", CodigoGuardado());

        Assert.Equal(HttpStatusCode.Redirect, registro.StatusCode);
        Assert.Equal("/", registro.Headers.Location?.OriginalString);
        var pagina = await beto.GetAsync("/Hogar/Codigo");
        Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
        Assert.Contains(CodigoGuardado(), await Html(pagina));
    }

    [Fact]
    public async Task AC_02_el_segundo_cuidador_al_iniciar_sesion_ve_la_seccion_de_la_mascota_A()
    {
        var ana = await AnaCreaElHogar();
        await ana.PostAsync("/Mascotas/Nueva", new Dictionary<string, string?> { ["Nombre"] = "A" });
        await _app.Cliente().RegistrarAsync("beto@ejemplo.com", "secreta123", CodigoGuardado());
        var beto = _app.Cliente();

        var login = await beto.IniciarSesionAsync("beto@ejemplo.com", "secreta123");

        Assert.Equal("/", login.Headers.Location?.OriginalString);
        var estado = await beto.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, estado.StatusCode);
        int mascotaA;
        using (var db = _app.Base.Contexto()) mascotaA = db.Mascotas.Single(m => m.Nombre == "A").Id;
        var html = await Html(estado);
        Assert.Contains($"id=\"mascota-{mascotaA}\"", html);
        Assert.Contains("<h2>A</h2>", html);
    }

    [Fact]
    public async Task FR_051_un_registro_rechazado_muestra_el_motivo()
    {
        await AnaCreaElHogar();

        var response = await _app.Cliente().RegistrarAsync("beto@ejemplo.com", "secreta123", "ZZZZ9999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("El código de hogar es incorrecto.", await Html(response));
    }

    [Theory]
    [InlineData("ana@ejemplo.com")]
    [InlineData("Ana@Ejemplo.com")]
    public async Task AC_03_AC_03b_al_iniciar_sesion_ve_el_estado_del_dia(string email)
    {
        await AnaCreaElHogar();
        var navegador = _app.Cliente();

        var response = await navegador.IniciarSesionAsync(email, "secreta123");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await navegador.GetAsync("/")).StatusCode);
    }

    [Theory]
    [InlineData("ana@ejemplo.com", "incorrecta1")]
    [InlineData("beto@ejemplo.com", "secreta123")]
    public async Task AC_04_AC_04f_login_fallido_da_el_mismo_mensaje_y_no_inicia_sesion(string email, string password)
    {
        await AnaCreaElHogar();
        var navegador = _app.Cliente();

        var response = await navegador.IniciarSesionAsync(email, password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await Html(response);
        Assert.Contains("El email o la contraseña son incorrectos.", html);
        Assert.DoesNotContain(CodigoGuardado(), html);
        AssertRedirigeAlLoginSinSesion(await navegador.GetAsync("/"));
    }

    [Fact]
    public async Task AC_04b_tras_cerrar_sesion_se_rechaza_ver_el_estado_del_dia()
    {
        var ana = await AnaCreaElHogar();

        var logout = await ana.PostAsync("/Cuenta/Logout", new Dictionary<string, string?>());

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        AssertRedirigeAlLoginSinSesion(await ana.GetAsync("/"));
    }

    [Fact]
    public async Task AC_05j_sin_sesion_no_se_devuelve_el_codigo_de_hogar()
    {
        await AnaCreaElHogar();

        var response = await _app.Cliente().GetAsync("/Hogar/Codigo");

        AssertRedirigeAlLoginSinSesion(response);
        Assert.DoesNotContain(CodigoGuardado(), await Html(response));
    }

    [Fact]
    public async Task AC_05k_sin_sesion_el_estado_del_dia_lleva_al_login_que_informa_el_motivo()
    {
        await AnaCreaElHogar();
        var anonimo = _app.Cliente();

        AssertRedirigeAlLoginSinSesion(await anonimo.GetAsync("/"));
        var login = await anonimo.GetAsync(LoginSinSesion);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var html = await Html(login);
        Assert.Contains("Iniciá sesión para continuar.", html);
        Assert.DoesNotContain(CodigoGuardado(), html);
    }

    [Fact]
    public async Task FR_006_una_solicitud_HTMX_sin_sesion_recibe_401_con_HX_Redirect_y_sin_cuerpo()
    {
        await AnaCreaElHogar();

        var response = await _app.Cliente().GetAsync("/", htmx: true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(LoginSinSesion, Assert.Single(response.Headers.GetValues("HX-Redirect")));
        Assert.Empty(await Html(response));
    }
}
