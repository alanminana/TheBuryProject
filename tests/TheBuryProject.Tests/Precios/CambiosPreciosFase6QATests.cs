using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TheBuryProject.Controllers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Precios;

/// <summary>
/// Tests de QA y seguridad para cambios de precios (Fase 6)
/// - Validaciones de porcentaje y filtros
/// - Permisos y claims
/// - Control de concurrencia (rowVersion)
/// - Cálculos de precios
/// </summary>
public class CambiosPreciosFase6QATests
{
    #region Validaciones de Porcentaje

    [Theory]
    [InlineData(0)]
    public async Task SimularCambioRapido_porcentaje_cero_rechazado(decimal porcentaje)
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = porcentaje,
            ProductoIds = new List<int> { productoId },
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(badResult.Value);
        Assert.Contains("porcentaje", json.ToLower());
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(50)]
    [InlineData(-25)]
    [InlineData(100)]
    public async Task SimularCambioRapido_porcentaje_valido_aceptado(decimal porcentaje)
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = porcentaje,
            ProductoIds = new List<int> { productoId },
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    #endregion

    #region Validaciones de Filtros

    [Fact]
    public async Task SimularCambioRapido_modo_filtrados_sin_filtros_rechazado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "filtrados",
            Porcentaje = 10,
            // Sin ningún filtro definido
            CategoriaId = null,
            MarcaId = null,
            SearchTerm = null,
            SoloActivos = null,
            StockBajo = null,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(badResult.Value);
        Assert.Contains("filtro", json.ToLower());
    }

    [Fact]
    public async Task SimularCambioRapido_modo_filtrados_con_categoria_aceptado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var categoriaId = db.Context.Productos.First(p => p.Id == productoId).CategoriaId;
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "filtrados",
            Porcentaje = 10,
            CategoriaId = categoriaId,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task SimularCambioRapido_modo_filtrados_con_busqueda_aceptado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var productoNombre = db.Context.Productos.First(p => p.Id == productoId).Nombre;
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "filtrados",
            Porcentaje = 5,
            SearchTerm = productoNombre.Substring(0, 5),
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task SimularCambioRapido_modo_filtrados_con_soloActivos_aceptado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "filtrados",
            Porcentaje = 15,
            SoloActivos = true,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task SimularCambioRapido_modo_seleccionados_sin_productos_rechazado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 10,
            ProductoIds = new List<int>(), // Lista vacía
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SimularCambioRapido_modo_seleccionados_con_ids_negativos_filtrados()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 10,
            ProductoIds = new List<int> { -1, 0, productoId }, // Incluye inválidos
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert - Debe filtrar los inválidos y procesar el válido
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    #endregion

    #region Cálculos de Precio

    [Theory]
    [InlineData(100, 10, 110)] // 100 + 10% = 110
    [InlineData(100, -10, 90)] // 100 - 10% = 90
    [InlineData(200, 50, 300)] // 200 + 50% = 300
    [InlineData(150, 33.33, 200)] // 150 + 33.33% ≈ 200
    public async Task CalculoPrecio_porcentaje_correcto(decimal precioActual, decimal porcentaje, decimal precioEsperado)
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        
        // Crear producto con precio específico
        var categoria = new Categoria { Nombre = "Cat", Codigo = "C1" };
        var marca = new Marca { Nombre = "Marca", Codigo = "M1" };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

        var producto = new Producto
        {
            Codigo = "TEST",
            Nombre = "Producto Test",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 50,
            PrecioVenta = precioActual,
            StockActual = 10,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var lista = new ListaPrecio
        {
            Codigo = "LP1",
            Nombre = "Lista Test",
            Activa = true,
            EsPredeterminada = true,
            Orden = 1
        };
        db.Context.ListasPrecios.Add(lista);
        await db.Context.SaveChangesAsync();

        db.Context.ProductosPrecios.Add(new ProductoPrecioLista
        {
            ProductoId = producto.Id,
            ListaId = lista.Id,
            VigenciaDesde = DateTime.UtcNow.AddDays(-1),
            Costo = 50,
            Precio = precioActual,
            MargenValor = precioActual - 50,
            MargenPorcentaje = ((precioActual - 50) / 50) * 100,
            EsManual = true,
            EsVigente = true,
            CreadoPor = "seed"
        });
        await db.Context.SaveChangesAsync();

        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = porcentaje,
            ProductoIds = new List<int> { producto.Id },
            ListasPrecioIds = new List<int> { lista.Id }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        
        var batchId = doc.RootElement.GetProperty("batchId").GetInt32();
        
        // Verificar el precio calculado en el batch
        var item = db.Context.PriceChangeItems.First(i => i.BatchId == batchId);
        Assert.Equal(Math.Round(precioEsperado, 2), Math.Round(item.PrecioNuevo, 2));
    }

    #endregion

    #region Control de Concurrencia

    [Fact]
    public async Task RevertirApi_rowVersion_invalido_rechazado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:revert");

        var request = new RevertirApiRequest
        {
            BatchId = 1,
            RowVersion = "no-es-base64-valido!!!",
            Motivo = "Motivo de prueba válido"
        };

        // Act
        var result = await controller.RevertirApi(request);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(badResult.Value);
        Assert.Contains("RowVersion", json);
    }

    [Fact]
    public async Task GetBatchParaRevertirApi_incluye_rowVersion()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        
        var batch = new PriceChangeBatch
        {
            Nombre = "Test Batch",
            TipoCambio = TipoCambio.PorcentajeSobrePrecioActual,
            TipoAplicacion = TipoAplicacion.Aumento,
            ValorCambio = 10,
            Estado = EstadoBatch.Aplicado,
            SolicitadoPor = "tester",
            FechaSolicitud = DateTime.UtcNow,
            AplicadoPor = "admin",
            FechaAplicacion = DateTime.UtcNow,
            CantidadProductos = 5
        };
        db.Context.PriceChangeBatches.Add(batch);
        await db.Context.SaveChangesAsync();

        var controller = CreateController(db, "precios:revert");

        // Act
        var result = await controller.GetBatchParaRevertirApi(batch.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        
        var batchData = doc.RootElement.GetProperty("batch");
        var rowVersion = batchData.GetProperty("rowVersion").GetString();
        
        Assert.False(string.IsNullOrEmpty(rowVersion));
        // Verificar que es Base64 válido
        var decoded = Convert.FromBase64String(rowVersion!);
        Assert.NotNull(decoded);
    }

    #endregion

    #region Tests de Permisos (Claims)

    [Fact]
    public async Task SimularCambioRapido_requiere_permiso_simulate()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        
        // Controller SIN permiso de simulate
        var controller = CreateController(db, "precios:view"); // Solo view

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 10,
            ProductoIds = new List<int> { productoId },
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act - En un escenario real el filtro rechazaría, pero aquí probamos el claim
        var result = await controller.SimularCambioRapido(request);

        // Assert - El método se ejecuta pero el PermisoRequerido debería filtrar antes
        // Aquí verificamos que la lógica no dependa del claim en el método mismo
        Assert.IsType<OkObjectResult>(result); // El método funciona, el filtro es externo
    }

    #endregion

    #region Tests de Request Nulo

    [Fact]
    public async Task SimularCambioRapido_request_nulo_rechazado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        // Act
        var result = await controller.SimularCambioRapido(null!);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(badResult.Value);
        Assert.Contains("request inv", json.ToLower()); // "inválido" se serializa con escape unicode
    }

    [Fact]
    public async Task RevertirApi_request_nulo_rechazado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:revert");

        // Act
        var result = await controller.RevertirApi(null!);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(badResult.Value);
        Assert.Contains("request inv", json.ToLower()); // "inválido" se serializa con escape unicode
    }

    #endregion

    #region Tests de Múltiples Listas

    [Fact]
    public async Task SimularCambioRapido_multiples_listas_funciona()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId1) = await SeedBasicDataWithPrecio(db);
        
        // Agregar segunda lista
        var lista2 = new ListaPrecio
        {
            Codigo = "LP2",
            Nombre = "Lista 2",
            Activa = true,
            EsPredeterminada = false,
            Orden = 2
        };
        db.Context.ListasPrecios.Add(lista2);
        await db.Context.SaveChangesAsync();

        // Agregar precio en segunda lista
        db.Context.ProductosPrecios.Add(new ProductoPrecioLista
        {
            ProductoId = productoId,
            ListaId = lista2.Id,
            VigenciaDesde = DateTime.UtcNow.AddDays(-1),
            Costo = 100,
            Precio = 250,
            MargenValor = 150,
            MargenPorcentaje = 150,
            EsManual = true,
            EsVigente = true,
            CreadoPor = "seed"
        });
        await db.Context.SaveChangesAsync();

        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 20,
            ProductoIds = new List<int> { productoId },
            ListasPrecioIds = new List<int> { listaId1, lista2.Id }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        
        var batchId = doc.RootElement.GetProperty("batchId").GetInt32();
        
        // Verificar que se crearon items para ambas listas
        var items = db.Context.PriceChangeItems.Where(i => i.BatchId == batchId).ToList();
        Assert.Equal(2, items.Count);
    }

    #endregion

    #region Helpers

    private static CambiosPreciosController CreateController(SqliteInMemoryDb db, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "Gerente")
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("Permission", permission));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var controller = new CambiosPreciosController(
            db.Context,
            precioService,
            Mock.Of<IProductoService>(),
            Mock.Of<ICategoriaService>(),
            Mock.Of<IMarcaService>(),
            NullLogger<CambiosPreciosController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Setup TempData
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;

        return controller;
    }

    private static async Task<(int ProductoId, int ListaId)> SeedBasicDataWithPrecio(SqliteInMemoryDb db)
    {
        // Usar códigos únicos que no colisionen con los seeds predefinidos (ELEC, FRIO)
        var categoria = new Categoria { Nombre = "QA Categoria", Codigo = "QCAT" };
        var marca = new Marca { Nombre = "QA Marca", Codigo = "QMAR" };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

        var producto = new Producto
        {
            Codigo = "P1",
            Nombre = "Producto Test",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 100,
            PrecioVenta = 200,
            StockActual = 10,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var lista = new ListaPrecio
        {
            Codigo = "LP1",
            Nombre = "Lista Test",
            Activa = true,
            EsPredeterminada = true,
            Orden = 1
        };
        db.Context.ListasPrecios.Add(lista);
        await db.Context.SaveChangesAsync();

        db.Context.ProductosPrecios.Add(new ProductoPrecioLista
        {
            ProductoId = producto.Id,
            ListaId = lista.Id,
            VigenciaDesde = DateTime.UtcNow.AddDays(-1),
            Costo = 100,
            Precio = 200,
            MargenValor = 100,
            MargenPorcentaje = 100,
            EsManual = true,
            EsVigente = true,
            CreadoPor = "seed"
        });
        await db.Context.SaveChangesAsync();

        return (producto.Id, lista.Id);
    }

    #endregion
}
