namespace PetCare.Web.Programado;

/// <summary>
/// Ejecuta un ciclo de <see cref="ProcesadorProgramado"/> al arrancar y después cada 30 s
/// (R5). El temporizador sale del <see cref="TimeProvider"/> inyectado.
/// </summary>
public sealed class ProgramadorHostedService(
    IServiceScopeFactory scopes,
    TimeProvider tiempo,
    ILogger<ProgramadorHostedService> logger) : BackgroundService
{
    public static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo, tiempo);
        try
        {
            do
            {
                await EjecutarCicloAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La app se está deteniendo.
        }
    }

    private async Task EjecutarCicloAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ProcesadorProgramado>().EjecutarCicloAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Un ciclo fallido no detiene al procesador: el siguiente vuelve a intentar.
            logger.LogError(ex, "Falló un ciclo del procesador programado.");
        }
    }
}
