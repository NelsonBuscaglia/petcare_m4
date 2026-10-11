namespace PetCare.Web.Servicios;

/// <summary>Resultado de una operación que devuelve un valor o se rechaza con un motivo (FR-051).</summary>
public sealed record Resultado<T>(T Valor, string? Error)
{
    public bool Ok => Error is null;

    public static Resultado<T> Exito(T valor) => new(valor, null);

    public static Resultado<T> Rechazo(string motivo) => new(default!, motivo);
}
