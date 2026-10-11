using PetCare.Web.Dominio;

namespace PetCare.Tests.Dominio;

public class CodigoHogarTests
{
    [Fact]
    public void AC_01c_el_codigo_tiene_8_letras_mayusculas_o_digitos_sin_0_O_1_I_ni_L()
    {
        // Se generan varios para no depender de un único sorteo.
        for (var i = 0; i < 500; i++)
        {
            var codigo = CodigoHogar.Generar();

            Assert.Equal(8, codigo.Length);
            Assert.All(codigo, c => Assert.True(char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c), $"'{c}' en {codigo}"));
            Assert.All(codigo, c => Assert.DoesNotContain(c, "0O1IL"));
        }
    }

    [Theory]
    [InlineData("abcd2345")]
    [InlineData("ABCD2345")]
    [InlineData("AbCd2345")]
    public void AC_02h_la_comparacion_no_distingue_mayusculas(string ingresado)
    {
        Assert.True(CodigoHogar.Coincide(ingresado, "ABCD2345"));
    }

    [Theory]
    [InlineData("ABCD2346")]
    [InlineData("ABCD234")]
    [InlineData("")]
    [InlineData(null)]
    public void Un_codigo_distinto_o_ausente_no_coincide(string? ingresado)
    {
        Assert.False(CodigoHogar.Coincide(ingresado, "ABCD2345"));
    }
}
