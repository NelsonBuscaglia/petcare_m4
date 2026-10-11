using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Infra;

/// <summary>
/// Base SQLite en memoria para un test. La conexión queda abierta mientras dure el test (si se
/// cierra, la base desaparece) y el esquema se crea aplicando las migraciones de
/// <see cref="PetCareDbContext"/>, nunca con <c>EnsureCreated()</c> (R11).
/// Con <c>enArchivo</c>, la base vive en un archivo temporal y cada contexto abre su propia
/// conexión: una conexión en memoria no admite operaciones realmente simultáneas, que son las
/// que necesitan probar FR-028 y FR-048.
/// </summary>
public sealed class BaseDeDatosDePrueba : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly string? _archivo;

    public BaseDeDatosDePrueba(bool enArchivo = false)
    {
        if (enArchivo)
        {
            _archivo = Path.Combine(Path.GetTempPath(), $"petcare-test-{Guid.NewGuid():N}.db");
            _conexion = new SqliteConnection(CadenaArchivo());
        }
        else
        {
            _conexion = new SqliteConnection("DataSource=:memory:");
        }

        _conexion.Open();
        using var db = Contexto();
        db.Database.Migrate();
    }

    /// <summary>Reloj controlable de los servicios bajo prueba.</summary>
    public FakeTimeProvider Tiempo { get; } = new();

    public RelojHogar Reloj => new(Tiempo);

    /// <summary>Conexión abierta, para que la app de los tests web use esta misma base.</summary>
    public SqliteConnection Conexion => _conexion;

    /// <summary>Un contexto nuevo sobre la misma base, como en una solicitud distinta.</summary>
    public PetCareDbContext Contexto()
    {
        var opciones = new DbContextOptionsBuilder<PetCareDbContext>();
        if (_archivo is null) opciones.UseSqlite(_conexion);
        else opciones.UseSqlite(CadenaArchivo());
        return new PetCareDbContext(opciones.Options);
    }

    /// <summary>Lleva el reloj al día y la hora indicados, en hora del hogar (UTC−3).</summary>
    public void Fijar(DateOnly dia, TimeOnly hora) => Tiempo.SetUtcNow(RelojHogar.AUtc(dia, hora));

    private string CadenaArchivo() => $"Data Source={_archivo};Pooling=False;Default Timeout=30";

    public void Dispose()
    {
        _conexion.Dispose();
        if (_archivo is null) return;
        foreach (var sufijo in new[] { "", "-journal", "-wal", "-shm" })
            File.Delete(_archivo + sufijo);
    }
}
