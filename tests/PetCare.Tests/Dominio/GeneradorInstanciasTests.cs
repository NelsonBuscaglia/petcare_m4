using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Tests.Dominio;

public class GeneradorInstanciasTests
{
    private static readonly DateOnly DiaDelAlta = new(2026, 10, 10);

    private static Deber Programable(TipoDeber tipo, TimeOnly hora, int? cadaNHoras = null,
        Recurrencia recurrencia = Recurrencia.TodosLosDias, DateOnly? hasta = null) => new()
    {
        Tipo = tipo,
        Modalidad = cadaNHoras is null ? Modalidad.HoraFija : Modalidad.CadaNHoras,
        HoraInicio = hora,
        IntervaloHoras = cadaNHoras,
        Recurrencia = recurrencia,
        HastaDia = hasta,
        CreadoDia = DiaDelAlta,
    };

    private static (DateOnly, TimeOnly)[] En(DateOnly dia, params string[] horas) =>
        horas.Select(h => (dia, TimeOnly.Parse(h))).ToArray();

    [Fact]
    public void AC_10_RF_07_cada_4_horas_desde_las_08_genera_08_12_16_y_20()
    {
        var paseo = Programable(TipoDeber.Paseo, new(8, 0), cadaNHoras: 4);

        Assert.Equal(En(DiaDelAlta, "08:00", "12:00", "16:00", "20:00"), GeneradorInstancias.Generar(paseo, DiaDelAlta));
    }

    [Theory]
    [InlineData("08:30", 4, new[] { "08:30", "12:30", "16:30", "20:30" })]
    [InlineData("08:00", 5, new[] { "08:00", "13:00", "18:00", "23:00" })]
    [InlineData("22:00", 23, new[] { "22:00" })]
    [InlineData("23:59", 1, new[] { "23:59" })]
    public void FR_014_la_serie_cada_N_horas_se_corta_al_terminar_el_dia(string inicio, int n, string[] esperadas)
    {
        var comida = Programable(TipoDeber.Comida, TimeOnly.Parse(inicio), cadaNHoras: n);

        Assert.Equal(En(DiaDelAlta, esperadas), GeneradorInstancias.Generar(comida, DiaDelAlta));
    }

    [Fact]
    public void FR_014_cada_1_hora_desde_las_00_genera_24_instancias()
    {
        var comida = Programable(TipoDeber.Comida, new(0, 0), cadaNHoras: 1);

        Assert.Equal(24, GeneradorInstancias.Generar(comida, DiaDelAlta).Count);
    }

    [Fact]
    public void RF_06a_hora_fija_genera_una_sola_instancia()
    {
        var paseo = Programable(TipoDeber.Paseo, new(9, 0));

        Assert.Equal(En(DiaDelAlta, "09:00"), GeneradorInstancias.Generar(paseo, DiaDelAlta));
    }

    [Fact]
    public void AC_11_todos_los_dias_genera_tambien_el_dia_siguiente()
    {
        var paseo = Programable(TipoDeber.Paseo, new(9, 0));
        var manana = DiaDelAlta.AddDays(1);

        Assert.Equal(En(manana, "09:00"), GeneradorInstancias.Generar(paseo, manana));
    }

    [Fact]
    public void AC_12_solo_para_hoy_genera_el_dia_del_alta_y_no_el_siguiente()
    {
        var comida = Programable(TipoDeber.Comida, new(20, 0), recurrencia: Recurrencia.SoloHoy);

        Assert.Equal(En(DiaDelAlta, "20:00"), GeneradorInstancias.Generar(comida, DiaDelAlta));
        Assert.Empty(GeneradorInstancias.Generar(comida, DiaDelAlta.AddDays(1)));
    }

    [Fact]
    public void AC_13_AC_13b_hasta_una_fecha_genera_ese_dia_y_no_el_siguiente()
    {
        var dia10 = DiaDelAlta.AddDays(5);
        var medicacion = Programable(TipoDeber.Medicacion, new(9, 0), hasta: dia10);

        Assert.Equal(En(dia10, "09:00"), GeneradorInstancias.Generar(medicacion, dia10));
        Assert.Empty(GeneradorInstancias.Generar(medicacion, dia10.AddDays(1)));
    }

    [Fact]
    public void AC_15_para_siempre_sigue_generando()
    {
        var medicacion = Programable(TipoDeber.Medicacion, new(9, 0));

        Assert.Equal(En(DiaDelAlta.AddDays(30), "09:00"), GeneradorInstancias.Generar(medicacion, DiaDelAlta.AddDays(30)));
        Assert.Equal(En(DiaDelAlta.AddDays(1000), "09:00"), GeneradorInstancias.Generar(medicacion, DiaDelAlta.AddDays(1000)));
    }

    [Fact]
    public void No_genera_instancias_antes_del_dia_del_alta()
    {
        var paseo = Programable(TipoDeber.Paseo, new(9, 0));

        Assert.Empty(GeneradorInstancias.Generar(paseo, DiaDelAlta.AddDays(-1)));
    }
}
