using System.Globalization;
using System.Text;
using PruebaBlazorZiur.Modelos;

namespace PruebaBlazorZiur.Servicios;

public sealed record ParametrosConsultaDocumentos(
    string? Busqueda,
    ColumnaDocumento? Columna,
    bool Ascendente,
    int Pagina,
    int TamanoPagina);

public sealed record ResultadoConsultaDocumentos(
    IReadOnlyList<Documento> Documentos,
    int Total,
    int PaginaActual,
    int TotalPaginas,
    int Activos,
    int Inactivos);

/// <summary>
/// Búsqueda, ordenamiento y paginación en memoria. Se mantiene separada de los
/// componentes para poder probarla sin levantar la interfaz.
/// </summary>
public sealed class DocumentoConsultaService
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    private static readonly StringComparer ComparadorTexto =
        StringComparer.Create(Cultura, ignoreCase: true);

    private const int TamanoPaginaPredeterminado = 10;

    public ResultadoConsultaDocumentos Consultar(
        IEnumerable<Documento> documentos,
        ParametrosConsultaDocumentos parametros)
    {
        ArgumentNullException.ThrowIfNull(documentos);
        ArgumentNullException.ThrowIfNull(parametros);

        var busqueda = Normalizar(parametros.Busqueda);

        var filtrados = (string.IsNullOrEmpty(busqueda)
                ? documentos
                : documentos.Where(documento =>
                    Normalizar(documento.Codigo.ToString(CultureInfo.InvariantCulture)).Contains(busqueda) ||
                    Normalizar(documento.Descripcion).Contains(busqueda)))
            .ToList();

        var ordenados = AplicarOrden(filtrados, parametros.Columna, parametros.Ascendente).ToList();

        var tamanoPagina = parametros.TamanoPagina > 0
            ? parametros.TamanoPagina
            : TamanoPaginaPredeterminado;

        var total = ordenados.Count;
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        var paginaActual = Math.Clamp(parametros.Pagina, 1, totalPaginas);

        var pagina = ordenados
            .Skip((paginaActual - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return new ResultadoConsultaDocumentos(
            pagina,
            total,
            paginaActual,
            totalPaginas,
            filtrados.Count(documento => documento.VActiva),
            filtrados.Count(documento => !documento.VActiva));
    }

    private static IEnumerable<Documento> AplicarOrden(
        IEnumerable<Documento> documentos,
        ColumnaDocumento? columna,
        bool ascendente) => columna switch
        {
            ColumnaDocumento.Codigo => ascendente
                ? documentos.OrderBy(documento => documento.Codigo)
                : documentos.OrderByDescending(documento => documento.Codigo),

            ColumnaDocumento.Descripcion => ascendente
                ? documentos.OrderBy(documento => documento.Descripcion, ComparadorTexto)
                : documentos.OrderByDescending(documento => documento.Descripcion, ComparadorTexto),

            ColumnaDocumento.VActiva => ascendente
                ? documentos
                    .OrderBy(documento => documento.VActiva)
                    .ThenBy(documento => documento.Descripcion, ComparadorTexto)
                : documentos
                    .OrderByDescending(documento => documento.VActiva)
                    .ThenBy(documento => documento.Descripcion, ComparadorTexto),

            _ => documentos
        };

    /// <summary>
    /// Búsqueda insensible a mayúsculas y a tildes: "cancelacion" encuentra
    /// "Cancelación", lo que mejora la experiencia en español.
    /// </summary>
    private static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                resultado.Append(char.ToLowerInvariant(caracter));
            }
        }

        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }
}
