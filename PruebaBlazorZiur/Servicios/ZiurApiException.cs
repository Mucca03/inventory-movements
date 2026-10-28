using System.Net;

namespace PruebaBlazorZiur.Servicios;

/// <summary>
/// Error controlado de la integración con Ziur. El mensaje está pensado para
/// mostrarse al usuario; los detalles técnicos se registran en el log.
/// </summary>
public sealed class ZiurApiException : Exception
{
    public ZiurApiException(
        string message,
        HttpStatusCode? estadoHttp = null,
        bool esReintentable = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        EstadoHttp = estadoHttp;
        EsReintentable = esReintentable;
    }

    public HttpStatusCode? EstadoHttp { get; }

    /// <summary>
    /// Indica si tiene sentido ofrecer al usuario un nuevo intento.
    /// </summary>
    public bool EsReintentable { get; }

    public static ZiurApiException PorEstado(HttpStatusCode estado) => estado switch
    {
        HttpStatusCode.BadRequest => new ZiurApiException(
            "La solicitud a la API de Ziur no es válida. Revisa la configuración del endpoint.",
            estado),
        HttpStatusCode.Unauthorized => new ZiurApiException(
            "La API de Ziur rechazó la autenticación. Verifica que el token configurado sea válido.",
            estado),
        HttpStatusCode.Forbidden => new ZiurApiException(
            "No tienes autorización para consultar los documentos en la API de Ziur.",
            estado),
        HttpStatusCode.NotFound => new ZiurApiException(
            "No se encontró el endpoint de documentos en la API de Ziur. Revisa la URL configurada.",
            estado),
        HttpStatusCode.RequestTimeout => new ZiurApiException(
            "La API de Ziur no respondió a tiempo. Intenta de nuevo.",
            estado,
            esReintentable: true),
        HttpStatusCode.TooManyRequests => new ZiurApiException(
            "La API de Ziur está recibiendo demasiadas solicitudes. Espera un momento e intenta de nuevo.",
            estado,
            esReintentable: true),
        >= HttpStatusCode.InternalServerError => new ZiurApiException(
            "La API de Ziur presentó un error interno. Intenta de nuevo en unos minutos.",
            estado,
            esReintentable: true),
        _ => new ZiurApiException(
            $"La API de Ziur respondió con un estado inesperado ({(int)estado}).",
            estado)
    };
}
