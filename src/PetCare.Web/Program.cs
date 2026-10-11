using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;
using PetCare.Web.Email;
using PetCare.Web.Infra;
using PetCare.Web.Programado;
using PetCare.Web.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Toda hora sale del TimeProvider inyectado (AGENTS.md, R2).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<RelojHogar>();

builder.Services.AddDbContext<PetCareDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PetCare")));

builder.Services.AddScoped<RegistroService>();
builder.Services.AddScoped<MascotasService>();
builder.Services.AddScoped<DeberesService>();
builder.Services.AddScoped<CumplimientosService>();
builder.Services.AddScoped<BajasInstanciaService>();
builder.Services.AddScoped<EstadoDelDiaQuery>();

// Emails: en desarrollo se escriben en la consola; en los demás entornos se envían por SMTP con
// la configuración de variables de entorno (Smtp__Host, …) o user-secrets, nunca de
// appsettings*.json. Si falta, la app no arranca e informa qué falta (Principio II, R7).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
}
else
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
    builder.Services.AddOptions<SmtpOptions>()
        .Bind(builder.Configuration.GetSection(SmtpOptions.Seccion))
        .ValidateOnStart();
    builder.Services.AddSingleton<IValidateOptions<SmtpOptions>, ValidacionSmtpOptions>();
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}

builder.Services.AddScoped<NotificacionesService>();

// Procesador en segundo plano: genera instancias y envía emails sin depender de usuarios conectados (R5).
builder.Services.AddScoped<ProcesadorProgramado>();
builder.Services.AddHostedService<ProgramadorHostedService>();

// Los textos en español (á, ñ, ¿…) se escriben tal cual en el HTML, sin convertirlos en entidades.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Sesión de 30 días desde la última solicitud (FR-005, R9). La renovación y la respuesta sin
// sesión están en EventosCookie.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = false;
        options.EventsType = typeof(EventosCookie);
    });
builder.Services.AddSingleton<EventosCookie>();
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<TimeProvider>((options, tiempo) => options.TimeProvider = tiempo);

// Toda página exige sesión salvo registro e inicio de sesión (FR-006).
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Cuenta/Registro");
    options.Conventions.AllowAnonymousToPage("/Cuenta/Login");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Expuesta para WebApplicationFactory en los tests.
public partial class Program;
