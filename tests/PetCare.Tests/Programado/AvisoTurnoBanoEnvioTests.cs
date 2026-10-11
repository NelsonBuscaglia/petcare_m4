using PetCare.Tests.Infra;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Programado;

/// <summary>Envío diario a las 07:00 del aviso de turno de baño (FR-040, FR-054).</summary>
public class AvisoTurnoBanoEnvioTests : IDisposable
{
    private static readonly DateOnly Hoy = new(2026, 11, 10);
    private static readonly DateOnly Manana = Hoy.AddDays(1);

    private readonly Escenario _hogar = new();

    public AvisoTurnoBanoEnvioTests()
    {
        _hogar.Cuidador("ana@ejemplo.com");
        _hogar.Cuidador("beto@ejemplo.com");
    }

    public void Dispose() => _hogar.Dispose();

    private static DateTimeOffset A(DateOnly dia, int hora, int minuto = 0) => new(RelojHogar.AUtc(dia, new TimeOnly(hora, minuto)));

    private IReadOnlyList<EmailEnviado> Avisos() => _hogar.Emails.Enviados.Where(e => e.Asunto.Contains("turno de baño")).ToList();

    private void Maximo(int mascotaId, int dias)
    {
        using var db = _hogar.Base.Contexto();
        db.Mascotas.Single(m => m.Id == mascotaId).MaxDiasEntreBanos = dias;
        db.SaveChanges();
    }

    /// <summary>Firulais con máximo 20 días y un baño cumplido hace 25 días (escenario del AC-53).</summary>
    private async Task<int> FirulaisConUltimoBanoHace25Dias()
    {
        var haceVeinticinco = Hoy.AddDays(-25);
        _hogar.Fijar(haceVeinticinco, 9);
        var firulais = _hogar.Mascota();
        Maximo(firulais, 20);
        var bano = await _hogar.Puntual(firulais, TipoDeber.Bano, haceVeinticinco, "10:00");
        _hogar.Fijar(haceVeinticinco, 10, 30);
        await _hogar.CumplirOk((await _hogar.Instancia(bano, haceVeinticinco, "10:00")).Id);
        return firulais;
    }

    /// <summary>Ciclos alrededor de las 07:00 y algunos más en el resto del día.</summary>
    private async Task UnDia(DateOnly dia)
    {
        _hogar.Fijar(dia, 6, 55);
        await _hogar.CiclosHasta(dia, 7, 10);
        _hogar.Fijar(dia, 12);
        await _hogar.Ciclo();
        _hogar.Fijar(dia, 23, 59);
        await _hogar.Ciclo();
    }

    [Fact]
    public async Task AC_53b_el_aviso_llega_a_todos_a_mas_tardar_a_las_07_05()
    {
        await FirulaisConUltimoBanoHace25Dias();

        _hogar.Fijar(Hoy, 6, 55);
        await _hogar.CiclosHasta(Hoy, 7, 5);

        var aviso = Assert.Single(Avisos());
        Assert.Equal(["ana@ejemplo.com", "beto@ejemplo.com"], aviso.Destinatarios.Order());
        Assert.InRange(aviso.Momento, A(Hoy, 7), A(Hoy, 7, 5));
        Assert.Equal("PetCare: hay que pedir turno de baño para Firulais", aviso.Asunto);
    }

    [Fact]
    public async Task AC_53_se_envia_exactamente_un_aviso_en_el_dia()
    {
        await FirulaisConUltimoBanoHace25Dias();

        await UnDia(Hoy);

        Assert.Single(Avisos());
    }

    [Fact]
    public async Task AC_58_si_nada_cambia_el_aviso_se_repite_al_dia_siguiente()
    {
        await FirulaisConUltimoBanoHace25Dias();
        await UnDia(Hoy);

        await UnDia(Manana);

        Assert.Equal(2, Avisos().Count);
        Assert.InRange(Avisos()[1].Momento, A(Manana, 7), A(Manana, 7, 5));
    }

    [Fact]
    public async Task AC_53c_si_se_da_de_baja_el_bano_futuro_a_las_10_el_aviso_llega_recien_al_dia_siguiente()
    {
        var firulais = await FirulaisConUltimoBanoHace25Dias();
        _hogar.Fijar(Hoy.AddDays(-1), 9);
        var banoFuturo = await _hogar.Puntual(firulais, TipoDeber.Bano, Hoy.AddDays(3), "15:00");

        _hogar.Fijar(Hoy, 6, 55);
        await _hogar.CiclosHasta(Hoy, 10);
        await _hogar.DarDeBajaDeber(banoFuturo);
        await _hogar.CiclosHasta(Hoy, 10, 10);
        _hogar.Fijar(Hoy, 23, 59);
        await _hogar.Ciclo();
        Assert.Empty(Avisos());

        _hogar.Fijar(Manana, 6, 55);
        await _hogar.CiclosHasta(Manana, 7, 5);

        var aviso = Assert.Single(Avisos());
        Assert.InRange(aviso.Momento, A(Manana, 7), A(Manana, 7, 5));
    }

    [Fact]
    public async Task AC_54c_un_bano_no_cumplido_de_hace_2_dias_cuenta_como_activo_y_no_hay_aviso()
    {
        var firulais = await FirulaisConUltimoBanoHace25Dias();
        _hogar.Fijar(Hoy.AddDays(-3), 9);
        await _hogar.Puntual(firulais, TipoDeber.Bano, Hoy.AddDays(-2), "15:00");

        await UnDia(Hoy);

        Assert.Empty(Avisos());
    }

    [Fact]
    public async Task AC_64c_el_aviso_indica_la_mascota_el_maximo_y_la_fecha_del_ultimo_bano()
    {
        await FirulaisConUltimoBanoHace25Dias();

        _hogar.Fijar(Hoy, 6, 55);
        await _hogar.CiclosHasta(Hoy, 7, 5);

        var cuerpo = Assert.Single(Avisos()).Cuerpo;
        Assert.Contains("Firulais", cuerpo);
        Assert.Contains("20 días", cuerpo);
        Assert.Contains(Hoy.AddDays(-25).ToString("dd/MM/yyyy"), cuerpo);
    }

    [Fact]
    public async Task AC_56_AC_64d_sin_banos_registrados_se_cuenta_desde_el_alta_y_el_aviso_lo_indica()
    {
        _hogar.Fijar(Hoy.AddDays(-25), 9);
        var firulais = _hogar.Mascota();
        Maximo(firulais, 20);

        await UnDia(Hoy);

        var aviso = Assert.Single(Avisos());
        Assert.Contains("no hay baños registrados", aviso.Cuerpo);
    }
}
