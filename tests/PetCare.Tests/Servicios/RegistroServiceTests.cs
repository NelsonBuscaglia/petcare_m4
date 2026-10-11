using Microsoft.EntityFrameworkCore;
using PetCare.Tests.Infra;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

public class RegistroServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _base = new();

    public void Dispose() => _base.Dispose();

    private async Task<ResultadoRegistro> Registrar(string email, string password, string? codigo = null)
    {
        await using var db = _base.Contexto();
        return await new RegistroService(db, _base.Reloj).RegistrarAsync(email, password, codigo);
    }

    /// <summary>Registra a la primera persona y devuelve el código del hogar creado.</summary>
    private async Task<string> HogarDeAna()
    {
        var resultado = await Registrar("ana@ejemplo.com", "secreta123");
        Assert.True(resultado.Ok, resultado.Error);
        return resultado.CodigoCreado!;
    }

    private int CantidadDeCuidadores()
    {
        using var db = _base.Contexto();
        return db.Cuidadores.Count();
    }

    [Fact]
    public async Task AC_01_la_primera_persona_crea_el_hogar_y_recibe_su_codigo()
    {
        var resultado = await Registrar("ana@ejemplo.com", "secreta123");

        Assert.True(resultado.Ok, resultado.Error);
        await using var db = _base.Contexto();
        var hogar = await db.Hogares.SingleAsync();
        Assert.Equal(1, hogar.Id);
        Assert.Equal(hogar.Codigo, resultado.CodigoCreado);
        var ana = await db.Cuidadores.SingleAsync();
        Assert.Equal(hogar.Id, ana.HogarId);
        Assert.Equal("ana@ejemplo.com", ana.Email);
        Assert.NotEqual("secreta123", ana.PasswordHash);
    }

    [Fact]
    public async Task FR_002_con_el_codigo_correcto_la_segunda_persona_se_suma_al_mismo_hogar()
    {
        var codigo = await HogarDeAna();

        var resultado = await Registrar("beto@ejemplo.com", "secreta123", codigo);

        Assert.True(resultado.Ok, resultado.Error);
        Assert.Null(resultado.CodigoCreado);
        await using var db = _base.Contexto();
        Assert.Single(db.Hogares);
        Assert.All(db.Cuidadores, c => Assert.Equal(1, c.HogarId));
    }

    [Fact]
    public async Task AC_02b_AC_63d_con_codigo_incorrecto_se_rechaza_y_no_se_registra()
    {
        await HogarDeAna();

        var resultado = await Registrar("beto@ejemplo.com", "secreta123", "ZZZZ9999");

        Assert.Equal("El código de hogar es incorrecto.", resultado.Error);
        Assert.Equal(1, CantidadDeCuidadores());
    }

    [Fact]
    public async Task AC_02c_sin_codigo_con_el_hogar_ya_creado_se_rechaza_y_no_se_registra()
    {
        await HogarDeAna();

        var resultado = await Registrar("beto@ejemplo.com", "secreta123");

        Assert.Equal("El hogar ya existe: para sumarte necesitás su código.", resultado.Error);
        Assert.Equal(1, CantidadDeCuidadores());
    }

    [Theory]
    [InlineData("ana@ejemplo.com")]
    [InlineData("Ana@Ejemplo.com")]
    public async Task AC_02d_AC_02e_un_email_ya_registrado_sin_distinguir_mayusculas_se_rechaza(string email)
    {
        var codigo = await HogarDeAna();

        var resultado = await Registrar(email, "otraClave99", codigo);

        Assert.Equal("Ese email ya está registrado.", resultado.Error);
        Assert.Equal(1, CantidadDeCuidadores());
    }

    [Fact]
    public async Task AC_02f_una_contrasena_de_7_caracteres_se_rechaza_informando_el_minimo()
    {
        var codigo = await HogarDeAna();

        var resultado = await Registrar("beto@ejemplo.com", "abc1234", codigo);

        Assert.Equal("La contraseña debe tener al menos 8 caracteres.", resultado.Error);
        Assert.Equal(1, CantidadDeCuidadores());
    }

    [Fact]
    public async Task AC_02g_una_contrasena_de_8_caracteres_se_acepta()
    {
        var codigo = await HogarDeAna();

        var resultado = await Registrar("beto@ejemplo.com", "abcd1234", codigo);

        Assert.True(resultado.Ok, resultado.Error);
        Assert.Equal(2, CantidadDeCuidadores());
    }

    [Fact]
    public async Task FR_047_la_primera_persona_tampoco_puede_usar_menos_de_8_caracteres()
    {
        var resultado = await Registrar("ana@ejemplo.com", "abc1234");

        Assert.Equal("La contraseña debe tener al menos 8 caracteres.", resultado.Error);
        await using var db = _base.Contexto();
        Assert.Empty(db.Hogares);
        Assert.Empty(db.Cuidadores);
    }

    [Fact]
    public async Task AC_02h_el_codigo_en_minusculas_se_acepta()
    {
        var codigo = await HogarDeAna();

        var resultado = await Registrar("beto@ejemplo.com", "secreta123", codigo.ToLowerInvariant());

        Assert.True(resultado.Ok, resultado.Error);
    }

    [Fact]
    public async Task AC_02i_dos_registros_simultaneos_sin_codigo_dejan_un_unico_hogar_y_un_registro()
    {
        using var enArchivo = new BaseDeDatosDePrueba(enArchivo: true);

        async Task<ResultadoRegistro> RegistrarEnParalelo(string email)
        {
            await using var db = enArchivo.Contexto();
            return await new RegistroService(db, enArchivo.Reloj).RegistrarAsync(email, "secreta123", null);
        }

        var resultados = await Task.WhenAll(
            Task.Run(() => RegistrarEnParalelo("ana@ejemplo.com")),
            Task.Run(() => RegistrarEnParalelo("beto@ejemplo.com")));

        Assert.Single(resultados, r => r.Ok);
        Assert.Single(resultados, r => r.Error == "El hogar ya existe: para sumarte necesitás su código.");
        await using var verificacion = enArchivo.Contexto();
        Assert.Single(verificacion.Hogares);
        Assert.Single(verificacion.Cuidadores);
    }

    [Fact]
    public async Task AC_02j_email_registrado_con_codigo_incorrecto_solo_informa_el_codigo()
    {
        await HogarDeAna();

        var resultado = await Registrar("ana@ejemplo.com", "secreta123", "ZZZZ9999");

        Assert.Equal("El código de hogar es incorrecto.", resultado.Error);
    }
}
