using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Tests.Dominio;

/// <summary>
/// Cuándo corresponde el aviso de turno de baño: las tres condiciones de FR-040, contando días
/// calendario en UTC−3 (con máximo 20, a partir del día 21).
/// </summary>
public class AvisoTurnoBanoTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private static Mascota Firulais(int? maximo = 20, int altaHaceDias = 100) =>
        new() { Nombre = "Firulais", MaxDiasEntreBanos = maximo, CreadaDia = Hoy.AddDays(-altaHaceDias) };

    [Fact]
    public void AC_53_ultimo_bano_hace_25_dias_con_maximo_20_y_sin_bano_activo_corresponde()
    {
        Assert.True(AvisoTurnoBano.Corresponde(Firulais(), Hoy.AddDays(-25), tieneBanoActivo: false, Hoy));
    }

    [Fact]
    public void AC_53d_ultimo_bano_hace_exactamente_20_dias_no_corresponde()
    {
        Assert.False(AvisoTurnoBano.Corresponde(Firulais(), Hoy.AddDays(-20), tieneBanoActivo: false, Hoy));
    }

    [Fact]
    public void FR_040_con_maximo_20_corresponde_a_partir_del_dia_21()
    {
        Assert.True(AvisoTurnoBano.Corresponde(Firulais(), Hoy.AddDays(-21), tieneBanoActivo: false, Hoy));
    }

    [Fact]
    public void AC_55_sin_maximo_definido_no_corresponde_aunque_hayan_pasado_60_dias()
    {
        Assert.False(AvisoTurnoBano.Corresponde(Firulais(maximo: null), Hoy.AddDays(-60), tieneBanoActivo: false, Hoy));
    }

    [Fact]
    public void AC_56_sin_banos_registrados_se_cuenta_desde_el_alta_de_la_mascota()
    {
        Assert.True(AvisoTurnoBano.Corresponde(Firulais(altaHaceDias: 25), fechaUltimoBano: null, tieneBanoActivo: false, Hoy));
        Assert.False(AvisoTurnoBano.Corresponde(Firulais(altaHaceDias: 20), fechaUltimoBano: null, tieneBanoActivo: false, Hoy));
    }

    [Fact]
    public void AC_54_AC_54b_AC_54c_con_un_bano_activo_no_corresponde()
    {
        // Baño activo: futuro (AC-54), de hoy (AC-54b) o "no cumplido" (AC-54c).
        Assert.False(AvisoTurnoBano.Corresponde(Firulais(), Hoy.AddDays(-25), tieneBanoActivo: true, Hoy));
    }
}
