using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Tests.Dominio;

/// <summary>Marcas calculadas al leer: vencida y expirada (data-model, R3).</summary>
public class ReglasVencimientoTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);
    private static readonly DateOnly Ayer = Hoy.AddDays(-1);

    private static Instancia Instancia(DateOnly dia, string hora, EstadoInstancia estado = EstadoInstancia.Vigente)
    {
        var horaDelDia = TimeOnly.Parse(hora);
        return new Instancia { Dia = dia, Hora = horaDelDia, ProgramadaUtc = RelojHogar.AUtc(dia, horaDelDia), Estado = estado };
    }

    private static DateTime A(DateOnly dia, string hora) => RelojHogar.AUtc(dia, TimeOnly.Parse(hora));

    [Theory]
    [InlineData(TipoDeber.Comida)]
    [InlineData(TipoDeber.Paseo)]
    [InlineData(TipoDeber.Medicacion)]
    public void AC_29_a_los_10_minutos_todavia_no_esta_vencida(TipoDeber tipo)
    {
        Assert.False(ReglasVencimiento.EstaVencida(Instancia(Hoy, "09:00"), tipo, A(Hoy, "09:10")));
    }

    [Theory]
    [InlineData(TipoDeber.Comida)]
    [InlineData(TipoDeber.Paseo)]
    [InlineData(TipoDeber.Medicacion)]
    public void AC_29b_a_los_15_minutos_esta_vencida(TipoDeber tipo)
    {
        Assert.True(ReglasVencimiento.EstaVencida(Instancia(Hoy, "09:00"), tipo, A(Hoy, "09:15")));
    }

    [Fact]
    public void FR_019_un_segundo_antes_de_los_15_minutos_todavia_no_esta_vencida()
    {
        var ahora = A(Hoy, "09:15").AddSeconds(-1);

        Assert.False(ReglasVencimiento.EstaVencida(Instancia(Hoy, "09:00"), TipoDeber.Medicacion, ahora));
    }

    [Fact]
    public void AC_44_el_paseo_de_las_12_a_las_12_20_esta_vencido()
    {
        Assert.True(ReglasVencimiento.EstaVencida(Instancia(Hoy, "12:00"), TipoDeber.Paseo, A(Hoy, "12:20")));
    }

    [Fact]
    public void AC_61_a_las_05_15_del_hogar_una_comida_de_las_08_no_esta_vencida()
    {
        // 05:15 en UTC−3 son las 08:15 UTC: la hora de la comida se interpreta en hora del hogar.
        var ahora = new DateTime(2026, 10, 10, 8, 15, 0, DateTimeKind.Utc);

        Assert.False(ReglasVencimiento.EstaVencida(Instancia(Hoy, "08:00"), TipoDeber.Comida, ahora));
    }

    [Theory]
    [InlineData(EstadoInstancia.Cumplida)]
    [InlineData(EstadoInstancia.DadaDeBaja)]
    public void FR_019_una_instancia_cumplida_o_dada_de_baja_no_esta_vencida(EstadoInstancia estado)
    {
        Assert.False(ReglasVencimiento.EstaVencida(Instancia(Hoy, "09:00", estado), TipoDeber.Medicacion, A(Hoy, "12:00")));
    }

    [Theory]
    [InlineData(TipoDeber.Comida)]
    [InlineData(TipoDeber.Paseo)]
    public void AC_30_comida_y_paseo_de_ayer_no_cumplidos_expiran(TipoDeber tipo)
    {
        Assert.True(ReglasVencimiento.EstaExpirada(Instancia(Ayer, "08:00"), tipo, Hoy));
    }

    [Theory]
    [InlineData(TipoDeber.Comida)]
    [InlineData(TipoDeber.Paseo)]
    public void FR_021_comida_y_paseo_de_hoy_no_expiran_aunque_esten_vencidos(TipoDeber tipo)
    {
        Assert.False(ReglasVencimiento.EstaExpirada(Instancia(Hoy, "08:00"), tipo, Hoy));
    }

    [Fact]
    public void FR_022_la_medicacion_no_expira()
    {
        Assert.False(ReglasVencimiento.EstaExpirada(Instancia(Ayer.AddDays(-5), "09:00"), TipoDeber.Medicacion, Hoy));
    }

    [Fact]
    public void FR_021_una_comida_de_ayer_ya_cumplida_no_esta_expirada()
    {
        Assert.False(ReglasVencimiento.EstaExpirada(Instancia(Ayer, "08:00", EstadoInstancia.Cumplida), TipoDeber.Comida, Hoy));
    }
}
