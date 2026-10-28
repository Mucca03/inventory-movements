using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PruebaBlazorZiur.Configuracion;
using PruebaBlazorZiur.Servicios;

namespace PruebaBlazorZiur.Tests;

public sealed class DocumentoServiceTests
{
    private const string Token = "token-de-prueba";

    [Fact]
    public async Task ObtenerDocumentosAsync_ConRespuestaValida_DeserializaYNormaliza()
    {
        const string json = """
            [
              { "Codigo": 29, "Descripcion": "  Ajuste al Inventario  ", "VActiva": false },
              { "Codigo": 51, "Descripcion": null, "VActiva": true }
            ]
            """;

        var servicio = CrearServicio(Responder(HttpStatusCode.OK, json));

        var documentos = await servicio.ObtenerDocumentosAsync();

        Assert.Equal(2, documentos.Count);
        Assert.Equal("Ajuste al Inventario", documentos[0].Descripcion);
        Assert.Equal("(Sin descripción)", documentos[1].Descripcion);
        Assert.True(documentos[1].VActiva);
    }

    [Fact]
    public async Task ObtenerDocumentosAsync_ConsultaElEndpointConfigurado()
    {
        Uri? uriSolicitada = null;
        var servicio = CrearServicio((solicitud, _) =>
        {
            uriSolicitada = solicitud.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        });

        await servicio.ObtenerDocumentosAsync();

        Assert.Equal("https://ziur.test/api/DocumentosFillsCombos", uriSolicitada?.ToString());
    }

    [Fact]
    public async Task ObtenerDocumentosAsync_ConRespuestaNoContent_DevuelveListaVacia()
    {
        var servicio = CrearServicio((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        var documentos = await servicio.ObtenerDocumentosAsync();

        Assert.Empty(documentos);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "no es válida", false)]
    [InlineData(HttpStatusCode.Unauthorized, "autenticación", false)]
    [InlineData(HttpStatusCode.Forbidden, "autorización", false)]
    [InlineData(HttpStatusCode.NotFound, "endpoint", false)]
    [InlineData(HttpStatusCode.RequestTimeout, "no respondió a tiempo", true)]
    [InlineData(HttpStatusCode.InternalServerError, "error interno", true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "error interno", true)]
    [InlineData(HttpStatusCode.TooManyRequests, "demasiadas solicitudes", true)]
    public async Task ObtenerDocumentosAsync_ConEstadoNoExitoso_LanzaErrorControlado(
        HttpStatusCode estado,
        string fragmentoMensaje,
        bool reintentable)
    {
        var servicio = CrearServicio(Responder(estado, "{}"));

        var excepcion = await Assert.ThrowsAsync<ZiurApiException>(
            () => servicio.ObtenerDocumentosAsync());

        Assert.Equal(estado, excepcion.EstadoHttp);
        Assert.Equal(reintentable, excepcion.EsReintentable);
        Assert.Contains(fragmentoMensaje, excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerDocumentosAsync_ConJsonInvalido_LanzaErrorControlado()
    {
        var servicio = CrearServicio(Responder(HttpStatusCode.OK, "esto no es json"));

        var excepcion = await Assert.ThrowsAsync<ZiurApiException>(
            () => servicio.ObtenerDocumentosAsync());

        Assert.Contains("formato de datos", excepcion.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(excepcion.EsReintentable);
    }

    [Fact]
    public async Task ObtenerDocumentosAsync_ConErrorDeRed_LanzaErrorReintentable()
    {
        var servicio = CrearServicio((_, _) =>
            throw new HttpRequestException("sin conexión"));

        var excepcion = await Assert.ThrowsAsync<ZiurApiException>(
            () => servicio.ObtenerDocumentosAsync());

        Assert.True(excepcion.EsReintentable);
        Assert.Contains("conectar", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerDocumentosAsync_ConTimeout_LanzaErrorReintentable()
    {
        var servicio = CrearServicio(
            async (_, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
            timeout: TimeSpan.FromMilliseconds(60));

        var excepcion = await Assert.ThrowsAsync<ZiurApiException>(
            () => servicio.ObtenerDocumentosAsync());

        Assert.True(excepcion.EsReintentable);
        Assert.Contains("tardó demasiado", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerDocumentosAsync_SinToken_NoLlamaALaApi()
    {
        var llamadaRealizada = false;
        var servicio = CrearServicio(
            (_, _) =>
            {
                llamadaRealizada = true;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            token: " ");

        var excepcion = await Assert.ThrowsAsync<ZiurApiException>(
            () => servicio.ObtenerDocumentosAsync());

        Assert.False(llamadaRealizada);
        Assert.Contains("token", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Responder(
        HttpStatusCode estado,
        string contenido) =>
        (_, _) => Task.FromResult(new HttpResponseMessage(estado)
        {
            Content = new StringContent(contenido, Encoding.UTF8, "application/json")
        });

    private static DocumentoService CrearServicio(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder,
        string token = Token,
        TimeSpan? timeout = null)
    {
        var cliente = new HttpClient(new HandlerFalso(responder))
        {
            BaseAddress = new Uri("https://ziur.test/api/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        };

        var opciones = Options.Create(new ZiurApiOptions
        {
            BaseUrl = "https://ziur.test/api/",
            DocumentosEndpoint = "DocumentosFillsCombos",
            BearerToken = token,
            TimeoutSegundos = 30
        });

        return new DocumentoService(cliente, opciones, NullLogger<DocumentoService>.Instance);
    }

    private sealed class HandlerFalso(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responder(request, cancellationToken);
    }
}
