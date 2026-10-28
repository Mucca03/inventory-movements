using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PruebaBlazorZiur.Configuracion;
using PruebaBlazorZiur.Modelos;

namespace PruebaBlazorZiur.Servicios;

/// <summary>
/// Consume el endpoint DocumentosFillsCombos de la API de Ziur y traduce los
/// fallos técnicos a errores controlados para la interfaz.
/// </summary>
public sealed class DocumentoService : IDocumentoService
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptions<ZiurApiOptions> _opciones;
    private readonly ILogger<DocumentoService> _logger;

    public DocumentoService(
        HttpClient httpClient,
        IOptions<ZiurApiOptions> opciones,
        ILogger<DocumentoService> logger)
    {
        _httpClient = httpClient;
        _opciones = opciones;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Documento>> ObtenerDocumentosAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_opciones.Value.BearerToken))
        {
            throw new ZiurApiException(
                "No se configuró el token de acceso a la API de Ziur. " +
                "Define ZiurApi:BearerToken mediante user-secrets o una variable de entorno.");
        }

        try
        {
            using var respuesta = await _httpClient.GetAsync(
                _opciones.Value.DocumentosEndpoint,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "La API de Ziur respondió {EstadoHttp} al consultar los documentos.",
                    (int)respuesta.StatusCode);

                throw ZiurApiException.PorEstado(respuesta.StatusCode);
            }

            if (respuesta.StatusCode == HttpStatusCode.NoContent)
            {
                return Array.Empty<Documento>();
            }

            await using var contenido = await respuesta.Content.ReadAsStreamAsync(cancellationToken);

            var recibidos = await JsonSerializer.DeserializeAsync<List<Documento?>>(
                contenido,
                OpcionesJson,
                cancellationToken);

            if (recibidos is null)
            {
                throw new ZiurApiException(
                    "La API de Ziur devolvió una respuesta vacía o inválida.");
            }

            return recibidos
                .OfType<Documento>()
                .Select(documento => documento with
                {
                    Descripcion = string.IsNullOrWhiteSpace(documento.Descripcion)
                        ? "(Sin descripción)"
                        : documento.Descripcion.Trim()
                })
                .ToList();
        }
        catch (ZiurApiException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Se agotó el tiempo de espera ({Timeout} s) al consultar la API de Ziur.",
                _opciones.Value.TimeoutSegundos);

            throw new ZiurApiException(
                "La API de Ziur tardó demasiado en responder. Intenta de nuevo.",
                esReintentable: true);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red al consultar la API de Ziur.");

            throw new ZiurApiException(
                "No fue posible conectar con la API de Ziur. Verifica la conexión e intenta de nuevo.",
                esReintentable: true,
                innerException: ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "La API de Ziur devolvió un JSON que no se pudo interpretar.");

            throw new ZiurApiException(
                "La API de Ziur devolvió un formato de datos no esperado.",
                innerException: ex);
        }
    }
}
