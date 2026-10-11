using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

/// <summary>Baja de deberes e instancias (FR-013, FR-015, FR-016).</summary>
public class BajasTests : IDisposable
{
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private static readonly DateOnly Hoy = new(2026, 10, 10);
    private static readonly DateOnly Manana = new(2026, 10, 11);

    private readonly Escenario _hogar = new();
    private readonly int _firulais;

    public BajasTests() => _firulais = _hogar.Mascota();

    public void Dispose() => _hogar.Dispose();

    private async Task<string[]> Instancias() =>
        (await _hogar.Seccion(_firulais)).Instancias.Select(i => $"{i.Dia:dd} {i.Hora:HH:mm} {i.Tipo}").ToArray();

    private async Task<EstadoInstancia> Estado(int deberId, DateOnly dia, string hora) =>
        (await _hogar.Instancia(deberId, dia, hora)).Estado;

    [Fact]
    public async Task AC_20d_dar_de_baja_una_visita_de_manana_la_saca_de_la_configuracion_y_del_estado_de_manana()
    {
        _hogar.Fijar(Hoy, 9);
        var visita = await _hogar.Puntual(_firulais, TipoDeber.VisitaEstudio, Manana, "10:00", SubtipoVisita.Veterinario);

        await _hogar.DarDeBajaDeber(visita);

        Assert.Empty(await _hogar.DeberesActivos(_firulais));
        _hogar.Fijar(Manana, 9);
        await _hogar.Ciclo();
        Assert.Empty(await Instancias());
        Assert.Equal(EstadoInstancia.DadaDeBaja, await Estado(visita, Manana, "10:00"));
    }

    [Fact]
    public async Task AC_24_dar_de_baja_un_paseo_diario_lo_saca_de_la_configuracion_y_manana_no_hay_instancias()
    {
        _hogar.Fijar(Hoy, 7);
        await _hogar.Programable(_firulais, TipoDeber.Paseo, "09:00");

        await _hogar.DarDeBajaDeber((await _hogar.DeberesActivos(_firulais)).Single().Id);

        Assert.Empty(await _hogar.DeberesActivos(_firulais));
        _hogar.Fijar(Manana, 8);
        await _hogar.Ciclo();
        Assert.Empty(await Instancias());
    }

    [Fact]
    public async Task AC_20b_parte_pantalla_dar_de_baja_la_medicacion_saca_las_instancias_de_ayer_y_de_hoy()
    {
        _hogar.Fijar(Ayer, 7);
        var medicacion = await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00");
        _hogar.Fijar(Hoy, 0, 1);
        await _hogar.Ciclo();
        _hogar.Fijar(Hoy, 8);
        Assert.Equal(2, (await Instancias()).Length);

        await _hogar.DarDeBajaDeber(medicacion);

        Assert.Empty(await Instancias());
        Assert.Equal(EstadoInstancia.DadaDeBaja, await Estado(medicacion, Ayer, "09:00"));
        Assert.Equal(EstadoInstancia.DadaDeBaja, await Estado(medicacion, Hoy, "09:00"));
    }

    [Fact]
    public async Task AC_25_dar_de_baja_el_deber_conserva_sus_registros_y_el_paseo_sigue_contado()
    {
        _hogar.Fijar(Hoy, 7);
        var paseo = await _hogar.Programable(_firulais, TipoDeber.Paseo, "09:00");
        _hogar.Fijar(Hoy, 9, 5);
        await _hogar.CumplirOk((await _hogar.Instancia(paseo, Hoy, "09:00")).Id);

        _hogar.Fijar(Hoy, 12);
        await _hogar.DarDeBajaDeber(paseo);

        Assert.Equal(1, (await _hogar.Seccion(_firulais)).Resumen.PaseosDeHoy);
        Assert.Equal(EstadoInstancia.Cumplida, await Estado(paseo, Hoy, "09:00"));
    }

    [Fact]
    public async Task AC_20_dar_de_baja_la_instancia_de_un_bano_da_de_baja_el_deber()
    {
        _hogar.Fijar(Hoy, 9);
        var bano = await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy, "15:00");
        _hogar.Fijar(Hoy, 10);

        var resultado = await _hogar.DarDeBajaInstancia((await _hogar.Instancia(bano, Hoy, "15:00")).Id);

        Assert.True(resultado.Ok, resultado.Error);
        Assert.Empty(await Instancias());
        Assert.Empty(await _hogar.DeberesActivos(_firulais));
    }

    [Fact]
    public async Task AC_20c_dar_de_baja_la_instancia_de_una_comida_solo_para_hoy_da_de_baja_el_deber()
    {
        _hogar.Fijar(Hoy, 9);
        var comida = await _hogar.Programable(_firulais, TipoDeber.Comida, "20:00", recurrencia: Recurrencia.SoloHoy);
        _hogar.Fijar(Hoy, 10);

        await _hogar.DarDeBajaInstancia((await _hogar.Instancia(comida, Hoy, "20:00")).Id);

        Assert.Empty(await Instancias());
        Assert.Empty(await _hogar.DeberesActivos(_firulais));
        using var db = _hogar.Base.Contexto();
        Assert.NotNull(db.Deberes.Single(d => d.Id == comida).DadoDeBajaUtc);
    }

    [Fact]
    public async Task AC_23_AC_23b_dar_de_baja_la_instancia_de_las_12_conserva_el_deber_y_manana_vuelven_las_cuatro()
    {
        _hogar.Fijar(Hoy, 6);
        var paseo = await _hogar.Programable(_firulais, TipoDeber.Paseo, "08:00", cadaNHoras: 4);
        _hogar.Fijar(Hoy, 7);

        await _hogar.DarDeBajaInstancia((await _hogar.Instancia(paseo, Hoy, "12:00")).Id);

        Assert.Equal(["10 08:00 Paseo", "10 16:00 Paseo", "10 20:00 Paseo"], await Instancias());
        Assert.Equal(paseo, Assert.Single(await _hogar.DeberesActivos(_firulais)).Id);

        _hogar.Fijar(Manana, 0, 1);
        await _hogar.Ciclo();
        _hogar.Fijar(Manana, 7);
        Assert.Equal(["11 08:00 Paseo", "11 12:00 Paseo", "11 16:00 Paseo", "11 20:00 Paseo"], await Instancias());
    }
}
