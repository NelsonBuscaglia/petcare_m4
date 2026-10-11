using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Servicios;

public class EstadoDelDiaQueryTests : IDisposable
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly BaseDeDatosDePrueba _base = new();

    public EstadoDelDiaQueryTests()
    {
        _base.Fijar(Hoy, new TimeOnly(7, 0));
        Semillas.Hogar(_base);
    }

    public void Dispose() => _base.Dispose();

    private async Task<EstadoDelDia> Estado()
    {
        await using var db = _base.Contexto();
        return await new EstadoDelDiaQuery(db, _base.Reloj).ObtenerAsync();
    }

    /// <summary>Un deber de hora fija diario con una instancia por cada (día, hora, estado) indicado.</summary>
    private int DeberConInstancias(int mascotaId, TipoDeber tipo, params (DateOnly Dia, string Hora, EstadoInstancia Estado)[] instancias)
    {
        using var db = _base.Contexto();
        var deber = new Deber
        {
            MascotaId = mascotaId,
            Tipo = tipo,
            Modalidad = Modalidad.HoraFija,
            HoraInicio = TimeOnly.Parse(instancias[0].Hora),
            Recurrencia = Recurrencia.TodosLosDias,
            CreadoUtc = _base.Reloj.AhoraUtc,
            CreadoDia = Hoy,
        };
        db.Deberes.Add(deber);
        foreach (var (dia, hora, estado) in instancias)
        {
            var horaDelDia = TimeOnly.Parse(hora);
            db.Instancias.Add(new Instancia
            {
                Deber = deber,
                Dia = dia,
                Hora = horaDelDia,
                ProgramadaUtc = RelojHogar.AUtc(dia, horaDelDia),
                Estado = estado,
                GeneradaUtc = _base.Reloj.AhoraUtc,
            });
        }
        db.SaveChanges();
        return deber.Id;
    }

    [Fact]
    public async Task FR_033_cada_mascota_tiene_su_seccion_con_las_instancias_vigentes_del_dia_ordenadas_por_hora()
    {
        var firulais = Semillas.Mascota(_base, "Firulais");
        DeberConInstancias(firulais, TipoDeber.Comida, (Hoy, "20:00", EstadoInstancia.Vigente));
        DeberConInstancias(firulais, TipoDeber.Paseo, (Hoy, "09:00", EstadoInstancia.Vigente));
        DeberConInstancias(firulais, TipoDeber.Medicacion, (Hoy, "13:00", EstadoInstancia.Vigente));

        var seccion = Assert.Single((await Estado()).Mascotas);

        Assert.Equal(firulais, seccion.MascotaId);
        Assert.Equal("Firulais", seccion.Nombre);
        Assert.Equal(["09:00", "13:00", "20:00"], seccion.Instancias.Select(i => i.Hora.ToString("HH:mm")));
        Assert.Equal([TipoDeber.Paseo, TipoDeber.Medicacion, TipoDeber.Comida], seccion.Instancias.Select(i => i.Tipo));
        Assert.All(seccion.Instancias, i => Assert.Equal(Hoy, i.Dia));
    }

    [Fact]
    public async Task FR_033_solo_se_muestran_las_instancias_vigentes()
    {
        var firulais = Semillas.Mascota(_base, "Firulais");
        DeberConInstancias(firulais, TipoDeber.Paseo,
            (Hoy, "08:00", EstadoInstancia.Cumplida),
            (Hoy, "12:00", EstadoInstancia.DadaDeBaja),
            (Hoy, "16:00", EstadoInstancia.Vigente));

        var seccion = Assert.Single((await Estado()).Mascotas);

        Assert.Equal(["16:00"], seccion.Instancias.Select(i => i.Hora.ToString("HH:mm")));
    }

    [Fact]
    public async Task FR_033_no_se_muestran_instancias_de_dias_futuros()
    {
        var firulais = Semillas.Mascota(_base, "Firulais");
        DeberConInstancias(firulais, TipoDeber.Paseo, (Hoy.AddDays(1), "09:00", EstadoInstancia.Vigente));

        Assert.Empty(Assert.Single((await Estado()).Mascotas).Instancias);
    }

    [Fact]
    public async Task AC_09_cada_seccion_muestra_solo_las_instancias_de_su_mascota()
    {
        var a = Semillas.Mascota(_base, "A");
        var b = Semillas.Mascota(_base, "B");
        DeberConInstancias(a, TipoDeber.Paseo, (Hoy, "08:00", EstadoInstancia.Vigente));

        var estado = await Estado();

        Assert.Single(estado.Mascotas.Single(m => m.MascotaId == a).Instancias);
        Assert.Empty(estado.Mascotas.Single(m => m.MascotaId == b).Instancias);
    }

    [Fact]
    public async Task AC_65_sin_mascotas_el_estado_del_dia_lo_indica()
    {
        var estado = await Estado();

        Assert.True(estado.SinMascotas);
        Assert.Empty(estado.Mascotas);
    }

    [Fact]
    public async Task AC_02_el_estado_del_dia_es_del_hogar_y_lo_ve_cualquier_cuidador()
    {
        // La consulta no depende de qué cuidador la pide: el segundo cuidador ve la sección de A.
        using (var db = _base.Contexto())
        {
            db.Cuidadores.Add(new Cuidador { HogarId = 1, Email = "ana@ejemplo.com", EmailNormalizado = "ANA@EJEMPLO.COM" });
            db.Cuidadores.Add(new Cuidador { HogarId = 1, Email = "beto@ejemplo.com", EmailNormalizado = "BETO@EJEMPLO.COM" });
            db.SaveChanges();
        }
        Semillas.Mascota(_base, "A");

        var estado = await Estado();

        Assert.False(estado.SinMascotas);
        Assert.Equal("A", Assert.Single(estado.Mascotas).Nombre);
    }
}
