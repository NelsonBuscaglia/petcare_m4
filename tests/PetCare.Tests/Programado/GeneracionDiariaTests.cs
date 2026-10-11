using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PetCare.Tests.Infra;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Programado;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Programado;

/// <summary>Generación de las instancias del día por el procesador programado (FR-017, R4, R5).</summary>
public class GeneracionDiariaTests : IDisposable
{
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private readonly BaseDeDatosDePrueba _base = new();
    private readonly int _firulais;

    public GeneracionDiariaTests()
    {
        _base.Fijar(Ayer, new TimeOnly(10, 0));
        Semillas.Hogar(_base);
        _firulais = Semillas.Mascota(_base, "Firulais");
    }

    public void Dispose() => _base.Dispose();

    private async Task Ciclo()
    {
        await using var db = _base.Contexto();
        var notificaciones = new NotificacionesService(
            db, _base.Reloj, new FakeEmailSender(_base.Tiempo), NullLogger<NotificacionesService>.Instance);
        await new ProcesadorProgramado(db, _base.Reloj, notificaciones, NullLogger<ProcesadorProgramado>.Instance)
            .EjecutarCicloAsync(CancellationToken.None);
    }

    /// <summary>Deber programable dado de alta ayer, sin instancias generadas.</summary>
    private int Deber(TipoDeber tipo, string hora, int? cadaNHoras = null,
        Recurrencia recurrencia = Recurrencia.TodosLosDias, DateOnly? hasta = null, DateTime? dadoDeBaja = null)
    {
        using var db = _base.Contexto();
        var deber = new Deber
        {
            MascotaId = _firulais,
            Tipo = tipo,
            Modalidad = cadaNHoras is null ? Modalidad.HoraFija : Modalidad.CadaNHoras,
            HoraInicio = TimeOnly.Parse(hora),
            IntervaloHoras = cadaNHoras,
            Recurrencia = recurrencia,
            HastaDia = hasta,
            CreadoUtc = _base.Reloj.AhoraUtc,
            CreadoDia = Ayer,
            DadoDeBajaUtc = dadoDeBaja,
        };
        db.Deberes.Add(deber);
        db.SaveChanges();
        return deber.Id;
    }

    private List<Instancia> InstanciasDe(int deberId, DateOnly dia)
    {
        using var db = _base.Contexto();
        return db.Instancias.AsNoTracking()
            .Where(i => i.DeberId == deberId && i.Dia == dia)
            .OrderBy(i => i.ProgramadaUtc)
            .ToList();
    }

    [Fact]
    public async Task FR_017_un_ciclo_despues_de_las_00_genera_las_instancias_del_dia_de_los_deberes_diarios()
    {
        var paseo = Deber(TipoDeber.Paseo, "08:00", cadaNHoras: 4);

        _base.Fijar(Hoy, new TimeOnly(0, 1));
        await Ciclo();

        var instancias = InstanciasDe(paseo, Hoy);
        Assert.Equal(["08:00", "12:00", "16:00", "20:00"], instancias.Select(i => i.Hora.ToString("HH:mm")));
        Assert.All(instancias, i => Assert.Equal(EstadoInstancia.Vigente, i.Estado));
        Assert.All(instancias, i => Assert.Equal(RelojHogar.AUtc(Hoy, i.Hora), i.ProgramadaUtc));
        Assert.All(instancias, i => Assert.False(i.GeneradaVencida));
    }

    [Fact]
    public async Task R4_ejecutar_el_ciclo_dos_veces_no_duplica_instancias()
    {
        var paseo = Deber(TipoDeber.Paseo, "08:00", cadaNHoras: 4);
        _base.Fijar(Hoy, new TimeOnly(0, 1));

        await Ciclo();
        await Ciclo();

        Assert.Equal(4, InstanciasDe(paseo, Hoy).Count);
    }

    [Fact]
    public async Task R4_el_ciclo_completa_solo_las_instancias_que_faltan()
    {
        var paseo = Deber(TipoDeber.Paseo, "08:00", cadaNHoras: 4);
        using (var db = _base.Contexto())
        {
            db.Instancias.Add(new Instancia
            {
                DeberId = paseo, Dia = Hoy, Hora = new TimeOnly(8, 0),
                ProgramadaUtc = RelojHogar.AUtc(Hoy, new TimeOnly(8, 0)), GeneradaUtc = _base.Reloj.AhoraUtc,
            });
            db.SaveChanges();
        }
        _base.Fijar(Hoy, new TimeOnly(0, 1));

        await Ciclo();

        Assert.Equal(["08:00", "12:00", "16:00", "20:00"], InstanciasDe(paseo, Hoy).Select(i => i.Hora.ToString("HH:mm")));
    }

    [Fact]
    public async Task AC_12_un_deber_solo_para_hoy_no_genera_instancias_al_dia_siguiente()
    {
        var comida = Deber(TipoDeber.Comida, "20:00", recurrencia: Recurrencia.SoloHoy);

        _base.Fijar(Hoy, new TimeOnly(0, 1));
        await Ciclo();

        Assert.Empty(InstanciasDe(comida, Hoy));
    }

    [Fact]
    public async Task AC_13b_una_medicacion_con_fecha_de_fin_ayer_no_genera_hoy()
    {
        var medicacion = Deber(TipoDeber.Medicacion, "09:00", hasta: Ayer);

        _base.Fijar(Hoy, new TimeOnly(0, 1));
        await Ciclo();

        Assert.Empty(InstanciasDe(medicacion, Hoy));
    }

    [Fact]
    public async Task FR_013_un_deber_dado_de_baja_no_genera_instancias()
    {
        var paseo = Deber(TipoDeber.Paseo, "09:00", dadoDeBaja: _base.Reloj.AhoraUtc);

        _base.Fijar(Hoy, new TimeOnly(0, 1));
        await Ciclo();

        Assert.Empty(InstanciasDe(paseo, Hoy));
    }
}
