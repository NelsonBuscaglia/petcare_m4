using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Servicios;

/// <summary>Resultado del registro: el cuidador creado o el motivo del rechazo (FR-051).</summary>
public sealed record ResultadoRegistro(Cuidador? Cuidador, string? CodigoCreado, string? Error)
{
    public bool Ok => Error is null;

    public static ResultadoRegistro Rechazo(string motivo) => new(null, null, motivo);
}

public sealed class RegistroService(PetCareDbContext db, RelojHogar reloj)
{
    public const int LargoMinimoPassword = 8;

    private static readonly PasswordHasher<Cuidador> Hasher = new();

    public async Task<ResultadoRegistro> RegistrarAsync(string email, string password, string? codigo)
    {
        var hogar = await db.Hogares.AsNoTracking().SingleOrDefaultAsync();
        if (hogar is null)
        {
            var creado = await CrearHogarAsync(email, password);
            if (creado is not null) return creado;

            // Otro registro creó el hogar al mismo tiempo: se evalúa como hogar existente (FR-048).
            db.ChangeTracker.Clear();
            hogar = await db.Hogares.AsNoTracking().SingleAsync();
        }

        // Con el hogar creado, el código se valida primero, aunque el email ya exista (FR-049, FR-002).
        if (string.IsNullOrEmpty(codigo)) return ResultadoRegistro.Rechazo(Mensajes.HogarExistente);
        if (!CodigoHogar.Coincide(codigo, hogar.Codigo)) return ResultadoRegistro.Rechazo(Mensajes.CodigoIncorrecto);

        var emailNormalizado = Normalizar(email);
        if (await db.Cuidadores.AnyAsync(c => c.EmailNormalizado == emailNormalizado))
            return ResultadoRegistro.Rechazo(Mensajes.EmailRegistrado);

        if (password.Length < LargoMinimoPassword) return ResultadoRegistro.Rechazo(Mensajes.PasswordCorta);

        var cuidador = NuevoCuidador(email, password, hogar.Id);
        db.Cuidadores.Add(cuidador);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // El mismo email se registró al mismo tiempo: lo frena el índice único (FR-003).
            return ResultadoRegistro.Rechazo(Mensajes.EmailRegistrado);
        }

        return new ResultadoRegistro(cuidador, null, null);
    }

    /// <summary>Verifica la contraseña de un cuidador contra su hash.</summary>
    public static bool VerificarPassword(Cuidador cuidador, string password) =>
        Hasher.VerifyHashedPassword(cuidador, cuidador.PasswordHash, password) != PasswordVerificationResult.Failed;

    public static string Normalizar(string email) => email.ToUpperInvariant();

    /// <summary>
    /// Crea el hogar (FR-001) y su primer cuidador. Devuelve null si otro registro creó el hogar
    /// primero: la clave fija <c>Id = 1</c> hace chocar la segunda inserción (R8).
    /// </summary>
    private async Task<ResultadoRegistro?> CrearHogarAsync(string email, string password)
    {
        if (password.Length < LargoMinimoPassword) return ResultadoRegistro.Rechazo(Mensajes.PasswordCorta);

        var hogar = new Hogar { Id = 1, Codigo = CodigoHogar.Generar(), CreadoUtc = reloj.AhoraUtc };
        var cuidador = NuevoCuidador(email, password, hogar.Id);
        db.Hogares.Add(hogar);
        db.Cuidadores.Add(cuidador);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return null;
        }

        return new ResultadoRegistro(cuidador, hogar.Codigo, null);
    }

    private Cuidador NuevoCuidador(string email, string password, int hogarId)
    {
        var cuidador = new Cuidador
        {
            HogarId = hogarId,
            Email = email,
            EmailNormalizado = Normalizar(email),
            CreadoUtc = reloj.AhoraUtc,
        };
        cuidador.PasswordHash = Hasher.HashPassword(cuidador, password);
        return cuidador;
    }
}
