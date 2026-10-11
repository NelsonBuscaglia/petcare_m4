using PetCare.Tests.Infra;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Programado;

/// <summary>Al volver a estar en ejecución, se generan las instancias que faltan (FR-020).</summary>
public class ReinicioTests : IDisposable
{
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly Escenario _hogar = new();

    public void Dispose() => _hogar.Dispose();

    [Fact]
    public async Task AC_28c_tras_un_corte_entre_las_23_y_las_10_el_primer_ciclo_genera_la_comida_de_las_08_ya_vencida()
    {
        var firulais = _hogar.Mascota();
        _hogar.Fijar(Ayer, 7);
        var comida = await _hogar.Programable(firulais, TipoDeber.Comida, "08:00");
        _hogar.Fijar(Ayer, 23);
        await _hogar.Ciclo();

        // Servidor caído: no hay ciclos hasta las 10:00 de hoy.
        _hogar.Fijar(Hoy, 10);
        await _hogar.Ciclo();

        var instancia = await _hogar.Instancia(comida, Hoy, "08:00");
        Assert.True(instancia.GeneradaVencida);
        var vista = Assert.Single((await _hogar.Seccion(firulais)).Instancias);
        Assert.Equal((Hoy, new TimeOnly(8, 0), MarcaInstancia.Pendiente), (vista.Dia, vista.Hora, vista.Marca));
    }
}
