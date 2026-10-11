using Microsoft.EntityFrameworkCore;
using PetCare.Tests.Infra;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

public class MascotasServiceTests : IDisposable
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly BaseDeDatosDePrueba _base = new();

    public MascotasServiceTests()
    {
        _base.Fijar(Hoy, new TimeOnly(10, 0));
        using var db = _base.Contexto();
        db.Hogares.Add(new Hogar { Id = 1, Codigo = "ABCD2345", CreadoUtc = _base.Reloj.AhoraUtc });
        db.SaveChanges();
    }

    public void Dispose() => _base.Dispose();

    private async Task<Resultado<int>> Registrar(string nombre)
    {
        await using var db = _base.Contexto();
        return await new MascotasService(db, _base.Reloj).RegistrarAsync(nombre);
    }

    private List<Mascota> Mascotas()
    {
        using var db = _base.Contexto();
        return db.Mascotas.AsNoTracking().ToList();
    }

    [Fact]
    public async Task AC_06_se_registra_la_mascota_del_hogar_con_su_nombre_y_el_dia_del_alta()
    {
        var resultado = await Registrar("Firulais");

        Assert.True(resultado.Ok, resultado.Error);
        var firulais = Assert.Single(Mascotas());
        Assert.Equal(resultado.Valor, firulais.Id);
        Assert.Equal("Firulais", firulais.Nombre);
        Assert.Equal(1, firulais.HogarId);
        Assert.Equal(Hoy, firulais.CreadaDia);
        Assert.Null(firulais.MaxDiasEntreBanos);
    }

    [Fact]
    public async Task RF_04_se_pueden_registrar_varias_mascotas()
    {
        await Registrar("Firulais");

        var resultado = await Registrar("Michi");

        Assert.True(resultado.Ok, resultado.Error);
        Assert.Equal(2, Mascotas().Count);
    }

    [Theory]
    [InlineData("Firulais")]
    [InlineData("FIRULAIS")]
    [InlineData("firulais")]
    public async Task AC_06b_AC_06c_AC_63_un_nombre_repetido_sin_distinguir_mayusculas_se_rechaza(string repetido)
    {
        await Registrar("Firulais");

        var resultado = await Registrar(repetido);

        Assert.Equal("Ya existe una mascota con ese nombre.", resultado.Error);
        Assert.Equal("Firulais", Assert.Single(Mascotas()).Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AC_06d_AC_06e_un_nombre_vacio_o_con_solo_espacios_se_rechaza(string nombre)
    {
        await Registrar("Firulais");

        var resultado = await Registrar(nombre);

        Assert.Equal("El nombre de la mascota no puede estar vacío.", resultado.Error);
        Assert.Equal("Firulais", Assert.Single(Mascotas()).Nombre);
    }

    [Fact]
    public async Task AC_63e_sin_mascotas_un_nombre_vacio_se_rechaza_informando_el_motivo()
    {
        var resultado = await Registrar("");

        Assert.Equal("El nombre de la mascota no puede estar vacío.", resultado.Error);
        Assert.Empty(Mascotas());
    }

    [Fact]
    public async Task AC_06f_los_espacios_al_principio_y_al_final_se_quitan_al_guardar()
    {
        var resultado = await Registrar("  Firulais  ");

        Assert.True(resultado.Ok, resultado.Error);
        var firulais = Assert.Single(Mascotas());
        Assert.Equal("Firulais", firulais.Nombre);
        Assert.Equal("FIRULAIS", firulais.NombreNormalizado);
    }

    [Fact]
    public async Task AC_06g_un_nombre_repetido_con_un_espacio_al_final_se_rechaza()
    {
        await Registrar("Firulais");

        var resultado = await Registrar("Firulais ");

        Assert.Equal("Ya existe una mascota con ese nombre.", resultado.Error);
        Assert.Equal("Firulais", Assert.Single(Mascotas()).Nombre);
    }
}
