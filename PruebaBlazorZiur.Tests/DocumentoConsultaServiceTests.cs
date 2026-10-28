using PruebaBlazorZiur.Modelos;
using PruebaBlazorZiur.Servicios;

namespace PruebaBlazorZiur.Tests;

public sealed class DocumentoConsultaServiceTests
{
    private readonly DocumentoConsultaService _servicio = new();

    private static readonly Documento[] Documentos =
    [
        new() { Codigo = 29, Descripcion = "Ajuste al Inventario", VActiva = false },
        new() { Codigo = 51, Descripcion = "Avance Produccion", VActiva = true },
        new() { Codigo = 17, Descripcion = "Balance Inicial", VActiva = false },
        new() { Codigo = 28, Descripcion = "Cancelación Cuentas de Impuestos", VActiva = true },
        new() { Codigo = 2, Descripcion = "Factura de Venta", VActiva = false },
        new() { Codigo = 44, Descripcion = "Factura de Compra Equivalente", VActiva = false }
    ];

    [Fact]
    public void Consultar_SinBusquedaNiOrden_ConservaElOrdenDeLaApi()
    {
        var resultado = _servicio.Consultar(Documentos, Parametros());

        Assert.Equal(Documentos.Length, resultado.Total);
        Assert.Equal(29, resultado.Documentos[0].Codigo);
        Assert.Equal(1, resultado.PaginaActual);
        Assert.Equal(1, resultado.TotalPaginas);
        Assert.Equal(2, resultado.Activos);
        Assert.Equal(4, resultado.Inactivos);
    }

    [Theory]
    [InlineData("cancelacion")]
    [InlineData("CANCELACIÓN")]
    [InlineData("  cancel  ")]
    public void Consultar_Busqueda_IgnoraMayusculasYTildes(string busqueda)
    {
        var resultado = _servicio.Consultar(Documentos, Parametros(busqueda: busqueda));

        var documento = Assert.Single(resultado.Documentos);
        Assert.Equal(28, documento.Codigo);
        Assert.Equal("Cancelación Cuentas de Impuestos", documento.Descripcion);
    }

    [Fact]
    public void Consultar_BusquedaPorCodigo_EncuentraElDocumento()
    {
        var resultado = _servicio.Consultar(Documentos, Parametros(busqueda: "44"));

        var documento = Assert.Single(resultado.Documentos);
        Assert.Equal("Factura de Compra Equivalente", documento.Descripcion);
    }

    [Fact]
    public void Consultar_BusquedaSinResultados_DevuelveColeccionVacia()
    {
        var resultado = _servicio.Consultar(Documentos, Parametros(busqueda: "no-existe"));

        Assert.Empty(resultado.Documentos);
        Assert.Equal(0, resultado.Total);
        Assert.Equal(1, resultado.TotalPaginas);
        Assert.Equal(0, resultado.Activos);
        Assert.Equal(0, resultado.Inactivos);
    }

    [Fact]
    public void Consultar_OrdenDescripcionAscendente_UsaCulturaEspanola()
    {
        var resultado = _servicio.Consultar(
            Documentos,
            Parametros(columna: ColumnaDocumento.Descripcion, ascendente: true, tamanoPagina: 10));

        Assert.Equal("Ajuste al Inventario", resultado.Documentos[0].Descripcion);
        Assert.Equal("Factura de Compra Equivalente", resultado.Documentos[^2].Descripcion);
    }

    [Fact]
    public void Consultar_OrdenDescripcionDescendente_InvierteElResultado()
    {
        var resultado = _servicio.Consultar(
            Documentos,
            Parametros(columna: ColumnaDocumento.Descripcion, ascendente: false, tamanoPagina: 10));

        Assert.Equal("Factura de Venta", resultado.Documentos[0].Descripcion);
    }

    [Fact]
    public void Consultar_OrdenCodigoAscendente_OrdenaPorCodigo()
    {
        var resultado = _servicio.Consultar(
            Documentos,
            Parametros(columna: ColumnaDocumento.Codigo, ascendente: true));

        Assert.Equal(new[] { 2, 17, 28, 29, 44, 51 }, resultado.Documentos.Select(d => d.Codigo));
    }

    [Fact]
    public void Consultar_OrdenEstadoDescendente_PriorizaActivos()
    {
        var resultado = _servicio.Consultar(
            Documentos,
            Parametros(columna: ColumnaDocumento.VActiva, ascendente: false));

        Assert.All(resultado.Documentos.Take(2), documento => Assert.True(documento.VActiva));
        Assert.Equal("Avance Produccion", resultado.Documentos[0].Descripcion);
    }

    [Fact]
    public void Consultar_SegundaPagina_DevuelveElBloqueCorrecto()
    {
        var resultado = _servicio.Consultar(
            Documentos,
            Parametros(pagina: 2, tamanoPagina: 4));

        Assert.Equal(2, resultado.TotalPaginas);
        Assert.Equal(2, resultado.PaginaActual);
        Assert.Equal(new[] { 2, 44 }, resultado.Documentos.Select(d => d.Codigo));
    }

    [Fact]
    public void Consultar_PaginaFueraDeRango_SeAjustaALaUltimaPagina()
    {
        var resultado = _servicio.Consultar(
            Documentos,
            Parametros(pagina: 99, tamanoPagina: 4));

        Assert.Equal(2, resultado.PaginaActual);
        Assert.Equal(2, resultado.Documentos.Count);
    }

    [Fact]
    public void Consultar_TamanoPaginaInvalido_UsaDiezPorDefecto()
    {
        var resultado = _servicio.Consultar(Documentos, Parametros(tamanoPagina: 0));

        Assert.Equal(Documentos.Length, resultado.Documentos.Count);
        Assert.Equal(1, resultado.PaginaActual);
        Assert.Equal(1, resultado.TotalPaginas);
    }

    private static ParametrosConsultaDocumentos Parametros(
        string? busqueda = null,
        ColumnaDocumento? columna = null,
        bool ascendente = true,
        int pagina = 1,
        int tamanoPagina = 10) =>
        new(busqueda, columna, ascendente, pagina, tamanoPagina);
}
