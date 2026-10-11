using Microsoft.Extensions.Time.Testing;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Dominio;

public class RelojHogarTests
{
    private static RelojHogar RelojEn(DateTimeOffset utc) => new(new FakeTimeProvider(utc));

    [Fact]
    public void FR_044_la_hora_del_hogar_es_UTC_menos_3()
    {
        var reloj = RelojEn(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 10), reloj.Hoy);
        Assert.Equal(new TimeOnly(9, 0), reloj.HoraActual);
    }

    [Fact]
    public void FR_044_no_hay_horario_de_verano()
    {
        // Enero (verano austral) y julio (invierno) dan el mismo offset.
        var enero = RelojEn(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var julio = RelojEn(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(new TimeOnly(9, 0), enero.HoraActual);
        Assert.Equal(new TimeOnly(9, 0), julio.HoraActual);
    }

    [Fact]
    public void AC_28_a_las_03_05_UTC_ya_son_las_00_05_del_nuevo_dia()
    {
        var reloj = RelojEn(new DateTimeOffset(2026, 10, 11, 3, 5, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 11), reloj.Hoy);
        Assert.Equal(new TimeOnly(0, 5), reloj.HoraActual);
    }

    [Fact]
    public void AC_28b_a_las_02_59_UTC_todavia_son_las_23_59_del_dia_anterior()
    {
        var reloj = RelojEn(new DateTimeOffset(2026, 10, 11, 2, 59, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 10), reloj.Hoy);
        Assert.Equal(new TimeOnly(23, 59), reloj.HoraActual);
    }

    [Fact]
    public void AhoraUtc_sale_del_TimeProvider_inyectado()
    {
        var tiempo = new FakeTimeProvider(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));
        var reloj = new RelojHogar(tiempo);

        tiempo.Advance(TimeSpan.FromMinutes(15));

        Assert.Equal(new DateTime(2026, 10, 10, 12, 15, 0, DateTimeKind.Utc), reloj.AhoraUtc);
        Assert.Equal(DateTimeKind.Utc, reloj.AhoraUtc.Kind);
    }

    [Fact]
    public void AUtc_convierte_dia_y_hora_del_hogar_a_un_instante_UTC()
    {
        var utc = RelojHogar.AUtc(new DateOnly(2026, 10, 10), new TimeOnly(22, 30));

        Assert.Equal(new DateTime(2026, 10, 11, 1, 30, 0, DateTimeKind.Utc), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    [Fact]
    public void ADiaHora_convierte_un_instante_UTC_a_dia_y_hora_del_hogar()
    {
        var (dia, hora) = RelojHogar.ADiaHora(new DateTime(2026, 10, 11, 1, 30, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(2026, 10, 10), dia);
        Assert.Equal(new TimeOnly(22, 30), hora);
    }

    [Theory]
    [InlineData(2026, 10, 10, 0, 0)]
    [InlineData(2026, 10, 10, 2, 59)]
    [InlineData(2026, 10, 10, 21, 0)]
    [InlineData(2026, 12, 31, 23, 59)]
    public void Dia_y_hora_ida_y_vuelta_dan_el_mismo_valor(int anio, int mes, int dia, int hora, int minuto)
    {
        var original = (new DateOnly(anio, mes, dia), new TimeOnly(hora, minuto));

        Assert.Equal(original, RelojHogar.ADiaHora(RelojHogar.AUtc(original.Item1, original.Item2)));
    }
}
