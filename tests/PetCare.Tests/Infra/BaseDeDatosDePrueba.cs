using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;

namespace PetCare.Tests.Infra;

/// <summary>
/// Base SQLite en memoria para un test. La conexión queda abierta mientras dure el test (si se
/// cierra, la base desaparece) y el esquema se crea aplicando las migraciones de
/// <see cref="PetCareDbContext"/>, nunca con <c>EnsureCreated()</c> (R11).
/// </summary>
public sealed class BaseDeDatosDePrueba : IDisposable
{
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    public BaseDeDatosDePrueba()
    {
        _conexion.Open();
        using var db = Contexto();
        db.Database.Migrate();
    }

    /// <summary>Conexión abierta, para que la app de los tests web use esta misma base.</summary>
    public SqliteConnection Conexion => _conexion;

    /// <summary>Un contexto nuevo sobre la misma base, como en una solicitud distinta.</summary>
    public PetCareDbContext Contexto() =>
        new(new DbContextOptionsBuilder<PetCareDbContext>().UseSqlite(_conexion).Options);

    public void Dispose() => _conexion.Dispose();
}
