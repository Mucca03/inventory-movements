namespace PruebaBlazorZiur.Modelos;

/// <summary>
/// Documento (movimiento de inventario) devuelto por el endpoint
/// DocumentosFillsCombos de la API de Ziur.
/// </summary>
public sealed record Documento
{
    public int Codigo { get; init; }

    public string Descripcion { get; init; } = string.Empty;

    public bool VActiva { get; init; }

    public string EstadoTexto => VActiva ? "Activa" : "Inactiva";
}
