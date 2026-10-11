using Microsoft.EntityFrameworkCore;

namespace PetCare.Tests.Infra;

/// <summary>Verifica que la migración inicial crea el esquema completo (T018).</summary>
public class BaseDeDatosDePruebaTests
{
    [Fact]
    public void El_esquema_se_crea_aplicando_todas_las_migraciones()
    {
        using var baseDePrueba = new BaseDeDatosDePrueba();
        using var db = baseDePrueba.Contexto();

        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.Contains("Inicial", db.Database.GetAppliedMigrations().Single());
    }

    [Fact]
    public void Las_tablas_de_las_siete_entidades_existen_y_estan_vacias()
    {
        using var baseDePrueba = new BaseDeDatosDePrueba();
        using var db = baseDePrueba.Contexto();

        Assert.Empty(db.Hogares);
        Assert.Empty(db.Cuidadores);
        Assert.Empty(db.Mascotas);
        Assert.Empty(db.Deberes);
        Assert.Empty(db.Instancias);
        Assert.Empty(db.RegistrosCumplimiento);
        Assert.Empty(db.Notificaciones);
    }

    [Fact]
    public void Cada_instancia_de_prueba_tiene_su_propia_base()
    {
        using var primera = new BaseDeDatosDePrueba();
        using var segunda = new BaseDeDatosDePrueba();
        using (var db = primera.Contexto())
        {
            db.Hogares.Add(new() { Id = 1, Codigo = "ABCD2345" });
            db.SaveChanges();
        }

        using var dbPrimera = primera.Contexto();
        using var dbSegunda = segunda.Contexto();
        Assert.Single(dbPrimera.Hogares);
        Assert.Empty(dbSegunda.Hogares);
    }
}
