using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PetCare.Tests.Infra;
using PetCare.Web.Datos;
using PetCare.Web.Email;
using PetCare.Web.Programado;

namespace PetCare.Tests.Web;

/// <summary>
/// Qué servicio de email registra la app según el entorno (R7, Principio II): en desarrollo
/// escribe en la consola; fuera de desarrollo exige la configuración SMTP o no arranca.
/// </summary>
public class ConfiguracionEmailTests
{
    /// <summary>La app tal como arranca, con la base de prueba y sin el procesador en segundo plano.</summary>
    private sealed class App(string entorno, IDictionary<string, string?> configuracion) : WebApplicationFactory<Program>
    {
        private readonly BaseDeDatosDePrueba _base = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(entorno);
            builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(configuracion));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<PetCareDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<PetCareDbContext>>();
                services.AddDbContext<PetCareDbContext>(o => o.UseSqlite(_base.Conexion));
                services.Remove(services.Single(s => s.ImplementationType == typeof(ProgramadorHostedService)));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _base.Dispose();
        }
    }

    /// <summary>Configuración SMTP vacía, para que la prueba no dependa de la máquina en que corre.</summary>
    private static Dictionary<string, string?> SinSmtp() => new()
    {
        ["Smtp:Host"] = "", ["Smtp:Port"] = "", ["Smtp:User"] = "", ["Smtp:Password"] = "", ["Smtp:From"] = "",
    };

    [Fact]
    public void Principio_II_en_desarrollo_los_emails_se_escriben_en_la_consola()
    {
        using var app = new App("Development", SinSmtp());

        Assert.IsType<ConsoleEmailSender>(app.Services.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void Principio_II_fuera_de_desarrollo_sin_configuracion_SMTP_la_app_no_arranca_e_informa_la_falta()
    {
        using var app = new App("Production", SinSmtp());

        var error = Assert.ThrowsAny<Exception>(() => app.Services);

        Assert.Contains("Smtp", error.ToString());
    }

    [Fact]
    public void R7_fuera_de_desarrollo_con_la_configuracion_SMTP_se_usa_el_envio_real()
    {
        using var app = new App("Production", new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp.ejemplo.com", ["Smtp:Port"] = "587", ["Smtp:User"] = "usuario",
            ["Smtp:Password"] = "clave-de-prueba", ["Smtp:From"] = "petcare@ejemplo.com",
        });

        Assert.IsType<SmtpEmailSender>(app.Services.GetRequiredService<IEmailSender>());
    }
}
