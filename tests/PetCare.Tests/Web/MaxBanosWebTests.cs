using System.Net;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Web;

/// <summary>Máximo de días entre baños desde la configuración de deberes (FR-008, RF-05c).</summary>
public class MaxBanosWebTests : IDisposable
{
    private readonly PetCareWebFactory _app = new();

    public MaxBanosWebTests() => _app.Fijar(new DateOnly(2026, 10, 10), new TimeOnly(9, 0));

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task RF_05c_un_valor_no_entero_se_rechaza_con_el_motivo_y_el_maximo_no_cambia()
    {
        var ana = _app.Cliente();
        await ana.RegistrarAsync("ana@ejemplo.com", "secreta123");
        var firulais = Semillas.Mascota(_app.Base, "Firulais");
        using (var db = _app.Base.Contexto())
        {
            db.Mascotas.Single(m => m.Id == firulais).MaxDiasEntreBanos = 20;
            db.SaveChanges();
        }

        var response = await ana.PostAsync($"/Mascotas/{firulais}/Deberes?handler=MaxBanos",
            new Dictionary<string, string?> { ["MaxDiasEntreBanos"] = "2,5" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("El máximo de días entre baños debe ser un número entero mayor que 0.", await response.Content.ReadAsStringAsync());
        using var verificacion = _app.Base.Contexto();
        Assert.Equal(20, verificacion.Mascotas.Single(m => m.Id == firulais).MaxDiasEntreBanos);
    }
}
