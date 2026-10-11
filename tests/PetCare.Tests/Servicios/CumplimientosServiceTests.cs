using Microsoft.EntityFrameworkCore;
using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

public class CumplimientosServiceTests : IDisposable
{
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly BaseDeDatosDePrueba _base = new();
    private readonly int _firulais;

    /// <summary>El hogar y Firulais existen desde ayer; cada test fija la hora que necesita.</summary>
    public CumplimientosServiceTests()
    {
        _base.Fijar(Ayer, new TimeOnly(6, 0));
        Semillas.Hogar(_base);
        _firulais = Semillas.Mascota(_base, "Firulais");
    }

    public void Dispose() => _base.Dispose();

    private void Fijar(DateOnly dia, int hora, int minuto = 0) => _base.Fijar(dia, new TimeOnly(hora, minuto));

    private static async Task<Resultado<int>> Cumplir(BaseDeDatosDePrueba baseDePrueba, int instanciaId, string? hora = null)
    {
        await using var db = baseDePrueba.Contexto();
        return await new CumplimientosService(db, baseDePrueba.Reloj)
            .CumplirAsync(instanciaId, hora is null ? null : TimeOnly.Parse(hora));
    }

    private Task<Resultado<int>> Cumplir(int instanciaId, string? hora = null) => Cumplir(_base, instanciaId, hora);

    private async Task<int> CumplirOk(int instanciaId, string? hora = null)
    {
        var resultado = await Cumplir(instanciaId, hora);
        Assert.True(resultado.Ok, resultado.Error);
        return resultado.Valor;
    }

    private async Task<Resultado> Eliminar(int registroId)
    {
        await using var db = _base.Contexto();
        return await new CumplimientosService(db, _base.Reloj).EliminarAsync(registroId);
    }

    /// <summary>Una instancia vigente de un deber de hora fija de Firulais.</summary>
    private static int Instancia(BaseDeDatosDePrueba baseDePrueba, int mascotaId, TipoDeber tipo, DateOnly dia, string hora)
    {
        using var db = baseDePrueba.Contexto();
        var horaDelDia = TimeOnly.Parse(hora);
        var instancia = new Instancia
        {
            Deber = new Deber
            {
                MascotaId = mascotaId, Tipo = tipo, Modalidad = Modalidad.HoraFija, HoraInicio = horaDelDia,
                Recurrencia = Recurrencia.TodosLosDias, CreadoDia = dia,
            },
            Dia = dia,
            Hora = horaDelDia,
            ProgramadaUtc = RelojHogar.AUtc(dia, horaDelDia),
            Estado = EstadoInstancia.Vigente,
        };
        db.Instancias.Add(instancia);
        db.SaveChanges();
        return instancia.Id;
    }

    private int Instancia(TipoDeber tipo, DateOnly dia, string hora) => Instancia(_base, _firulais, tipo, dia, hora);

    private Instancia Leer(int instanciaId)
    {
        using var db = _base.Contexto();
        return db.Instancias.AsNoTracking().Single(i => i.Id == instanciaId);
    }

    private List<RegistroCumplimiento> Registros()
    {
        using var db = _base.Contexto();
        return db.RegistrosCumplimiento.AsNoTracking().ToList();
    }

    [Fact]
    public async Task AC_32_cumplir_un_paseo_lo_deja_cumplido_y_suma_un_paseo_de_hoy()
    {
        var paseo = Instancia(TipoDeber.Paseo, Hoy, "09:00");
        Fijar(Hoy, 9, 5);

        var registroId = await CumplirOk(paseo);

        Assert.Equal(EstadoInstancia.Cumplida, Leer(paseo).Estado);
        var registro = Assert.Single(Registros());
        Assert.Equal(registroId, registro.Id);
        Assert.Equal(paseo, registro.InstanciaId);
        Assert.Equal(Hoy, registro.Dia);
        Assert.Equal(new TimeOnly(9, 5), registro.Hora);
    }

    [Fact]
    public async Task AC_32b_dos_cumplimientos_simultaneos_de_la_misma_instancia_registran_uno_solo()
    {
        using var enArchivo = new BaseDeDatosDePrueba(enArchivo: true);
        enArchivo.Fijar(Hoy, new TimeOnly(9, 0));
        Semillas.Hogar(enArchivo);
        var paseo = Instancia(enArchivo, Semillas.Mascota(enArchivo, "A"), TipoDeber.Paseo, Hoy, "09:00");
        enArchivo.Fijar(Hoy, new TimeOnly(9, 10));

        var resultados = await Task.WhenAll(
            Task.Run(() => Cumplir(enArchivo, paseo)),
            Task.Run(() => Cumplir(enArchivo, paseo)));

        Assert.Single(resultados, r => r.Ok);
        Assert.Single(resultados, r => r.Error == "Esta tarea ya fue cumplida.");
        using var db = enArchivo.Contexto();
        Assert.Single(db.RegistrosCumplimiento);
    }

    [Fact]
    public async Task FR_029_cumplir_una_instancia_ya_cumplida_se_rechaza_sin_un_segundo_registro()
    {
        var paseo = Instancia(TipoDeber.Paseo, Hoy, "09:00");
        Fijar(Hoy, 9, 10);
        await CumplirOk(paseo);

        var segundo = await Cumplir(paseo);

        Assert.Equal("Esta tarea ya fue cumplida.", segundo.Error);
        Assert.Single(Registros());
    }

    [Fact]
    public async Task AC_35_sin_hora_se_registra_la_hora_actual()
    {
        var comida = Instancia(TipoDeber.Comida, Hoy, "10:00");
        Fijar(Hoy, 10, 30);

        await CumplirOk(comida);

        var registro = Assert.Single(Registros());
        Assert.Equal((Hoy, new TimeOnly(10, 30)), (registro.Dia, registro.Hora!.Value));
    }

    [Fact]
    public async Task AC_35b_se_registra_la_hora_editada()
    {
        var comida = Instancia(TipoDeber.Comida, Hoy, "10:00");
        Fijar(Hoy, 10, 30);

        await CumplirOk(comida, "10:00");

        Assert.Equal(new TimeOnly(10, 0), Assert.Single(Registros()).Hora);
    }

    [Fact]
    public async Task AC_35c_AC_63c_una_hora_posterior_a_la_actual_se_rechaza_y_la_instancia_sigue_vigente()
    {
        var comida = Instancia(TipoDeber.Comida, Hoy, "10:00");
        Fijar(Hoy, 10, 30);

        var resultado = await Cumplir(comida, "11:00");

        Assert.Equal("La hora no puede ser posterior a la actual.", resultado.Error);
        Assert.Equal(EstadoInstancia.Vigente, Leer(comida).Estado);
        Assert.Empty(Registros());
    }

    [Fact]
    public async Task AC_35d_cumplir_hoy_una_medicacion_de_ayer_con_una_hora_futura_de_hoy_se_rechaza()
    {
        var medicacion = Instancia(TipoDeber.Medicacion, Ayer, "21:00");
        Fijar(Hoy, 8);

        var resultado = await Cumplir(medicacion, "22:00");

        Assert.Equal("La hora no puede ser posterior a la actual.", resultado.Error);
        Assert.Equal(EstadoInstancia.Vigente, Leer(medicacion).Estado);
        Assert.Empty(Registros());
    }

    [Fact]
    public async Task FR_052_la_fecha_del_registro_es_el_dia_en_que_se_registra()
    {
        var medicacion = Instancia(TipoDeber.Medicacion, Ayer, "21:00");
        Fijar(Hoy, 8);

        await CumplirOk(medicacion, "07:30");

        var registro = Assert.Single(Registros());
        Assert.Equal((Hoy, new TimeOnly(7, 30)), (registro.Dia, registro.Hora!.Value));
    }

    [Fact]
    public async Task AC_37_eliminar_uno_de_dos_paseos_de_hoy_deja_uno()
    {
        var paseo1 = Instancia(TipoDeber.Paseo, Hoy, "08:00");
        var paseo2 = Instancia(TipoDeber.Paseo, Hoy, "12:00");
        Fijar(Hoy, 12, 30);
        await CumplirOk(paseo1);
        var porError = await CumplirOk(paseo2);

        var resultado = await Eliminar(porError);

        Assert.True(resultado.Ok, resultado.Error);
        Assert.Equal(paseo1, Assert.Single(Registros()).InstanciaId);
        Assert.Equal(EstadoInstancia.Vigente, Leer(paseo2).Estado);
    }

    [Fact]
    public async Task AC_37b_eliminar_un_registro_de_ayer_se_rechaza_y_se_conserva()
    {
        var paseo = Instancia(TipoDeber.Paseo, Ayer, "18:00");
        Fijar(Ayer, 18);
        var deAyer = await CumplirOk(paseo);
        Fijar(Hoy, 9);

        var resultado = await Eliminar(deAyer);

        Assert.Equal("Solo se pueden eliminar registros de hoy.", resultado.Error);
        var registro = Assert.Single(Registros());
        Assert.Equal((Ayer, new TimeOnly(18, 0)), (registro.Dia, registro.Hora!.Value));
        Assert.Equal(EstadoInstancia.Cumplida, Leer(paseo).Estado);
    }

    [Fact]
    public async Task AC_37c_un_registro_de_hoy_sobre_una_medicacion_de_ayer_se_elimina_y_la_instancia_vuelve()
    {
        var medicacion = Instancia(TipoDeber.Medicacion, Ayer, "09:00");
        Fijar(Hoy, 8);
        var porError = await CumplirOk(medicacion);
        Fijar(Hoy, 10);

        var resultado = await Eliminar(porError);

        Assert.True(resultado.Ok, resultado.Error);
        Assert.Empty(Registros());
        Assert.Equal(EstadoInstancia.Vigente, Leer(medicacion).Estado);
    }

    [Fact]
    public async Task R7_eliminar_un_registro_abre_un_nuevo_episodio_de_vencimiento()
    {
        var comida = Instancia(TipoDeber.Comida, Hoy, "08:00");
        Fijar(Hoy, 8, 10);
        var porError = await CumplirOk(comida);
        Fijar(Hoy, 10);

        await Eliminar(porError);

        var instancia = Leer(comida);
        Assert.Equal(1, instancia.EpisodioVencimiento);
        Assert.Equal(RelojHogar.AUtc(Hoy, new TimeOnly(10, 0)), instancia.ReactivadaUtc);
    }

    [Fact]
    public async Task FR_031_cumplir_instancia_inexistente_se_rechaza()
    {
        Fijar(Hoy, 9);

        var resultado = await Cumplir(9999);

        Assert.Equal("Esa tarea no existe.", resultado.Error);
        Assert.Empty(Registros());
    }
}
