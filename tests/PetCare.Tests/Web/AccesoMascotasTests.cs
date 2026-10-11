using System.Net;
using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Tests.Web;

/// <summary>Sin sesión no se ven ni se modifican mascotas ni deberes (FR-006).</summary>
public class AccesoMascotasTests : IDisposable
{
    private readonly PetCareWebFactory _app = new();
    private readonly ClienteWeb _anonimo;
    private readonly int _firulais;

    public AccesoMascotasTests()
    {
        _app.Fijar(new DateOnly(2026, 10, 10), new TimeOnly(10, 0));
        Semillas.Hogar(_app.Base);
        _firulais = Semillas.Mascota(_app.Base, "Firulais");
        _anonimo = _app.Cliente();
    }

    public void Dispose() => _app.Dispose();

    private static async Task AssertRechazadoSinDatos(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Cuenta/Login?motivo=sin-sesion", response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("Firulais", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AC_05_el_estado_del_dia_sin_sesion_no_devuelve_datos()
    {
        await AssertRechazadoSinDatos(await _anonimo.GetAsync("/"));
    }

    [Fact]
    public async Task AC_05_el_estado_del_dia_pedido_por_HTMX_sin_sesion_recibe_401_sin_datos()
    {
        var response = await _anonimo.GetAsync("/", htmx: true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AC_05c_la_configuracion_de_deberes_sin_sesion_no_devuelve_datos()
    {
        await AssertRechazadoSinDatos(await _anonimo.GetAsync($"/Mascotas/{_firulais}/Deberes"));
    }

    [Fact]
    public async Task AC_05d_registrar_una_mascota_sin_sesion_se_rechaza_y_no_se_crea()
    {
        var response = await _anonimo.PostAsync("/Mascotas/Nueva", new Dictionary<string, string?> { ["Nombre"] = "Michi" });

        await AssertRechazadoSinDatos(response);
        using var db = _app.Base.Contexto();
        Assert.Equal(["Firulais"], db.Mascotas.Select(m => m.Nombre));
    }

    [Fact]
    public async Task AC_05e_dar_de_alta_un_deber_sin_sesion_se_rechaza_y_no_se_crea()
    {
        var response = await _anonimo.PostAsync($"/Mascotas/{_firulais}/Deberes?handler=AltaProgramable",
            new Dictionary<string, string?>
            {
                ["Tipo"] = "Paseo",
                ["Modalidad"] = "HoraFija",
                ["HoraInicio"] = "09:00",
                ["Recurrencia"] = "TodosLosDias",
            });

        await AssertRechazadoSinDatos(response);
        using var db = _app.Base.Contexto();
        Assert.Empty(db.Deberes);
        Assert.Empty(db.Instancias);
    }

    /// <summary>Paseo diario de Firulais con su instancia de hoy vigente; devuelve el deber y la instancia.</summary>
    private (int Deber, int Instancia) PaseoVigente()
    {
        using var db = _app.Base.Contexto();
        var hoy = new DateOnly(2026, 10, 10);
        var instancia = new Instancia
        {
            Deber = new Deber
            {
                MascotaId = _firulais, Tipo = TipoDeber.Paseo, Modalidad = Modalidad.HoraFija,
                HoraInicio = new TimeOnly(12, 0), Recurrencia = Recurrencia.TodosLosDias, CreadoDia = hoy,
            },
            Dia = hoy,
            Hora = new TimeOnly(12, 0),
            ProgramadaUtc = RelojHogar.AUtc(hoy, new TimeOnly(12, 0)),
            Estado = EstadoInstancia.Vigente,
        };
        db.Instancias.Add(instancia);
        db.SaveChanges();
        return (instancia.DeberId, instancia.Id);
    }

    [Fact]
    public async Task AC_05f_dar_de_baja_un_deber_sin_sesion_se_rechaza_y_el_deber_sigue_activo()
    {
        var (deber, instancia) = PaseoVigente();

        var response = await _anonimo.PostAsync($"/Mascotas/{_firulais}/Deberes?handler=Baja",
            new Dictionary<string, string?> { ["DeberId"] = deber.ToString() });

        await AssertRechazadoSinDatos(response);
        using var db = _app.Base.Contexto();
        Assert.Null(db.Deberes.Single(d => d.Id == deber).DadoDeBajaUtc);
        Assert.Equal(EstadoInstancia.Vigente, db.Instancias.Single(i => i.Id == instancia).Estado);
    }

    [Fact]
    public async Task AC_05g_dar_de_baja_una_instancia_sin_sesion_se_rechaza_y_sigue_vigente()
    {
        var (_, instancia) = PaseoVigente();

        var response = await _anonimo.PostAsync("/?handler=BajaInstancia",
            new Dictionary<string, string?> { ["InstanciaId"] = instancia.ToString() }, htmx: true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        using var db = _app.Base.Contexto();
        Assert.Equal(EstadoInstancia.Vigente, db.Instancias.Single(i => i.Id == instancia).Estado);
    }

    [Fact]
    public async Task AC_05i_cambiar_el_maximo_de_dias_entre_banos_sin_sesion_se_rechaza_y_sigue_en_20()
    {
        using (var db = _app.Base.Contexto())
        {
            db.Mascotas.Single(m => m.Id == _firulais).MaxDiasEntreBanos = 20;
            db.SaveChanges();
        }

        var response = await _anonimo.PostAsync($"/Mascotas/{_firulais}/Deberes?handler=MaxBanos",
            new Dictionary<string, string?> { ["MaxDiasEntreBanos"] = "30" });

        await AssertRechazadoSinDatos(response);
        using var verificacion = _app.Base.Contexto();
        Assert.Equal(20, verificacion.Mascotas.Single(m => m.Id == _firulais).MaxDiasEntreBanos);
    }
}
