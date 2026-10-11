using System.Security.Cryptography;

namespace PetCare.Web.Dominio;

/// <summary>Código para sumarse al hogar (FR-046, R10).</summary>
public static class CodigoHogar
{
    /// <summary>Letras mayúsculas y dígitos, sin 0, O, 1, I ni L.</summary>
    public const string Alfabeto = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public const int Largo = 8;

    /// <summary>Elige cada carácter con un generador criptográfico.</summary>
    public static string Generar() => new(RandomNumberGenerator.GetItems<char>(Alfabeto, Largo));

    /// <summary>Compara sin distinguir mayúsculas de minúsculas.</summary>
    public static bool Coincide(string? ingresado, string guardado) =>
        string.Equals(ingresado, guardado, StringComparison.OrdinalIgnoreCase);
}
