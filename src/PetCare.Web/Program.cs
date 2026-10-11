using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PetCare.Web.Datos;
using PetCare.Web.Dominio;

var builder = WebApplication.CreateBuilder(args);

// Toda hora sale del TimeProvider inyectado (AGENTS.md, R2).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<RelojHogar>();

builder.Services.AddDbContext<PetCareDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PetCare")));

// Sesión de 30 días desde la última solicitud (FR-005, R9). La renovación y la respuesta sin
// sesión se configuran en los eventos de la cookie (US1).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = false;
    });
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
