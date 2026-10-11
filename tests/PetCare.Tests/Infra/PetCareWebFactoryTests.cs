using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;

namespace PetCare.Tests.Infra;

/// <summary>Verifica que la factory de tests arranca la app con el reloj y la base de prueba.</summary>
public class PetCareWebFactoryTests
{
    [Fact]
    public async Task La_app_arranca_y_exige_sesion()
    {
        using var app = new PetCareWebFactory();

        var response = await app.Cliente().GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public void La_app_usa_el_reloj_falso()
    {
        using var app = new PetCareWebFactory();
        app.Fijar(new DateOnly(2026, 10, 10), new TimeOnly(9, 30));

        var reloj = app.Services.GetRequiredService<RelojHogar>();

        Assert.Equal(new DateOnly(2026, 10, 10), reloj.Hoy);
        Assert.Equal(new TimeOnly(9, 30), reloj.HoraActual);
    }

    [Fact]
    public void La_app_usa_la_base_de_prueba()
    {
        using var app = new PetCareWebFactory();
        using (var db = app.Base.Contexto())
        {
            db.Hogares.Add(new() { Id = 1, Codigo = "ABCD2345" });
            db.SaveChanges();
        }

        using var scope = app.Services.CreateScope();
        var dbDeLaApp = scope.ServiceProvider.GetRequiredService<PetCareDbContext>();

        Assert.Equal("ABCD2345", dbDeLaApp.Hogares.Single().Codigo);
    }
}
