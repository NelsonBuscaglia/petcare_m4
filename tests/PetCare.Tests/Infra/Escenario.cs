using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;
using PetCare.Web.Programado;
using PetCare.Web.Servicios;

namespace PetCare.Tests.Infra;

/// <summary>
/// Un hogar de prueba que se maneja con los servicios reales, para los tests que recorren varios
/// pasos (alta, ciclos del procesador, cumplimientos, consulta). Cada operación usa un contexto
/// nuevo, como una solicitud distinta. El reloj arranca en <see cref="Inicio"/>.
/// </summary>
public sealed class Escenario : IDisposable
{
    public static readonly DateOnly Inicio = new(2026, 10, 1);

    public Escenario()
    {
        Base.Fijar(Inicio, new TimeOnly(0, 0));
        Semillas.Hogar(Base);
    }

    public BaseDeDatosDePrueba Base { get; } = new();

    public void Dispose() => Base.Dispose();

    /// <summary>Lleva el reloj al día y la hora del hogar indicados (no puede volver atrás).</summary>
    public void Fijar(DateOnly dia, int hora, int minuto = 0) => Base.Fijar(dia, new TimeOnly(hora, minuto));

    public int Mascota(string nombre = "Firulais") => Semillas.Mascota(Base, nombre);

    public async Task<int> Programable(int mascotaId, TipoDeber tipo, string hora, int? cadaNHoras = null,
        Recurrencia recurrencia = Recurrencia.TodosLosDias, DateOnly? hasta = null)
    {
        await using var db = Base.Contexto();
        var resultado = await new DeberesService(db, Base.Reloj).AltaProgramableAsync(new AltaProgramable(
            mascotaId, tipo, cadaNHoras is null ? Modalidad.HoraFija : Modalidad.CadaNHoras,
            TimeOnly.Parse(hora), cadaNHoras, recurrencia, hasta));
        Assert.True(resultado.Ok, resultado.Error);
        return resultado.Valor;
    }

    public async Task<Resultado<int>> AltaPuntual(int mascotaId, TipoDeber tipo, DateOnly dia, string hora, SubtipoVisita? subtipo = null)
    {
        await using var db = Base.Contexto();
        return await new DeberesService(db, Base.Reloj).AltaPuntualAsync(new AltaPuntual(mascotaId, tipo, subtipo, dia, TimeOnly.Parse(hora)));
    }

    public async Task<int> Puntual(int mascotaId, TipoDeber tipo, DateOnly dia, string hora, SubtipoVisita? subtipo = null)
    {
        var resultado = await AltaPuntual(mascotaId, tipo, dia, hora, subtipo);
        Assert.True(resultado.Ok, resultado.Error);
        return resultado.Valor;
    }

    public async Task<IReadOnlyList<Deber>> DeberesActivos(int mascotaId)
    {
        await using var db = Base.Contexto();
        return await new DeberesService(db, Base.Reloj).DeberesActivosAsync(mascotaId);
    }

    public async Task DarDeBajaDeber(int deberId)
    {
        await using var db = Base.Contexto();
        await new DeberesService(db, Base.Reloj).DarDeBajaAsync(deberId);
    }

    public async Task<Resultado> DarDeBajaInstancia(int instanciaId)
    {
        await using var db = Base.Contexto();
        return await new BajasInstanciaService(db, Base.Reloj).DarDeBajaAsync(instanciaId);
    }

    /// <summary>Un ciclo del procesador programado a la hora actual del reloj.</summary>
    public async Task Ciclo()
    {
        await using var db = Base.Contexto();
        await new ProcesadorProgramado(db, Base.Reloj, NullLogger<ProcesadorProgramado>.Instance)
            .EjecutarCicloAsync(CancellationToken.None);
    }

    public async Task<Instancia> Instancia(int deberId, DateOnly dia, string hora)
    {
        await using var db = Base.Contexto();
        var horaDelDia = TimeOnly.Parse(hora);
        return await db.Instancias.AsNoTracking().SingleAsync(i => i.DeberId == deberId && i.Dia == dia && i.Hora == horaDelDia);
    }

    public async Task<Resultado<int>> Cumplir(int instanciaId, string? hora = null)
    {
        await using var db = Base.Contexto();
        return await new CumplimientosService(db, Base.Reloj).CumplirAsync(instanciaId, hora is null ? null : TimeOnly.Parse(hora));
    }

    public async Task<int> CumplirOk(int instanciaId, string? hora = null)
    {
        var resultado = await Cumplir(instanciaId, hora);
        Assert.True(resultado.Ok, resultado.Error);
        return resultado.Valor;
    }

    public async Task<Resultado> Eliminar(int registroId)
    {
        await using var db = Base.Contexto();
        return await new CumplimientosService(db, Base.Reloj).EliminarAsync(registroId);
    }

    public async Task<SeccionMascota> Seccion(int mascotaId)
    {
        await using var db = Base.Contexto();
        return (await new EstadoDelDiaQuery(db, Base.Reloj).ObtenerAsync()).Mascotas.Single(m => m.MascotaId == mascotaId);
    }
}
