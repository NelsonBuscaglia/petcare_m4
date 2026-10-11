using Microsoft.EntityFrameworkCore;
using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

public class DeberesProgramablesTests : IDisposable
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);
    private static readonly DateOnly Ayer = Hoy.AddDays(-1);

    private readonly BaseDeDatosDePrueba _base = new();
    private readonly int _firulais;

    public DeberesProgramablesTests()
    {
        Fijar(Hoy, 7);
        Semillas.Hogar(_base);
        _firulais = Semillas.Mascota(_base, "Firulais");
    }

    public void Dispose() => _base.Dispose();

    private void Fijar(DateOnly dia, int hora, int minuto = 0) => _base.Fijar(dia, new TimeOnly(hora, minuto));

    private async Task<Resultado<int>> Alta(int mascotaId, TipoDeber tipo, string hora, int? cadaNHoras = null,
        Recurrencia recurrencia = Recurrencia.TodosLosDias, DateOnly? hasta = null)
    {
        await using var db = _base.Contexto();
        return await new DeberesService(db, _base.Reloj).AltaProgramableAsync(new AltaProgramable(
            mascotaId, tipo, cadaNHoras is null ? Modalidad.HoraFija : Modalidad.CadaNHoras,
            TimeOnly.Parse(hora), cadaNHoras, recurrencia, hasta));
    }

    private async Task<int> AltaOk(int mascotaId, TipoDeber tipo, string hora, int? cadaNHoras = null,
        Recurrencia recurrencia = Recurrencia.TodosLosDias, DateOnly? hasta = null)
    {
        var resultado = await Alta(mascotaId, tipo, hora, cadaNHoras, recurrencia, hasta);
        Assert.True(resultado.Ok, resultado.Error);
        return resultado.Valor;
    }

    private async Task<IReadOnlyList<Deber>> Activos(int mascotaId)
    {
        await using var db = _base.Contexto();
        return await new DeberesService(db, _base.Reloj).DeberesActivosAsync(mascotaId);
    }

    private List<Instancia> InstanciasDe(int deberId)
    {
        using var db = _base.Contexto();
        return db.Instancias.AsNoTracking().Where(i => i.DeberId == deberId).OrderBy(i => i.ProgramadaUtc).ToList();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    public async Task AC_10b_AC_10c_AC_63b_N_fuera_de_1_a_23_se_rechaza_y_no_aparece_en_la_configuracion(int n)
    {
        var resultado = await Alta(_firulais, TipoDeber.Paseo, "08:00", cadaNHoras: n);

        Assert.Equal("N debe ser un número entero entre 1 y 23.", resultado.Error);
        Assert.Empty(await Activos(_firulais));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(23)]
    public async Task FR_009_N_entre_1_y_23_se_acepta(int n)
    {
        var resultado = await Alta(_firulais, TipoDeber.Paseo, "08:00", cadaNHoras: n);

        Assert.True(resultado.Ok, resultado.Error);
    }

    [Fact]
    public async Task AC_13d_medicacion_diaria_con_fecha_de_fin_ayer_se_rechaza()
    {
        var resultado = await Alta(_firulais, TipoDeber.Medicacion, "09:00", hasta: Ayer);

        Assert.Equal("La fecha no puede ser anterior a hoy.", resultado.Error);
        Assert.Empty(await Activos(_firulais));
    }

    [Fact]
    public async Task FR_010_medicacion_diaria_con_fecha_de_fin_hoy_se_acepta()
    {
        var resultado = await Alta(_firulais, TipoDeber.Medicacion, "09:00", hasta: Hoy);

        Assert.True(resultado.Ok, resultado.Error);
    }

    [Fact]
    public async Task FR_010_la_fecha_de_fin_solo_aplica_a_la_medicacion_diaria()
    {
        var paseo = await AltaOk(_firulais, TipoDeber.Paseo, "09:00", hasta: Ayer);

        Assert.Null(Assert.Single(await Activos(_firulais)).HastaDia);
        Assert.Single(InstanciasDe(paseo));
    }

    [Fact]
    public async Task AC_14b_RF_17b_el_alta_a_las_13_genera_tambien_las_instancias_de_horarios_pasados()
    {
        Fijar(Hoy, 13);

        var comida = await AltaOk(_firulais, TipoDeber.Comida, "08:00", cadaNHoras: 4, recurrencia: Recurrencia.SoloHoy);

        var instancias = InstanciasDe(comida);
        Assert.Equal(["08:00", "12:00", "16:00", "20:00"], instancias.Select(i => i.Hora.ToString("HH:mm")));
        Assert.All(instancias, i => Assert.Equal(Hoy, i.Dia));
        Assert.All(instancias, i => Assert.Equal(EstadoInstancia.Vigente, i.Estado));
        Assert.All(instancias, i => Assert.Equal(RelojHogar.AUtc(i.Dia, i.Hora), i.ProgramadaUtc));
    }

    [Fact]
    public async Task FR_018_R4_las_instancias_ya_vencidas_al_generarse_quedan_marcadas()
    {
        Fijar(Hoy, 13);

        var comida = await AltaOk(_firulais, TipoDeber.Comida, "08:00", cadaNHoras: 4, recurrencia: Recurrencia.SoloHoy);

        Assert.Equal([true, true, false, false], InstanciasDe(comida).Select(i => i.GeneradaVencida));
    }

    [Fact]
    public async Task FR_018_una_instancia_que_todavia_no_cumplio_15_minutos_no_nace_vencida()
    {
        Fijar(Hoy, 12, 10);

        var paseo = await AltaOk(_firulais, TipoDeber.Paseo, "12:00");

        Assert.False(Assert.Single(InstanciasDe(paseo)).GeneradaVencida);
    }

    [Fact]
    public async Task AC_09_el_deber_de_la_mascota_A_no_genera_instancias_ni_aparece_en_la_mascota_B()
    {
        var b = Semillas.Mascota(_base, "B");

        var paseoDeA = await AltaOk(_firulais, TipoDeber.Paseo, "08:00", cadaNHoras: 4);

        Assert.Empty(await Activos(b));
        using var db = _base.Contexto();
        Assert.Empty(db.Instancias.Where(i => i.Deber!.MascotaId == b));
        Assert.Equal(4, InstanciasDe(paseoDeA).Count);
    }

    [Fact]
    public async Task AC_19b_parte_paseo_la_configuracion_muestra_el_deber_una_vez_con_sus_datos_de_alta()
    {
        await AltaOk(_firulais, TipoDeber.Paseo, "08:00", cadaNHoras: 4);

        var paseo = Assert.Single(await Activos(_firulais));

        Assert.Equal(TipoDeber.Paseo, paseo.Tipo);
        Assert.Equal(Modalidad.CadaNHoras, paseo.Modalidad);
        Assert.Equal(new TimeOnly(8, 0), paseo.HoraInicio);
        Assert.Equal(4, paseo.IntervaloHoras);
        Assert.Equal(Recurrencia.TodosLosDias, paseo.Recurrencia);
    }

    [Fact]
    public async Task AC_19c_una_comida_solo_para_hoy_dada_de_alta_ayer_y_no_cumplida_ya_no_esta_activa()
    {
        Fijar(Ayer, 7);
        await AltaOk(_firulais, TipoDeber.Comida, "08:00", recurrencia: Recurrencia.SoloHoy);

        Fijar(Hoy, 7);

        Assert.Empty(await Activos(_firulais));
    }

    [Fact]
    public async Task AC_19f_un_deber_dado_de_alta_desde_la_configuracion_aparece_en_ella()
    {
        var paseo = await AltaOk(_firulais, TipoDeber.Paseo, "18:00");

        Assert.Equal(paseo, Assert.Single(await Activos(_firulais)).Id);
    }
}
