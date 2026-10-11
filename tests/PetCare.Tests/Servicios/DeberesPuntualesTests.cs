using Microsoft.EntityFrameworkCore;
using PetCare.Tests.Infra;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Servicios;

/// <summary>Alta de baños y visitas/estudios (FR-011).</summary>
public class DeberesPuntualesTests : IDisposable
{
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private static readonly DateOnly Hoy = new(2026, 10, 10);
    private static readonly DateOnly Manana = new(2026, 10, 11);

    private readonly Escenario _hogar = new();
    private readonly int _firulais;

    public DeberesPuntualesTests() => _firulais = _hogar.Mascota();

    public void Dispose() => _hogar.Dispose();

    [Fact]
    public async Task AC_17b_un_bano_agendado_para_ayer_se_rechaza_y_no_aparece_en_la_configuracion()
    {
        _hogar.Fijar(Hoy, 9);

        var resultado = await _hogar.AltaPuntual(_firulais, TipoDeber.Bano, Ayer, "15:00");

        Assert.Equal("La fecha no puede ser anterior a hoy.", resultado.Error);
        Assert.Empty(await _hogar.DeberesActivos(_firulais));
    }

    [Fact]
    public async Task AC_17c_una_visita_agendada_para_ayer_se_rechaza_y_no_aparece_en_la_configuracion()
    {
        _hogar.Fijar(Hoy, 9);

        var resultado = await _hogar.AltaPuntual(_firulais, TipoDeber.VisitaEstudio, Ayer, "15:00", SubtipoVisita.Veterinario);

        Assert.Equal("La fecha no puede ser anterior a hoy.", resultado.Error);
        Assert.Empty(await _hogar.DeberesActivos(_firulais));
    }

    [Fact]
    public async Task AC_17d_un_bano_de_hoy_con_la_hora_ya_pasada_se_acepta_y_nace_vencido()
    {
        _hogar.Fijar(Hoy, 10, 30);

        var bano = await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy, "10:00");

        Assert.True((await _hogar.Instancia(bano, Hoy, "10:00")).GeneradaVencida);
    }

    [Fact]
    public async Task FR_011_el_alta_crea_una_unica_instancia_en_su_fecha_y_hora_sin_marca_si_no_paso()
    {
        _hogar.Fijar(Hoy, 9);

        var bano = await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy.AddDays(10), "15:00");

        var instancia = await _hogar.Instancia(bano, Hoy.AddDays(10), "15:00");
        Assert.False(instancia.GeneradaVencida);
        using var db = _hogar.Base.Contexto();
        Assert.Equal(1, await db.Instancias.CountAsync(i => i.DeberId == bano));
    }

    [Theory]
    [InlineData(SubtipoVisita.Veterinario)]
    [InlineData(SubtipoVisita.Estudio)]
    [InlineData(SubtipoVisita.Vacuna)]
    [InlineData(SubtipoVisita.IntervencionMedica)]
    public async Task AC_18_los_cuatro_subtipos_se_aceptan_y_se_guardan(SubtipoVisita subtipo)
    {
        _hogar.Fijar(Hoy, 9);

        var visita = await _hogar.Puntual(_firulais, TipoDeber.VisitaEstudio, Manana, "10:00", subtipo);

        Assert.Equal(subtipo, Assert.Single(await _hogar.DeberesActivos(_firulais), d => d.Id == visita).Subtipo);
    }

    [Fact]
    public async Task AC_18b_una_visita_sin_subtipo_se_rechaza_informando_el_motivo()
    {
        _hogar.Fijar(Hoy, 9);

        var resultado = await _hogar.AltaPuntual(_firulais, TipoDeber.VisitaEstudio, Manana, "10:00");

        Assert.Equal("Elegí el subtipo: veterinario, estudio, vacuna o intervención médica.", resultado.Error);
        Assert.Empty(await _hogar.DeberesActivos(_firulais));
    }

    [Fact]
    public async Task FR_011_el_subtipo_solo_aplica_a_visita_estudio()
    {
        _hogar.Fijar(Hoy, 9);

        await _hogar.Puntual(_firulais, TipoDeber.Bano, Manana, "10:00", SubtipoVisita.Vacuna);

        Assert.Null(Assert.Single(await _hogar.DeberesActivos(_firulais)).Subtipo);
    }

    [Fact]
    public async Task AC_19b_parte_bano_la_configuracion_muestra_el_bano_una_vez_con_su_fecha_y_hora()
    {
        _hogar.Fijar(Hoy, 9);
        await _hogar.Programable(_firulais, TipoDeber.Paseo, "08:00", cadaNHoras: 4);

        await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy.AddDays(10), "15:00");

        var activos = await _hogar.DeberesActivos(_firulais);
        Assert.Equal(2, activos.Count);
        var bano = Assert.Single(activos, d => d.Tipo == TipoDeber.Bano);
        Assert.Equal((Hoy.AddDays(10), new TimeOnly(15, 0)), (bano.Dia!.Value, bano.Hora!.Value));
    }

    [Fact]
    public async Task AC_19d_un_bano_de_hoy_cumplido_ya_no_esta_activo()
    {
        _hogar.Fijar(Hoy, 9);
        var bano = await _hogar.Puntual(_firulais, TipoDeber.Bano, Hoy, "10:00");
        _hogar.Fijar(Hoy, 10, 5);

        await _hogar.CumplirOk((await _hogar.Instancia(bano, Hoy, "10:00")).Id);

        Assert.Empty(await _hogar.DeberesActivos(_firulais));
    }

    [Fact]
    public async Task AC_16_una_vacuna_cumplida_hoy_no_aparece_manana()
    {
        _hogar.Fijar(Hoy, 9);
        var vacuna = await _hogar.Puntual(_firulais, TipoDeber.VisitaEstudio, Hoy, "10:00", SubtipoVisita.Vacuna);
        _hogar.Fijar(Hoy, 10, 5);
        await _hogar.CumplirOk((await _hogar.Instancia(vacuna, Hoy, "10:00")).Id);

        _hogar.Fijar(Manana, 9);
        await _hogar.Ciclo();

        Assert.Empty((await _hogar.Seccion(_firulais)).Instancias);
    }
}
