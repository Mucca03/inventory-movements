using PruebaBlazorZiur.Modelos;

namespace PruebaBlazorZiur.Servicios;

/// <summary>
/// Acceso a los documentos de inventario publicados por la API de Ziur.
/// </summary>
public interface IDocumentoService
{
    Task<IReadOnlyList<Documento>> ObtenerDocumentosAsync(
        CancellationToken cancellationToken = default);
}
