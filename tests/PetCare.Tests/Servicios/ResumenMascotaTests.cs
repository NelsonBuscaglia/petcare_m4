using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

/// <summary>Resumen por mascota en el estado del día (FR-034, FR-053).</summary>
public class ResumenMascotaTests : IDisposable
{
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly BaseDeDatosDePrueba _base = new();
    private readonly int _firulais;

    public ResumenMascotaTests()
    {
        _base.Fijar(Ayer, new TimeOnly(6, 0));
        Semillas.Hogar(_base);
        _firulais = Semillas.Mascota(_base, "Firulais");
    }

    public void Dispose() => _base.Dispose();

    private void Fijar(DateOnly dia, int hora, int minuto = 0) => _base.Fijar(dia, new TimeOnly(hora, minuto));

    private async Task<SeccionMascota> Seccion()
    {
        await using var db = _base.Contexto();
        return (await new EstadoDelDiaQuery(db, _base.Reloj).ObtenerAsync()).Mascotas.Single(m => m.MascotaId == _firulais);
    }

    /// <summary>
    /// Una instancia de un deber de hora fija; si se indica <paramref name="cumplida"/>, queda
    /// cumplida con un registro de ese día y hora. Devuelve el id del deber.
    /// </summary>
    private int Deber(TipoDeber tipo, DateOnly dia, string hora, (DateOnly Dia, string Hora)? cumplida = null,
        Recurrencia recurrencia = Recurrencia.TodosLosDias)
    {
        using var db = _base.Contexto();
        var horaDelDia = TimeOnly.Parse(hora);
        var deber = new Deber
        {
            MascotaId = _firulais, Tipo = tipo, Modalidad = Modalidad.HoraFija, HoraInicio = horaDelDia,
            Recurrencia = recurrencia, CreadoDia = dia,
        };
        var instancia = new Instancia
        {
            Deber = deber, Dia = dia, Hora = horaDelDia, ProgramadaUtc = RelojHogar.AUtc(dia, horaDelDia),
            Estado = cumplida is null ? EstadoInstancia.Vigente : EstadoInstancia.Cumplida,
        };
        db.Instancias.Add(instancia);
        if (cumplida is { } registro)
            db.RegistrosCumplimiento.Add(new RegistroCumplimiento
            {
                Instancia = instancia, Dia = registro.Dia, Hora = TimeOnly.Parse(registro.Hora),
            });
        db.SaveChanges();
        return deber.Id;
    }

    [Fact]
    public async Task AC_39c_muestra_la_ultima_comida_y_el_ultimo_paseo_aunque_sean_de_ayer()
    {
        Deber(TipoDeber.Comida, Ayer, "08:00", cumplida: (Ayer, "08:05"));
        Deber(TipoDeber.Comida, Ayer, "20:00", cumplida: (Ayer, "20:00"));
        Deber(TipoDeber.Paseo, Ayer, "21:00", cumplida: (Ayer, "21:00"));
        Fijar(Hoy, 7);

        var resumen = (await Seccion()).Resumen;

        Assert.Equal(new Momento(Ayer, new TimeOnly(20, 0)), resumen.UltimaComida);
        Assert.Equal(new Momento(Ayer, new TimeOnly(21, 0)), resumen.UltimoPaseo);
        Assert.Equal(0, resumen.PaseosDeHoy);
    }

    [Fact]
    public async Task AC_39d_sin_cumplimientos_no_hay_ultima_comida_ni_ultimo_paseo()
    {
        Fijar(Hoy, 7);

        var resumen = (await Seccion()).Resumen;

        Assert.Null(resumen.UltimaComida);
        Assert.Null(resumen.UltimoPaseo);
        Assert.Equal(0, resumen.PaseosDeHoy);
    }

    [Fact]
    public async Task AC_32_RF_27a_los_paseos_de_hoy_cuentan_solo_los_registros_de_hoy()
    {
        Deber(TipoDeber.Paseo, Ayer, "18:00", cumplida: (Ayer, "18:00"));
        Deber(TipoDeber.Paseo, Hoy, "08:00", cumplida: (Hoy, "08:10"));
        Deber(TipoDeber.Paseo, Hoy, "11:00", cumplida: (Hoy, "11:00"));
        Deber(TipoDeber.Comida, Hoy, "09:00", cumplida: (Hoy, "09:00"));
        Fijar(Hoy, 12);

        var resumen = (await Seccion()).Resumen;

        Assert.Equal(2, resumen.PaseosDeHoy);
        Assert.Equal(new Momento(Hoy, new TimeOnly(11, 0)), resumen.UltimoPaseo);
    }

    [Fact]
    public async Task AC_65b_sin_instancias_vigentes_la_seccion_se_muestra_con_el_resumen_y_sin_tareas_pendientes()
    {
        Deber(TipoDeber.Paseo, Hoy, "09:00", cumplida: (Hoy, "09:05"), recurrencia: Recurrencia.SoloHoy);
        Fijar(Hoy, 10);

        var seccion = await Seccion();

        Assert.Empty(seccion.Instancias);
        Assert.True(seccion.SinTareasPendientes);
        Assert.Equal(1, seccion.Resumen.PaseosDeHoy);
        Assert.Equal(new Momento(Hoy, new TimeOnly(9, 5)), seccion.Resumen.UltimoPaseo);
    }

    [Fact]
    public async Task AC_25_parte_resumen_el_paseo_cumplido_sigue_contado_tras_dar_de_baja_el_deber()
    {
        var paseo = Deber(TipoDeber.Paseo, Hoy, "09:00", cumplida: (Hoy, "09:05"));
        Fijar(Hoy, 12);
        using (var db = _base.Contexto())
        {
            db.Deberes.Single(d => d.Id == paseo).DadoDeBajaUtc = _base.Reloj.AhoraUtc;
            db.SaveChanges();
        }

        Assert.Equal(1, (await Seccion()).Resumen.PaseosDeHoy);
    }

    [Fact]
    public async Task FR_030_la_seccion_lista_los_registros_de_hoy_que_se_pueden_eliminar()
    {
        Deber(TipoDeber.Paseo, Ayer, "18:00", cumplida: (Ayer, "18:00"));
        Deber(TipoDeber.Medicacion, Ayer, "21:00", cumplida: (Hoy, "07:30"));
        Deber(TipoDeber.Paseo, Hoy, "08:00", cumplida: (Hoy, "08:10"));
        Fijar(Hoy, 9);

        var registros = (await Seccion()).RegistrosDeHoy;

        Assert.Equal([TipoDeber.Medicacion, TipoDeber.Paseo], registros.Select(r => r.Tipo));
        Assert.Equal([new TimeOnly(7, 30), new TimeOnly(8, 10)], registros.Select(r => r.Hora!.Value));
    }
}
