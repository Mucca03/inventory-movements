using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using PruebaBlazorZiur.Components;
using PruebaBlazorZiur.Configuracion;
using PruebaBlazorZiur.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Opciones de integración con la API de Ziur. El token nunca se escribe en el
// código ni en appsettings: se lee de user-secrets o de variables de entorno.
builder.Services
    .AddOptions<ZiurApiOptions>()
    .Bind(builder.Configuration.GetSection(ZiurApiOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services
    .AddHttpClient<IDocumentoService, DocumentoService>((servicios, cliente) =>
    {
        var opciones = servicios.GetRequiredService<IOptions<ZiurApiOptions>>().Value;

        cliente.BaseAddress = new Uri(opciones.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        cliente.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos);
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", opciones.BearerToken);
        cliente.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        cliente.DefaultRequestHeaders.UserAgent.ParseAdd("PruebaBlazorZiur/1.0");
    });

// Lógica de búsqueda, ordenamiento y paginación: pura y fácil de probar.
builder.Services.AddSingleton<DocumentoConsultaService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Cabeceras de endurecimiento básicas. La API key se usa únicamente en el
// servidor (Blazor Server), por lo que jamás llega al navegador.
app.Use(async (contexto, siguiente) =>
{
    var cabeceras = contexto.Response.Headers;
    cabeceras["X-Content-Type-Options"] = "nosniff";
    cabeceras["X-Frame-Options"] = "DENY";
    cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";
    cabeceras["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    cabeceras["Content-Security-Policy"] =
        "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; " +
        "img-src 'self' data:; font-src 'self' data:; " +
        "script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "connect-src 'self' ws: wss:; form-action 'self'";

    await siguiente();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
