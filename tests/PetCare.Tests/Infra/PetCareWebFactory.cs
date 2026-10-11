using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Infra;

/// <summary>
/// La app completa para tests web: el <see cref="TimeProvider"/> es un
/// <see cref="FakeTimeProvider"/> y la base es una <see cref="BaseDeDatosDePrueba"/> en memoria.
/// </summary>
public sealed class PetCareWebFactory : WebApplicationFactory<Program>
{
    public BaseDeDatosDePrueba Base { get; } = new();

    public FakeTimeProvider Tiempo { get; } = new();

    /// <summary>Lleva el reloj al día y la hora indicados, en hora del hogar (UTC−3).</summary>
    public void Fijar(DateOnly dia, TimeOnly hora) => Tiempo.SetUtcNow(RelojHogar.AUtc(dia, hora));

    /// <summary>Un navegador nuevo, con sus propias cookies y sin seguir redirecciones.</summary>
    public ClienteWeb Cliente() => new(CreateDefaultClient(new Uri("http://localhost"), new FrascoDeCookies()));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Tiempo);

            services.RemoveAll<DbContextOptions<PetCareDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PetCareDbContext>>();
            services.AddDbContext<PetCareDbContext>(options => options.UseSqlite(Base.Conexion));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) Base.Dispose();
    }
}
