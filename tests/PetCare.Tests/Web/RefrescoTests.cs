using System.Net;
using System.Text.RegularExpressions;
using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Tests.Web;

/// <summary>
/// Actualización automática del estado del día (FR-035, R6): la pantalla pide el parcial cada
/// 10 s; estos tests piden ese parcial como lo haría HTMX y verifican lo que trae.
/// </summary>
public class RefrescoTests : IDisposable
{
    private const string Refresco = "/?handler=Refresco";
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly PetCareWebFactory _app = new();
    private ClienteWeb _ana = null!;
    private ClienteWeb _beto = null!;
    private int _a;

    public RefrescoTests() => _app.Fijar(Hoy, new TimeOnly(7, 0));

    public void Dispose() => _app.Dispose();

    /// <summary>Ana y Beto con sesión en el mismo hogar, y la mascota A.</summary>
    private async Task DosCuidadores()
    {
        _ana = _app.Cliente();
        await _ana.RegistrarAsync("ana@ejemplo.com", "secreta123");
        string codigo;
        using (var db = _app.Base.Contexto()) codigo = db.Hogares.Single().Codigo;
        _beto = _app.Cliente();
        await _beto.RegistrarAsync("beto@ejemplo.com", "secreta123", codigo);
        _a = Semillas.Mascota(_app.Base, "A");
    }

    private int Instancia(TipoDeber tipo, string hora)
    {
        using var db = _app.Base.Contexto();
        var horaDelDia = TimeOnly.Parse(hora);
        var instancia = new Instancia
        {
            Deber = new Deber { MascotaId = _a, Tipo = tipo, Modalidad = Modalidad.HoraFija, HoraInicio = horaDelDia, Recurrencia = Recurrencia.TodosLosDias, CreadoDia = Hoy },
            Dia = Hoy,
            Hora = horaDelDia,
            ProgramadaUtc = RelojHogar.AUtc(Hoy, horaDelDia),
            Estado = EstadoInstancia.Vigente,
        };
        db.Instancias.Add(instancia);
        db.SaveChanges();
        return instancia.Id;
    }

    /// <summary>El refresco que pide HTMX: solo el parcial del estado del día, sin el layout.</summary>
    private async Task<string> Refrescar(ClienteWeb cliente)
    {
        var response = await cliente.GetAsync(Refresco, htmx: true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", html);
        return html;
    }

    private static string Fila(string html, int instanciaId) =>
        Regex.Replace(Regex.Replace(
            Regex.Match(html, $@"<li id=""instancia-{instanciaId}"">(.*?)</span>\s*<div", RegexOptions.Singleline).Groups[1].Value,
            "<[^>]+>", " "), @"\s+", " ").Trim();

    [Fact]
    public async Task RNF_04_el_estado_del_dia_se_refresca_cada_10_segundos()
    {
        await DosCuidadores();

        var pagina = await (await _ana.GetAsync("/")).Content.ReadAsStringAsync();

        Assert.Contains($"hx-get=\"{Refresco}\"", pagina);
        Assert.Contains("hx-trigger=\"every 10s", pagina);
    }

    [Fact]
    public async Task AC_47_el_cumplimiento_de_un_cuidador_se_ve_en_el_refresco_del_otro()
    {
        await DosCuidadores();
        var paseo = Instancia(TipoDeber.Paseo, "09:00");
        _app.Fijar(Hoy, new TimeOnly(9, 5));
        Assert.Contains($"instancia-{paseo}", await Refrescar(_beto));

        await _ana.PostAsync("/?handler=Cumplir", new Dictionary<string, string?> { ["instanciaId"] = paseo.ToString() }, htmx: true);

        var refresco = await Refrescar(_beto);
        Assert.DoesNotContain($"instancia-{paseo}", refresco);
        Assert.Matches(@"<dt>Paseos de hoy</dt><dd>1</dd>", refresco);
    }

    [Fact]
    public async Task AC_47d_la_baja_de_una_instancia_de_un_cuidador_se_ve_en_el_refresco_del_otro()
    {
        await DosCuidadores();
        var paseo = Instancia(TipoDeber.Paseo, "09:00");
        Assert.Contains($"instancia-{paseo}", await Refrescar(_beto));

        await _ana.PostAsync("/?handler=BajaInstancia", new Dictionary<string, string?> { ["instanciaId"] = paseo.ToString() }, htmx: true);

        Assert.DoesNotContain($"instancia-{paseo}", await Refrescar(_beto));
    }

    [Fact]
    public async Task AC_47b_sin_interactuar_el_refresco_muestra_la_marca_pendiente_al_vencer()
    {
        await DosCuidadores();
        var paseo = Instancia(TipoDeber.Paseo, "12:00");

        _app.Fijar(Hoy, new TimeOnly(12, 14));
        Assert.DoesNotContain("pendiente", Fila(await Refrescar(_beto), paseo));

        _app.Fijar(Hoy, new TimeOnly(12, 15));
        Assert.Contains("pendiente", Fila(await Refrescar(_beto), paseo));
    }

    [Fact]
    public async Task AC_47c_sin_interactuar_el_refresco_muestra_no_cumplido_al_superarse_la_hora_del_bano()
    {
        await DosCuidadores();
        var bano = Instancia(TipoDeber.Bano, "15:00");

        _app.Fijar(Hoy, new TimeOnly(14, 59));
        Assert.DoesNotContain("no cumplido", Fila(await Refrescar(_beto), bano));

        _app.Fijar(Hoy, new TimeOnly(15, 0, 1));
        Assert.Contains("no cumplido", Fila(await Refrescar(_beto), bano));
    }

    [Fact]
    public async Task AC_05_el_refresco_sin_sesion_recibe_401_con_HX_Redirect_y_sin_datos()
    {
        await DosCuidadores();

        var response = await _app.Cliente().GetAsync(Refresco, htmx: true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("/Cuenta/Login?motivo=sin-sesion", Assert.Single(response.Headers.GetValues("HX-Redirect")));
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }
}
