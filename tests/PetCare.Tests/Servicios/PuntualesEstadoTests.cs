using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

/// <summary>Baños y visitas/estudios en el estado del día (FR-023, FR-033).</summary>
public class PuntualesEstadoTests : IDisposable
{
    private static readonly DateOnly Lunes = new(2026, 10, 5);
    private static readonly DateOnly Viernes = new(2026, 10, 9);
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly Escenario _hogar = new();
    private readonly int _firulais;

    public PuntualesEstadoTests() => _firulais = _hogar.Mascota();

    public void Dispose() => _hogar.Dispose();

    private static string Ver(InstanciaVista i) =>
        $"{i.Dia:dd} {i.Hora:HH:mm} {i.Tipo}{(i.Subtipo is null ? "" : $"/{i.Subtipo}")} {i.Marca}";

    private async Task<string[]> Instancias() => (await _hogar.Seccion(_firulais)).Instancias.Select(Ver).ToArray();

    [Fact]
    public async Task AC_17_el_bano_del_viernes_aparece_el_viernes_sin_marca_antes_de_su_hora()
    {
        _hogar.Fijar(Lunes, 10);
        await _hogar.Puntual(_firulais, TipoDeber.Bano, Viernes, "15:00");

        _hogar.Fijar(Viernes, 9);

        Assert.Equal(["09 15:00 Bano Ninguna"], await Instancias());
    }

    [Fact]
    public async Task AC_39b_una_visita_agendada_para_dentro_de_3_dias_no_aparece()
    {
        _hogar.Fijar(Hoy, 9);

        await _hogar.Puntual(_firulais, TipoDeber.VisitaEstudio, Hoy.AddDays(3), "10:00", SubtipoVisita.Veterinario);

        Assert.Empty(await Instancias());
    }

    [Fact]
    public async Task AC_17d_un_bano_de_hoy_con_la_hora_ya_pasada_aparece_no_cumplido()
    {
        _hogar.Fijar(Hoy, 10, 30);

        await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy, "10:00");

        Assert.Equal(["10 10:00 Bano NoCumplido"], await Instancias());
    }

    [Fact]
    public async Task FR_023_el_bano_queda_no_cumplido_apenas_pasa_su_hora()
    {
        _hogar.Fijar(Hoy, 9);
        await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy, "15:00");

        _hogar.Fijar(Hoy, 15);
        Assert.Equal(["10 15:00 Bano Ninguna"], await Instancias());

        _hogar.Fijar(Hoy, 15, 1);
        Assert.Equal(["10 15:00 Bano NoCumplido"], await Instancias());
    }

    [Fact]
    public async Task AC_40_la_visita_muestra_su_subtipo()
    {
        _hogar.Fijar(Hoy, 9);

        await _hogar.Puntual(_firulais, TipoDeber.VisitaEstudio, Hoy, "16:00", SubtipoVisita.Vacuna);

        Assert.Equal(["10 16:00 VisitaEstudio/Vacuna Ninguna"], await Instancias());
    }

    [Fact]
    public async Task AC_45_AC_46_una_visita_de_hace_3_dias_sin_cumplir_aparece_no_cumplido_hasta_cumplirla()
    {
        var haceTresDias = Hoy.AddDays(-3);
        _hogar.Fijar(haceTresDias, 8);
        var visita = await _hogar.Puntual(_firulais, TipoDeber.VisitaEstudio, haceTresDias, "10:00", SubtipoVisita.Estudio);

        _hogar.Fijar(Hoy, 9);
        Assert.Equal(["07 10:00 VisitaEstudio/Estudio NoCumplido"], await Instancias());

        await _hogar.CumplirOk((await _hogar.Instancia(visita, haceTresDias, "10:00")).Id);
        Assert.Empty(await Instancias());
    }

    [Fact]
    public async Task AC_33_AC_33b_paseo_y_bano_del_dia_juntos_y_al_cumplir_el_bano_queda_el_paseo()
    {
        _hogar.Fijar(Hoy, 9);
        await _hogar.Programable(_firulais, TipoDeber.Paseo, "18:00", recurrencia: Recurrencia.SoloHoy);
        var bano = await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy, "15:00");

        _hogar.Fijar(Hoy, 12);
        Assert.Equal(["10 15:00 Bano Ninguna", "10 18:00 Paseo Ninguna"], await Instancias());

        await _hogar.CumplirOk((await _hogar.Instancia(bano, Hoy, "15:00")).Id);
        Assert.Equal(["10 18:00 Paseo Ninguna"], await Instancias());
    }

    [Fact]
    public async Task AC_36_cumplir_un_bano_no_pide_hora_y_lo_saca_del_estado_del_dia()
    {
        _hogar.Fijar(Hoy, 9);
        var bano = await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy, "10:00");
        _hogar.Fijar(Hoy, 10, 20);

        var registroId = await _hogar.CumplirOk((await _hogar.Instancia(bano, Hoy, "10:00")).Id);

        Assert.Empty(await Instancias());
        using var db = _hogar.Base.Contexto();
        Assert.Null(db.RegistrosCumplimiento.Single(r => r.Id == registroId).Hora);
    }

    [Fact]
    public async Task AC_39_escenario_combinado_del_estado_del_dia()
    {
        _hogar.Fijar(Ayer, 7);
        await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00");
        await _hogar.Puntual(_firulais, TipoDeber.VisitaEstudio, Ayer, "16:00", SubtipoVisita.Veterinario);
        _hogar.Fijar(Hoy, 0, 1);
        await _hogar.Ciclo();
        _hogar.Fijar(Hoy, 7);
        var paseo1 = await _hogar.Programable(_firulais, TipoDeber.Paseo, "08:00", recurrencia: Recurrencia.SoloHoy);
        var paseo2 = await _hogar.Programable(_firulais, TipoDeber.Paseo, "11:00", recurrencia: Recurrencia.SoloHoy);
        var comida = await _hogar.Programable(_firulais, TipoDeber.Comida, "13:00", recurrencia: Recurrencia.SoloHoy);
        _hogar.Fijar(Hoy, 8, 30);
        await _hogar.CumplirOk((await _hogar.Instancia(paseo1, Hoy, "08:00")).Id);
        _hogar.Fijar(Hoy, 11);
        await _hogar.CumplirOk((await _hogar.Instancia(paseo2, Hoy, "11:00")).Id);
        _hogar.Fijar(Hoy, 13);
        await _hogar.CumplirOk((await _hogar.Instancia(comida, Hoy, "13:00")).Id);

        _hogar.Fijar(Hoy, 14);
        var seccion = await _hogar.Seccion(_firulais);

        Assert.Equal(2, seccion.Resumen.PaseosDeHoy);
        Assert.Equal(new Momento(Hoy, new TimeOnly(11, 0)), seccion.Resumen.UltimoPaseo);
        Assert.Equal(new Momento(Hoy, new TimeOnly(13, 0)), seccion.Resumen.UltimaComida);
        Assert.Contains("09 09:00 Medicacion Pendiente", seccion.Instancias.Select(Ver));
        Assert.Contains("09 16:00 VisitaEstudio/Veterinario NoCumplido", seccion.Instancias.Select(Ver));
    }
}
