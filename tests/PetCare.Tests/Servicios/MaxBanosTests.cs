using PetCare.Tests.Infra;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

/// <summary>Máximo de días entre baños por mascota (FR-008).</summary>
public class MaxBanosTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _base = new();
    private readonly int _firulais;

    public MaxBanosTests()
    {
        _base.Fijar(new DateOnly(2026, 10, 10), new TimeOnly(9, 0));
        Semillas.Hogar(_base);
        _firulais = Semillas.Mascota(_base, "Firulais");
    }

    public void Dispose() => _base.Dispose();

    private async Task<Resultado> Definir(int maximo)
    {
        await using var db = _base.Contexto();
        return await new MascotasService(db, _base.Reloj).DefinirMaxDiasEntreBanosAsync(_firulais, maximo);
    }

    private int? MaximoGuardado()
    {
        using var db = _base.Contexto();
        return db.Mascotas.Single(m => m.Id == _firulais).MaxDiasEntreBanos;
    }

    [Fact]
    public async Task AC_08_se_define_el_maximo_en_una_mascota_sin_deberes()
    {
        var resultado = await Definir(20);

        Assert.True(resultado.Ok, resultado.Error);
        Assert.Equal(20, MaximoGuardado());
        using var db = _base.Contexto();
        Assert.Empty(db.Deberes);
    }

    [Fact]
    public async Task AC_07_el_maximo_definido_se_puede_modificar()
    {
        await Definir(20);

        var resultado = await Definir(30);

        Assert.True(resultado.Ok, resultado.Error);
        Assert.Equal(30, MaximoGuardado());
    }

    [Fact]
    public async Task AC_08b_AC_63f_definir_0_se_rechaza_informando_el_motivo_y_sigue_sin_maximo()
    {
        var resultado = await Definir(0);

        Assert.Equal("El máximo de días entre baños debe ser un número entero mayor que 0.", resultado.Error);
        Assert.Null(MaximoGuardado());
    }

    [Fact]
    public async Task AC_07b_cambiar_el_maximo_a_un_valor_negativo_se_rechaza_y_sigue_en_20()
    {
        await Definir(20);

        var resultado = await Definir(-5);

        Assert.Equal("El máximo de días entre baños debe ser un número entero mayor que 0.", resultado.Error);
        Assert.Equal(20, MaximoGuardado());
    }

    [Fact]
    public async Task RF_05c_un_maximo_de_1_se_acepta()
    {
        var resultado = await Definir(1);

        Assert.True(resultado.Ok, resultado.Error);
        Assert.Equal(1, MaximoGuardado());
    }
}
