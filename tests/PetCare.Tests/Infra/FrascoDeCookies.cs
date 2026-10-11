namespace PetCare.Tests.Infra;

/// <summary>
/// Guarda las cookies que manda el servidor sin aplicarles el vencimiento según el reloj real:
/// en los tests, si la sesión venció lo decide el servidor con el reloj falso. Solo descarta
/// las cookies que el servidor borra (valor vacío o fecha de 1970, como al cerrar sesión).
/// </summary>
public sealed class FrascoDeCookies : DelegatingHandler
{
    private readonly Dictionary<string, string> _cookies = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (_cookies.Count > 0)
            request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(c => $"{c.Key}={c.Value}")));

        var response = await base.SendAsync(request, ct);

        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var setCookie in setCookies)
            {
                var partes = setCookie.Split(';', StringSplitOptions.TrimEntries);
                var igual = partes[0].IndexOf('=');
                var (nombre, valor) = (partes[0][..igual], partes[0][(igual + 1)..]);
                var borrada = valor.Length == 0
                    || partes.Any(p => p.StartsWith("expires=", StringComparison.OrdinalIgnoreCase) && p.Contains("1970"));

                if (borrada) _cookies.Remove(nombre);
                else _cookies[nombre] = valor;
            }
        }

        return response;
    }
}
