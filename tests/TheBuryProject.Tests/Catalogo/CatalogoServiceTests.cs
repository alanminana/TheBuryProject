using Moq;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;
using Xunit;
using Microsoft.Extensions.Logging;

namespace TheBuryProject.Tests.Catalogo
{
    /// <summary>
    /// Tests unitarios para CatalogoService
    /// Cubre: resolver precio por catálogo, simulación de cambios, validaciones
    /// </summary>
    public class CatalogoServiceTests
    {
        private readonly Mock<ICatalogLookupService> _catalogLookupMock;
        private readonly Mock<IProductoService> _productoServiceMock;
        private readonly Mock<IPrecioService> _precioServiceMock;
        private readonly Mock<ILogger<CatalogoService>> _loggerMock;
        private readonly CatalogoService _sut;

        public CatalogoServiceTests()
        {
            _catalogLookupMock = new Mock<ICatalogLookupService>();
            _productoServiceMock = new Mock<IProductoService>();
            _precioServiceMock = new Mock<IPrecioService>();
            _loggerMock = new Mock<ILogger<CatalogoService>>();

            _sut = new CatalogoService(
                _catalogLookupMock.Object,
                _productoServiceMock.Object,
                _precioServiceMock.Object,
                _loggerMock.Object
            );
        }

        #region ObtenerCatalogoAsync Tests

        [Fact]
        public async Task ObtenerCatalogoAsync_SinFiltros_RetornaTodosLosProductos()
        {
            // Arrange
            var categorias = new List<Categoria>
            {
                new() { Id = 1, Nombre = "Electrónica" },
                new() { Id = 2, Nombre = "Hogar" }
            };
            var marcas = new List<Marca>
            {
                new() { Id = 1, Nombre = "Samsung" },
                new() { Id = 2, Nombre = "LG" }
            };
            var listaPredeterminada = new ListaPrecio { Id = 1, Nombre = "Contado" };
            var productos = new List<Producto>
            {
                new()
                {
                    Id = 1,
                    Codigo = "PROD001",
                    Nombre = "Televisor",
                    PrecioCompra = 1000,
                    PrecioVenta = 1500,
                    StockActual = 10,
                    StockMinimo = 5,
                    Activo = true,
                    Categoria = categorias[0],
                    CategoriaId = 1
                }
            };

            _catalogLookupMock
                .Setup(x => x.GetCategoriasYMarcasAsync())
                .ReturnsAsync((categorias.AsEnumerable(), marcas.AsEnumerable()));

            _precioServiceMock
                .Setup(x => x.GetAllListasAsync(true))
                .ReturnsAsync(new List<ListaPrecio> { listaPredeterminada });

            _precioServiceMock
                .Setup(x => x.GetListaPredeterminadaAsync())
                .ReturnsAsync(listaPredeterminada);

            _productoServiceMock
                .Setup(x => x.SearchAsync(null, null, null, false, false, null, "asc"))
                .ReturnsAsync(productos);

            _precioServiceMock
                .Setup(x => x.GetPrecioVigenteAsync(1, 1, It.IsAny<DateTime?>()))
                .ReturnsAsync((ProductoPrecioLista?)null);

            var filtros = new FiltrosCatalogo();

            // Act
            var resultado = await _sut.ObtenerCatalogoAsync(filtros);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(1, resultado.TotalResultados);
            Assert.Single(resultado.Filas);
            Assert.Equal(2, resultado.TotalCategorias);
            Assert.Equal(2, resultado.TotalMarcas);
            Assert.Equal("Contado", resultado.ListaPrecioNombre);
        }

        [Fact]
        public async Task ObtenerCatalogoAsync_ConListaPrecio_UsaListaEspecificada()
        {
            // Arrange
            var categorias = Enumerable.Empty<Categoria>();
            var marcas = Enumerable.Empty<Marca>();
            var listaContado = new ListaPrecio { Id = 1, Nombre = "Contado" };
            var listaTarjeta = new ListaPrecio { Id = 2, Nombre = "Tarjeta" };

            var productos = new List<Producto>
            {
                new()
                {
                    Id = 1,
                    Codigo = "PROD001",
                    Nombre = "Producto",
                    PrecioCompra = 100,
                    PrecioVenta = 150,
                    StockActual = 10,
                    Activo = true
                }
            };

            var precioLista = new ProductoPrecioLista
            {
                ProductoId = 1,
                ListaId = 2,
                Precio = 170 // Precio tarjeta
            };

            _catalogLookupMock
                .Setup(x => x.GetCategoriasYMarcasAsync())
                .ReturnsAsync((categorias, marcas));

            _precioServiceMock
                .Setup(x => x.GetAllListasAsync(true))
                .ReturnsAsync(new List<ListaPrecio> { listaContado, listaTarjeta });

            _precioServiceMock
                .Setup(x => x.GetListaPredeterminadaAsync())
                .ReturnsAsync(listaContado);

            _productoServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), false, false, It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(productos);

            _precioServiceMock
                .Setup(x => x.GetPrecioVigenteAsync(1, 2, It.IsAny<DateTime?>()))
                .ReturnsAsync(precioLista);

            var filtros = new FiltrosCatalogo { ListaPrecioId = 2 };

            // Act
            var resultado = await _sut.ObtenerCatalogoAsync(filtros);

            // Assert
            Assert.Equal(2, resultado.ListaPrecioId);
            Assert.Equal("Tarjeta", resultado.ListaPrecioNombre);
            var fila = resultado.Filas.First();
            Assert.Equal(170, fila.PrecioActual); // Debe usar precio de lista tarjeta
            Assert.True(fila.TienePrecioLista);
        }

        [Fact]
        public async Task ObtenerCatalogoAsync_ProductoSinPrecioLista_UsaPrecioBase()
        {
            // Arrange
            var categorias = Enumerable.Empty<Categoria>();
            var marcas = Enumerable.Empty<Marca>();
            var listaPredeterminada = new ListaPrecio { Id = 1, Nombre = "Contado" };

            var productos = new List<Producto>
            {
                new()
                {
                    Id = 1,
                    Codigo = "PROD001",
                    Nombre = "Producto Sin Precio Lista",
                    PrecioCompra = 100,
                    PrecioVenta = 150, // Precio base
                    StockActual = 10,
                    Activo = true
                }
            };

            _catalogLookupMock
                .Setup(x => x.GetCategoriasYMarcasAsync())
                .ReturnsAsync((categorias, marcas));

            _precioServiceMock
                .Setup(x => x.GetAllListasAsync(true))
                .ReturnsAsync(new List<ListaPrecio> { listaPredeterminada });

            _precioServiceMock
                .Setup(x => x.GetListaPredeterminadaAsync())
                .ReturnsAsync(listaPredeterminada);

            _productoServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), false, false, It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(productos);

            _precioServiceMock
                .Setup(x => x.GetPrecioVigenteAsync(1, 1, It.IsAny<DateTime?>()))
                .ReturnsAsync((ProductoPrecioLista?)null); // Sin precio lista

            var filtros = new FiltrosCatalogo();

            // Act
            var resultado = await _sut.ObtenerCatalogoAsync(filtros);

            // Assert
            var fila = resultado.Filas.First();
            Assert.Equal(150, fila.PrecioActual); // Debe usar PrecioVenta del producto
            Assert.False(fila.TienePrecioLista);
            Assert.Equal(150, fila.PrecioBase);
        }

        #endregion

        #region ObtenerFilaAsync Tests

        [Fact]
        public async Task ObtenerFilaAsync_ProductoExiste_RetornaFila()
        {
            // Arrange
            var listaPredeterminada = new ListaPrecio { Id = 1, Nombre = "Contado" };
            var producto = new Producto
            {
                Id = 1,
                Codigo = "PROD001",
                Nombre = "Televisor",
                PrecioCompra = 1000,
                PrecioVenta = 1500,
                StockActual = 10,
                StockMinimo = 5,
                Activo = true
            };

            _productoServiceMock
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(producto);

            _precioServiceMock
                .Setup(x => x.GetListaPredeterminadaAsync())
                .ReturnsAsync(listaPredeterminada);

            _precioServiceMock
                .Setup(x => x.GetPrecioVigenteAsync(1, 1, It.IsAny<DateTime?>()))
                .ReturnsAsync((ProductoPrecioLista?)null);

            // Act
            var fila = await _sut.ObtenerFilaAsync(1);

            // Assert
            Assert.NotNull(fila);
            Assert.Equal(1, fila.ProductoId);
            Assert.Equal("PROD001", fila.Codigo);
            Assert.Equal("Televisor", fila.Nombre);
            Assert.Equal(1500, fila.PrecioActual);
        }

        [Fact]
        public async Task ObtenerFilaAsync_ProductoNoExiste_RetornaNull()
        {
            // Arrange
            _productoServiceMock
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((Producto?)null);

            // Act
            var fila = await _sut.ObtenerFilaAsync(999);

            // Assert
            Assert.Null(fila);
        }

        [Fact]
        public async Task ObtenerFilaAsync_ConListaEspecifica_UsaEsaLista()
        {
            // Arrange
            var producto = new Producto
            {
                Id = 1,
                Codigo = "PROD001",
                Nombre = "Producto",
                PrecioCompra = 100,
                PrecioVenta = 150,
                StockActual = 10,
                Activo = true
            };

            var precioLista = new ProductoPrecioLista
            {
                ProductoId = 1,
                ListaId = 2,
                Precio = 180
            };

            _productoServiceMock
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(producto);

            _precioServiceMock
                .Setup(x => x.GetPrecioVigenteAsync(1, 2, It.IsAny<DateTime?>()))
                .ReturnsAsync(precioLista);

            // Act
            var fila = await _sut.ObtenerFilaAsync(1, listaPrecioId: 2);

            // Assert
            Assert.NotNull(fila);
            Assert.Equal(180, fila.PrecioActual);
            Assert.True(fila.TienePrecioLista);
        }

        #endregion

        #region Cálculo de Margen Tests

        [Theory]
        [InlineData(100, 150, 50)] // 50% margen
        [InlineData(100, 120, 20)] // 20% margen
        [InlineData(100, 100, 0)]  // 0% margen
        [InlineData(0, 150, 0)]    // Costo 0 -> margen 0
        public async Task ObtenerFilaAsync_CalculaMargenCorrectamente(
            decimal costo, decimal precioVenta, decimal margenEsperado)
        {
            // Arrange
            var producto = new Producto
            {
                Id = 1,
                Codigo = "P1",
                Nombre = "Producto",
                PrecioCompra = costo,
                PrecioVenta = precioVenta,
                StockActual = 10,
                Activo = true
            };

            _productoServiceMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(producto);
            _precioServiceMock.Setup(x => x.GetListaPredeterminadaAsync()).ReturnsAsync((ListaPrecio?)null);

            // Act
            var fila = await _sut.ObtenerFilaAsync(1);

            // Assert
            Assert.NotNull(fila);
            Assert.Equal(margenEsperado, fila.MargenPorcentaje);
        }

        #endregion

        #region Estado Stock Tests

        [Theory]
        [InlineData(0, 5, "Sin Stock")]
        [InlineData(3, 5, "Stock Bajo")]
        [InlineData(5, 5, "Stock Bajo")] // Exactamente en el mínimo
        [InlineData(10, 5, "Normal")]
        public async Task ObtenerFilaAsync_DeterminaEstadoStockCorrectamente(
            decimal stockActual, decimal stockMinimo, string estadoEsperado)
        {
            // Arrange
            var producto = new Producto
            {
                Id = 1,
                Codigo = "P1",
                Nombre = "Producto",
                PrecioCompra = 100,
                PrecioVenta = 150,
                StockActual = stockActual,
                StockMinimo = stockMinimo,
                Activo = true
            };

            _productoServiceMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(producto);
            _precioServiceMock.Setup(x => x.GetListaPredeterminadaAsync()).ReturnsAsync((ListaPrecio?)null);

            // Act
            var fila = await _sut.ObtenerFilaAsync(1);

            // Assert
            Assert.NotNull(fila);
            Assert.Equal(estadoEsperado, fila.EstadoStock);
        }

        #endregion

        #region SimularCambioPreciosAsync Tests

        [Fact]
        public async Task SimularCambioPreciosAsync_ConPorcentaje_SimulaCorrectamente()
        {
            // Arrange
            var batch = new PriceChangeBatch
            {
                Id = 1,
                Nombre = "Aumento 10%",
                TipoCambio = TipoCambio.PorcentajeSobrePrecioActual,
                TipoAplicacion = TipoAplicacion.Aumento,
                ValorCambio = 10,
                Estado = EstadoBatch.Simulado,
                RowVersion = new byte[] { 1, 2, 3, 4 }
            };

            var items = new List<PriceChangeItem>
            {
                new()
                {
                    Id = 1,
                    ProductoId = 1,
                    ListaId = 1,
                    PrecioAnterior = 100,
                    PrecioNuevo = 110,
                    Producto = new Producto { Id = 1, Codigo = "P1", Nombre = "Producto 1" },
                    Lista = new ListaPrecio { Id = 1, Nombre = "Contado" }
                },
                new()
                {
                    Id = 2,
                    ProductoId = 2,
                    ListaId = 1,
                    PrecioAnterior = 200,
                    PrecioNuevo = 220,
                    Producto = new Producto { Id = 2, Codigo = "P2", Nombre = "Producto 2" },
                    Lista = new ListaPrecio { Id = 1, Nombre = "Contado" }
                }
            };

            _precioServiceMock
                .Setup(x => x.SimularCambioMasivoAsync(
                    It.IsAny<string>(),
                    It.IsAny<TipoCambio>(),
                    It.IsAny<TipoAplicacion>(),
                    It.IsAny<decimal>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<List<int>?>(),
                    It.IsAny<List<int>?>(),
                    It.IsAny<List<int>?>()))
                .ReturnsAsync(batch);

            _precioServiceMock
                .Setup(x => x.GetItemsSimulacionAsync(1, 0, 500))
                .ReturnsAsync(items);

            _precioServiceMock
                .Setup(x => x.RequiereAutorizacionAsync(1))
                .ReturnsAsync(false);

            var solicitud = new SolicitudSimulacionPrecios
            {
                Nombre = "Aumento 10%",
                TipoCambio = "porcentaje",
                Valor = 10
            };

            // Act
            var resultado = await _sut.SimularCambioPreciosAsync(solicitud);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(1, resultado.BatchId);
            Assert.Equal("Aumento 10%", resultado.Nombre);
            Assert.Equal(2, resultado.TotalProductos);
            Assert.Equal(2, resultado.ProductosConAumento);
            Assert.Equal(0, resultado.ProductosConDescuento);
            Assert.False(resultado.RequiereAutorizacion);
            Assert.NotEmpty(resultado.RowVersion);
        }

        [Fact]
        public async Task SimularCambioPreciosAsync_ConValorNegativo_CalculaDescuento()
        {
            // Arrange
            var batch = new PriceChangeBatch
            {
                Id = 1,
                Nombre = "Descuento 15%",
                Estado = EstadoBatch.Simulado,
                RowVersion = new byte[] { 1, 2, 3 }
            };

            var items = new List<PriceChangeItem>
            {
                new()
                {
                    ProductoId = 1,
                    ListaId = 1,
                    PrecioAnterior = 100,
                    PrecioNuevo = 85, // -15%
                    Producto = new Producto { Id = 1, Codigo = "P1", Nombre = "Producto 1" },
                    Lista = new ListaPrecio { Id = 1, Nombre = "Contado" }
                }
            };

            _precioServiceMock
                .Setup(x => x.SimularCambioMasivoAsync(
                    It.IsAny<string>(),
                    It.IsAny<TipoCambio>(),
                    TipoAplicacion.Disminucion, // Debe ser disminución
                    15, // Valor absoluto
                    It.IsAny<List<int>>(),
                    It.IsAny<List<int>?>(),
                    It.IsAny<List<int>?>(),
                    It.IsAny<List<int>?>()))
                .ReturnsAsync(batch);

            _precioServiceMock
                .Setup(x => x.GetItemsSimulacionAsync(1, 0, 500))
                .ReturnsAsync(items);

            _precioServiceMock
                .Setup(x => x.RequiereAutorizacionAsync(1))
                .ReturnsAsync(false);

            var solicitud = new SolicitudSimulacionPrecios
            {
                Nombre = "Descuento 15%",
                TipoCambio = "porcentaje",
                Valor = -15 // Valor negativo = descuento
            };

            // Act
            var resultado = await _sut.SimularCambioPreciosAsync(solicitud);

            // Assert
            Assert.Equal(1, resultado.ProductosConDescuento);
            Assert.Equal(0, resultado.ProductosConAumento);

            _precioServiceMock.Verify(x => x.SimularCambioMasivoAsync(
                It.IsAny<string>(),
                It.IsAny<TipoCambio>(),
                TipoAplicacion.Disminucion,
                15, // Valor absoluto
                It.IsAny<List<int>>(),
                It.IsAny<List<int>?>(),
                It.IsAny<List<int>?>(),
                It.IsAny<List<int>?>()), Times.Once);
        }

        [Theory]
        [InlineData("porcentaje", TipoCambio.PorcentajeSobrePrecioActual)]
        [InlineData("porcentajecosto", TipoCambio.PorcentajeSobreCosto)]
        [InlineData("absoluto", TipoCambio.ValorAbsoluto)]
        [InlineData("directo", TipoCambio.AsignacionDirecta)]
        [InlineData("desconocido", TipoCambio.PorcentajeSobrePrecioActual)] // Default
        public async Task SimularCambioPreciosAsync_ParseaTipoCambioCorrectamente(
            string tipoString, TipoCambio tipoEsperado)
        {
            // Arrange
            var batch = new PriceChangeBatch
            {
                Id = 1,
                Estado = EstadoBatch.Simulado,
                RowVersion = new byte[] { 1 }
            };

            _precioServiceMock
                .Setup(x => x.SimularCambioMasivoAsync(
                    It.IsAny<string>(),
                    tipoEsperado,
                    It.IsAny<TipoAplicacion>(),
                    It.IsAny<decimal>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<List<int>?>(),
                    It.IsAny<List<int>?>(),
                    It.IsAny<List<int>?>()))
                .ReturnsAsync(batch);

            _precioServiceMock
                .Setup(x => x.GetItemsSimulacionAsync(1, 0, 500))
                .ReturnsAsync(new List<PriceChangeItem>());

            _precioServiceMock
                .Setup(x => x.RequiereAutorizacionAsync(1))
                .ReturnsAsync(false);

            var solicitud = new SolicitudSimulacionPrecios
            {
                Nombre = "Test",
                TipoCambio = tipoString,
                Valor = 10
            };

            // Act
            await _sut.SimularCambioPreciosAsync(solicitud);

            // Assert
            _precioServiceMock.Verify(x => x.SimularCambioMasivoAsync(
                It.IsAny<string>(),
                tipoEsperado,
                It.IsAny<TipoAplicacion>(),
                It.IsAny<decimal>(),
                It.IsAny<List<int>>(),
                It.IsAny<List<int>?>(),
                It.IsAny<List<int>?>(),
                It.IsAny<List<int>?>()), Times.Once);
        }

        #endregion

        #region AplicarCambioPreciosAsync Tests

        [Fact]
        public async Task AplicarCambioPreciosAsync_BatchNoExiste_RetornaError()
        {
            // Arrange
            _precioServiceMock
                .Setup(x => x.GetSimulacionAsync(999))
                .ReturnsAsync((PriceChangeBatch?)null);

            var solicitud = new SolicitudAplicarPrecios
            {
                BatchId = 999,
                RowVersion = "AQIDBA=="
            };

            // Act
            var resultado = await _sut.AplicarCambioPreciosAsync(solicitud);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Contains("No se encontró", resultado.Mensaje);
        }

        [Fact]
        public async Task AplicarCambioPreciosAsync_RowVersionNoCoincide_RetornaError()
        {
            // Arrange
            var batch = new PriceChangeBatch
            {
                Id = 1,
                Estado = EstadoBatch.Simulado,
                RowVersion = new byte[] { 1, 2, 3, 4 } // Original
            };

            _precioServiceMock
                .Setup(x => x.GetSimulacionAsync(1))
                .ReturnsAsync(batch);

            var solicitud = new SolicitudAplicarPrecios
            {
                BatchId = 1,
                RowVersion = Convert.ToBase64String(new byte[] { 5, 6, 7, 8 }) // Diferente
            };

            // Act
            var resultado = await _sut.AplicarCambioPreciosAsync(solicitud);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Contains("modificados por otro usuario", resultado.Mensaje);
        }

        [Fact]
        public async Task AplicarCambioPreciosAsync_BatchSimulado_ApruebaYAplica()
        {
            // Arrange
            var rowVersion = new byte[] { 1, 2, 3, 4 };
            var batch = new PriceChangeBatch
            {
                Id = 1,
                Estado = EstadoBatch.Simulado,
                RowVersion = rowVersion,
                CantidadProductos = 5
            };

            var batchAprobado = new PriceChangeBatch
            {
                Id = 1,
                Estado = EstadoBatch.Aprobado,
                RowVersion = rowVersion
            };

            var batchAplicado = new PriceChangeBatch
            {
                Id = 1,
                Estado = EstadoBatch.Aplicado,
                CantidadProductos = 5,
                FechaAplicacion = DateTime.UtcNow
            };

            _precioServiceMock
                .Setup(x => x.GetSimulacionAsync(1))
                .ReturnsAsync(batch);

            _precioServiceMock
                .Setup(x => x.AprobarBatchAsync(1, It.IsAny<string>(), rowVersion, It.IsAny<string?>()))
                .ReturnsAsync(batchAprobado);

            _precioServiceMock
                .Setup(x => x.AplicarBatchAsync(1, It.IsAny<string>(), rowVersion, null))
                .ReturnsAsync(batchAplicado);

            var solicitud = new SolicitudAplicarPrecios
            {
                BatchId = 1,
                RowVersion = Convert.ToBase64String(rowVersion)
            };

            // Act
            var resultado = await _sut.AplicarCambioPreciosAsync(solicitud);

            // Assert
            Assert.True(resultado.Exitoso);
            Assert.Equal(5, resultado.ProductosActualizados);
            Assert.Equal(1, resultado.BatchId);

            // Verificar que se aprobó y luego se aplicó
            _precioServiceMock.Verify(x => x.AprobarBatchAsync(
                1, It.IsAny<string>(), rowVersion, It.IsAny<string?>()), Times.Once);
            _precioServiceMock.Verify(x => x.AplicarBatchAsync(
                1, It.IsAny<string>(), rowVersion, null), Times.Once);
        }

        [Fact]
        public async Task AplicarCambioPreciosAsync_ConFechaVigencia_PasaFechaAlServicio()
        {
            // Arrange
            var rowVersion = new byte[] { 1, 2, 3, 4 };
            var fechaVigencia = new DateTime(2025, 2, 1);
            var batch = new PriceChangeBatch
            {
                Id = 1,
                Estado = EstadoBatch.Simulado,
                RowVersion = rowVersion
            };

            var batchAprobado = new PriceChangeBatch
            {
                Id = 1,
                Estado = EstadoBatch.Aprobado,
                RowVersion = rowVersion
            };

            var batchAplicado = new PriceChangeBatch
            {
                Id = 1,
                Estado = EstadoBatch.Aplicado,
                CantidadProductos = 1,
                FechaAplicacion = DateTime.UtcNow
            };

            _precioServiceMock
                .Setup(x => x.GetSimulacionAsync(1))
                .ReturnsAsync(batch);

            _precioServiceMock
                .Setup(x => x.AprobarBatchAsync(1, It.IsAny<string>(), rowVersion, It.IsAny<string?>()))
                .ReturnsAsync(batchAprobado);

            _precioServiceMock
                .Setup(x => x.AplicarBatchAsync(1, It.IsAny<string>(), rowVersion, fechaVigencia))
                .ReturnsAsync(batchAplicado);

            var solicitud = new SolicitudAplicarPrecios
            {
                BatchId = 1,
                RowVersion = Convert.ToBase64String(rowVersion),
                FechaVigencia = fechaVigencia
            };

            // Act
            var resultado = await _sut.AplicarCambioPreciosAsync(solicitud);

            // Assert
            Assert.True(resultado.Exitoso);
            _precioServiceMock.Verify(x => x.AplicarBatchAsync(
                1, It.IsAny<string>(), rowVersion, fechaVigencia), Times.Once);
        }

        #endregion
    }
}

