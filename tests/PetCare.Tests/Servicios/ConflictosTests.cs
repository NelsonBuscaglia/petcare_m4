using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

/// <summary>
/// Dos cuidadores operan sobre la misma instancia antes de que la pantalla del segundo se
/// actualice: se aplica la primera operación y la segunda se rechaza con el motivo (FR-045).
/// </summary>
public class ConflictosTests : IDisposable
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly Escenario _hogar = new();
    private readonly int _a;

    public ConflictosTests() => _a = _hogar.Mascota("A");

    public void Dispose() => _hogar.Dispose();

    /// <summary>Paseo cada 4 h desde las 08:00 (no es de instancia única); devuelve el deber y la instancia de las 08:00.</summary>
    private async Task<(int Deber, int Instancia)> PaseoDeLas8()
    {
        _hogar.Fijar(Hoy, 7);
        var paseo = await _hogar.Programable(_a, TipoDeber.Paseo, "08:00", cadaNHoras: 4);
        _hogar.Fijar(Hoy, 8, 5);
        return (paseo, (await _hogar.Instancia(paseo, Hoy, "08:00")).Id);
    }

    private async Task<int> Paseos() => (await _hogar.Seccion(_a)).Resumen.PaseosDeHoy;

    [Fact]
    public async Task AC_32c_baja_y_despues_cumplimiento_rechaza_el_cumplimiento_porque_ya_fue_dada_de_baja()
    {
        var (_, instancia) = await PaseoDeLas8();
        await _hogar.DarDeBajaInstancia(instancia);

        var cumplimiento = await _hogar.Cumplir(instancia);

        Assert.Equal("Esta tarea ya fue dada de baja.", cumplimiento.Error);
        Assert.Equal(0, await Paseos());
    }

    [Fact]
    public async Task AC_32d_cumplimiento_y_despues_baja_rechaza_la_baja_porque_ya_fue_cumplida()
    {
        var (_, instancia) = await PaseoDeLas8();
        await _hogar.CumplirOk(instancia);

        var baja = await _hogar.DarDeBajaInstancia(instancia);

        Assert.Equal("Esta tarea ya fue cumplida.", baja.Error);
        Assert.Equal(1, await Paseos());
    }

    [Fact]
    public async Task AC_32e_dos_bajas_de_la_misma_instancia_rechazan_la_segunda()
    {
        var (_, instancia) = await PaseoDeLas8();
        Assert.True((await _hogar.DarDeBajaInstancia(instancia)).Ok);

        var segunda = await _hogar.DarDeBajaInstancia(instancia);

        Assert.Equal("Esta tarea ya fue dada de baja.", segunda.Error);
    }

    [Fact]
    public async Task AC_32f_baja_del_deber_y_despues_cumplimiento_de_una_instancia_rechaza_el_cumplimiento()
    {
        var (deber, instancia) = await PaseoDeLas8();
        await _hogar.DarDeBajaDeber(deber);

        var cumplimiento = await _hogar.Cumplir(instancia);

        Assert.Equal("Esta tarea ya fue dada de baja.", cumplimiento.Error);
        Assert.Equal(0, await Paseos());
    }

    [Fact]
    public async Task FR_031_dar_de_baja_una_instancia_inexistente_se_rechaza()
    {
        _hogar.Fijar(Hoy, 9);

        var baja = await _hogar.DarDeBajaInstancia(9999);

        Assert.Equal("Esa tarea no existe.", baja.Error);
    }

    [Fact]
    public async Task FR_045_baja_y_cumplimiento_simultaneos_aplican_solo_uno()
    {
        using var enArchivo = new BaseDeDatosDePrueba(enArchivo: true);
        enArchivo.Fijar(Hoy, new TimeOnly(7, 0));
        Semillas.Hogar(enArchivo);
        var mascota = Semillas.Mascota(enArchivo, "A");
        int instancia;
        await using (var db = enArchivo.Contexto())
        {
            var alta = await new DeberesService(db, enArchivo.Reloj).AltaProgramableAsync(
                new AltaProgramable(mascota, TipoDeber.Paseo, Modalidad.CadaNHoras, new TimeOnly(8, 0), 4, Recurrencia.TodosLosDias, null));
            instancia = db.Instancias.Single(i => i.DeberId == alta.Valor && i.Hora == new TimeOnly(8, 0)).Id;
        }
        enArchivo.Fijar(Hoy, new TimeOnly(8, 5));

        async Task<string?> Cumplir()
        {
            await using var db = enArchivo.Contexto();
            return (await new CumplimientosService(db, enArchivo.Reloj).CumplirAsync(instancia, null)).Error;
        }

        async Task<string?> DarDeBaja()
        {
            await using var db = enArchivo.Contexto();
            return (await new BajasInstanciaService(db, enArchivo.Reloj).DarDeBajaAsync(instancia)).Error;
        }

        var errores = await Task.WhenAll(Task.Run(Cumplir), Task.Run(DarDeBaja));

        Assert.Single(errores, e => e is null);
        await using var verificacion = enArchivo.Contexto();
        var estado = verificacion.Instancias.Single(i => i.Id == instancia).Estado;
        var registros = verificacion.RegistrosCumplimiento.Count(r => r.InstanciaId == instancia);
        Assert.True(
            (estado == EstadoInstancia.Cumplida && registros == 1 && errores[1] == "Esta tarea ya fue cumplida.")
            || (estado == EstadoInstancia.DadaDeBaja && registros == 0 && errores[0] == "Esta tarea ya fue dada de baja."),
            $"estado {estado}, registros {registros}, errores [{errores[0]}] [{errores[1]}]");
    }
}
