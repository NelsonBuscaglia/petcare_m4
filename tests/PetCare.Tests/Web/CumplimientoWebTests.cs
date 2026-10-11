using System.Net;
using System.Text.RegularExpressions;
using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Tests.Web;

/// <summary>Cumplimiento desde el estado del día con HTMX (contracts/ui-routes.md).</summary>
public class CumplimientoWebTests : IDisposable
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly PetCareWebFactory _app = new();

    public CumplimientoWebTests() => _app.Fijar(Hoy, new TimeOnly(9, 0));

    public void Dispose() => _app.Dispose();

    /// <summary>Ana con sesión y Firulais con un paseo vigente de hoy a las 09:00.</summary>
    private async Task<(ClienteWeb Ana, int Paseo)> AnaConUnPaseoVigente()
    {
        var ana = _app.Cliente();
        await ana.RegistrarAsync("ana@ejemplo.com", "secreta123");
        var firulais = Semillas.Mascota(_app.Base, "Firulais");
        using var db = _app.Base.Contexto();
        var paseo = new Instancia
        {
            Deber = new Deber
            {
                MascotaId = firulais, Tipo = TipoDeber.Paseo, Modalidad = Modalidad.HoraFija,
                HoraInicio = new TimeOnly(9, 0), Recurrencia = Recurrencia.TodosLosDias, CreadoDia = Hoy,
            },
            Dia = Hoy,
            Hora = new TimeOnly(9, 0),
            ProgramadaUtc = RelojHogar.AUtc(Hoy, new TimeOnly(9, 0)),
            Estado = EstadoInstancia.Vigente,
        };
        db.Instancias.Add(paseo);
        db.SaveChanges();
        return (ana, paseo.Id);
    }

    private List<RegistroCumplimiento> Registros()
    {
        using var db = _app.Base.Contexto();
        return db.RegistrosCumplimiento.ToList();
    }

    [Fact]
    public async Task AC_60_con_la_hora_por_defecto_alcanzan_dos_toques_desde_el_estado_del_dia()
    {
        var (ana, paseo) = await AnaConUnPaseoVigente();
        _app.Fijar(Hoy, new TimeOnly(9, 12));
        var estado = await (await ana.GetAsync("/")).Content.ReadAsStringAsync();
        Assert.Contains($"/?handler=FormCumplir&amp;instanciaId={paseo}", estado);

        // Toque 1: "Hecho" en la instancia → formulario con la hora propuesta.
        var form = await ana.GetAsync($"/?handler=FormCumplir&instanciaId={paseo}", htmx: true);
        Assert.Equal(HttpStatusCode.OK, form.StatusCode);
        var horaPropuesta = Regex.Match(await form.Content.ReadAsStringAsync(), "name=\"Hora\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.Equal("09:12", horaPropuesta);

        // Toque 2: "Confirmar" aceptando la hora propuesta.
        var confirmar = await ana.PostAsync("/?handler=Cumplir",
            new Dictionary<string, string?> { ["InstanciaId"] = paseo.ToString(), ["Hora"] = horaPropuesta }, htmx: true);

        Assert.Equal(HttpStatusCode.OK, confirmar.StatusCode);
        var registro = Assert.Single(Registros());
        Assert.Equal((paseo, new TimeOnly(9, 12)), (registro.InstanciaId, registro.Hora!.Value));
        Assert.DoesNotContain($"instanciaId={paseo}", await confirmar.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FR_051_un_cumplimiento_rechazado_devuelve_la_seccion_con_el_motivo()
    {
        var (ana, paseo) = await AnaConUnPaseoVigente();
        _app.Fijar(Hoy, new TimeOnly(9, 12));

        var response = await ana.PostAsync("/?handler=Cumplir",
            new Dictionary<string, string?> { ["InstanciaId"] = paseo.ToString(), ["Hora"] = "10:00" }, htmx: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("La hora no puede ser posterior a la actual.", await response.Content.ReadAsStringAsync());
        Assert.Empty(Registros());
    }

    [Fact]
    public async Task AC_05b_cumplir_sin_sesion_se_rechaza_y_no_se_crea_el_registro()
    {
        var (_, paseo) = await AnaConUnPaseoVigente();

        var response = await _app.Cliente().PostAsync("/?handler=Cumplir",
            new Dictionary<string, string?> { ["InstanciaId"] = paseo.ToString() }, htmx: true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        Assert.Empty(Registros());
    }

    [Fact]
    public async Task AC_05h_eliminar_un_registro_sin_sesion_se_rechaza_y_el_registro_se_conserva()
    {
        var (ana, paseo) = await AnaConUnPaseoVigente();
        await ana.PostAsync("/?handler=Cumplir", new Dictionary<string, string?> { ["InstanciaId"] = paseo.ToString() }, htmx: true);
        var registro = Assert.Single(Registros());

        var response = await _app.Cliente().PostAsync("/?handler=EliminarRegistro",
            new Dictionary<string, string?> { ["RegistroId"] = registro.Id.ToString() }, htmx: true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Single(Registros());
    }
}
