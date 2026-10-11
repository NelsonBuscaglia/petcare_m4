using System.Text.RegularExpressions;

namespace PetCare.Tests.Infra;

/// <summary>
/// Cliente HTTP de un cuidador: conserva sus cookies, envía el token antiforgery en los POST y
/// puede marcar la solicitud como HTMX.
/// </summary>
public sealed partial class ClienteWeb(HttpClient http)
{
    public Task<HttpResponseMessage> GetAsync(string url, bool htmx = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (htmx) request.Headers.Add("HX-Request", "true");
        return http.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, IDictionary<string, string?> campos, bool htmx = false)
    {
        var token = await TokenAntiforgeryAsync();
        var datos = campos
            .Where(c => c.Value is not null)
            .Select(c => new KeyValuePair<string, string>(c.Key, c.Value!))
            .Append(new("__RequestVerificationToken", token));
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(datos) };
        if (htmx) request.Headers.Add("HX-Request", "true");
        return await http.SendAsync(request);
    }

    public Task<HttpResponseMessage> RegistrarAsync(string email, string password, string? codigoHogar = null) =>
        PostAsync("/Cuenta/Registro", new Dictionary<string, string?>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["CodigoHogar"] = codigoHogar,
        });

    public Task<HttpResponseMessage> IniciarSesionAsync(string email, string password) =>
        PostAsync("/Cuenta/Login", new Dictionary<string, string?> { ["Email"] = email, ["Password"] = password });

    /// <summary>
    /// El token depende de la sesión del cliente, así que se pide uno nuevo antes de cada POST
    /// a la página de login, que no exige sesión.
    /// </summary>
    private async Task<string> TokenAntiforgeryAsync()
    {
        var html = await (await http.GetAsync("/Cuenta/Login")).Content.ReadAsStringAsync();
        var token = TokenRegex().Match(html);
        return token.Success ? token.Groups[1].Value : "";
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();
}
