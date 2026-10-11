using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

/// <summary>Marca "pendiente", expiración y arrastre en el estado del día (FR-019, FR-021, FR-022).</summary>
public class VencimientosEstadoTests : IDisposable
{
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private static readonly DateOnly Hoy = new(2026, 10, 10);
    private static readonly DateOnly Manana = new(2026, 10, 11);

    private readonly Escenario _hogar = new();
    private readonly int _firulais;

    public VencimientosEstadoTests() => _firulais = _hogar.Mascota();

    public void Dispose() => _hogar.Dispose();

    private static string Ver(InstanciaVista i) => $"{i.Dia:dd} {i.Hora:HH:mm} {i.Marca}";

    [Fact]
    public async Task AC_31_la_medicacion_de_ayer_sin_cumplir_aparece_pendiente_junto_a_la_de_hoy()
    {
        _hogar.Fijar(Ayer, 7);
        await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00");
        _hogar.Fijar(Hoy, 0, 1);
        await _hogar.Ciclo();

        _hogar.Fijar(Hoy, 8);
        var seccion = await _hogar.Seccion(_firulais);

        Assert.Equal(["09 09:00 Pendiente", "10 09:00 Ninguna"], seccion.Instancias.Select(Ver));
    }

    [Fact]
    public async Task AC_31b_cumplir_la_medicacion_de_ayer_no_afecta_la_de_hoy()
    {
        _hogar.Fijar(Ayer, 7);
        var medicacion = await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00");
        _hogar.Fijar(Hoy, 0, 1);
        await _hogar.Ciclo();
        _hogar.Fijar(Hoy, 8);

        await _hogar.CumplirOk((await _hogar.Instancia(medicacion, Ayer, "09:00")).Id);

        Assert.Equal(["10 09:00 Ninguna"], (await _hogar.Seccion(_firulais)).Instancias.Select(Ver));
    }

    [Fact]
    public async Task AC_30_la_comida_de_ayer_sin_cumplir_no_aparece_y_la_de_hoy_aparece_sin_marca()
    {
        _hogar.Fijar(Ayer, 7);
        await _hogar.Programable(_firulais, TipoDeber.Comida, "08:00");
        _hogar.Fijar(Hoy, 0, 1);
        await _hogar.Ciclo();

        _hogar.Fijar(Hoy, 7);

        Assert.Equal(["10 08:00 Ninguna"], (await _hogar.Seccion(_firulais)).Instancias.Select(Ver));
    }

    [Fact]
    public async Task AC_13c_AC_19e_la_medicacion_terminada_ayer_sin_cumplir_sigue_pendiente_y_activa()
    {
        // "Día 10" = ayer, "día 11" = hoy.
        _hogar.Fijar(Ayer, 7);
        var medicacion = await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00", hasta: Ayer);
        _hogar.Fijar(Hoy, 0, 1);
        await _hogar.Ciclo();

        _hogar.Fijar(Hoy, 8);

        Assert.Equal(["09 09:00 Pendiente"], (await _hogar.Seccion(_firulais)).Instancias.Select(Ver));
        Assert.Equal(medicacion, Assert.Single(await _hogar.DeberesActivos(_firulais)).Id);
    }

    [Fact]
    public async Task AC_14_un_paseo_de_las_10_dado_de_alta_a_las_10_30_aparece_pendiente()
    {
        _hogar.Fijar(Hoy, 10, 30);

        await _hogar.Programable(_firulais, TipoDeber.Paseo, "10:00", recurrencia: Recurrencia.SoloHoy);

        Assert.Equal(["10 10:00 Pendiente"], (await _hogar.Seccion(_firulais)).Instancias.Select(Ver));
    }

    [Fact]
    public async Task AC_29_AC_29b_la_marca_pendiente_aparece_a_los_15_minutos()
    {
        _hogar.Fijar(Hoy, 7);
        await _hogar.Programable(_firulais, TipoDeber.Medicacion, "09:00");

        _hogar.Fijar(Hoy, 9, 10);
        Assert.Equal(["10 09:00 Ninguna"], (await _hogar.Seccion(_firulais)).Instancias.Select(Ver));

        _hogar.Fijar(Hoy, 9, 15);
        Assert.Equal(["10 09:00 Pendiente"], (await _hogar.Seccion(_firulais)).Instancias.Select(Ver));
    }

    [Fact]
    public async Task AC_28b_a_las_23_59_no_hay_instancias_del_dia_siguiente()
    {
        _hogar.Fijar(Hoy, 7);
        await _hogar.Programable(_firulais, TipoDeber.Comida, "08:00");

        _hogar.Fijar(Hoy, 23, 59);
        await _hogar.Ciclo();

        Assert.DoesNotContain((await _hogar.Seccion(_firulais)).Instancias, i => i.Dia == Manana);
    }

    [Fact]
    public async Task AC_28_a_las_00_05_del_hogar_ya_esta_la_instancia_del_nuevo_dia()
    {
        _hogar.Fijar(Hoy, 7);
        await _hogar.Programable(_firulais, TipoDeber.Comida, "08:00");

        _hogar.Fijar(Manana, 0, 5);
        await _hogar.Ciclo();

        Assert.Equal(["11 08:00 Ninguna"], (await _hogar.Seccion(_firulais)).Instancias.Select(Ver));
    }

    [Fact]
    public async Task AC_38_al_eliminar_el_registro_erroneo_la_comida_vuelve_pendiente()
    {
        _hogar.Fijar(Hoy, 7);
        var comida = await _hogar.Programable(_firulais, TipoDeber.Comida, "08:00");
        _hogar.Fijar(Hoy, 8, 10);
        var porError = await _hogar.CumplirOk((await _hogar.Instancia(comida, Hoy, "08:00")).Id);

        _hogar.Fijar(Hoy, 10);
        await _hogar.Eliminar(porError);

        Assert.Equal(["10 08:00 Pendiente"], (await _hogar.Seccion(_firulais)).Instancias.Select(Ver));
    }
}
