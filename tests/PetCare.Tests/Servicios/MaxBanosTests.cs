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
}
