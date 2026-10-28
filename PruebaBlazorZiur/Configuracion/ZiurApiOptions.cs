using System.ComponentModel.DataAnnotations;

namespace PruebaBlazorZiur.Configuracion;

/// <summary>
/// Configuración de la integración con la API REST de Ziur.
/// Se enlaza desde la sección "ZiurApi" de la configuración de la aplicación.
/// </summary>
public sealed class ZiurApiOptions
{
    public const string SectionName = "ZiurApi";

    [Required(ErrorMessage = "La URL base de la API de Ziur es obligatoria.")]
    [Url(ErrorMessage = "La URL base de la API de Ziur no es válida.")]
    public string BaseUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "El endpoint de documentos es obligatorio.")]
    public string DocumentosEndpoint { get; set; } = "DocumentosFillsCombos";

    /// <summary>
    /// Token Bearer suministrado por Ziur. Debe configurarse por user-secrets
    /// o variables de entorno; nunca debe versionarse en el repositorio.
    /// </summary>
    public string BearerToken { get; set; } = string.Empty;

    [Range(5, 120, ErrorMessage = "El tiempo de espera debe estar entre 5 y 120 segundos.")]
    public int TimeoutSegundos { get; set; } = 30;
}
