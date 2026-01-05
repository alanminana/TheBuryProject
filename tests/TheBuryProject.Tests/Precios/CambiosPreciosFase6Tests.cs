using System.Security.Claims;
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
/// Tests para la funcionalidad de Cambios de Precios - FASE 6
/// Verifica: SimularDesdeCatalogo, validaciones, permisos y manejo de errores
/// </summary>
public class CambiosPreciosFase6Tests
{
    #region SimularDesdeCatalogo - Validaciones

    [Fact]
    public async Task SimularDesdeCatalogo_con_productos_validos_crea_batch()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = productoId.ToString(),
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int> { listaId },
            Nota = "Test desde catálogo"
        };

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Preview", redirectResult.ActionName);
        Assert.NotNull(redirectResult.RouteValues);
        Assert.True(redirectResult.RouteValues.ContainsKey("id"));
    }

    [Fact]
    public async Task SimularDesdeCatalogo_con_multiples_productos_crea_batch()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (producto1Id, listaId) = await SeedBasicDataWithPrecio(db);
        var producto2Id = await SeedAdditionalProducto(db, listaId);
        var controller = CreateController(db, "precios:simulate");

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = $"{producto1Id},{producto2Id}",
            TipoCambio = "Porcentual",
            ValorInput = 15,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Preview", redirectResult.ActionName);
    }

    [Fact]
    public async Task SimularDesdeCatalogo_sin_productos_redirige_a_catalogo_con_error()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = "", // Sin productos
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int> { 1 }
        };

        // Forzar que ModelState sea inválido
        controller.ModelState.AddModelError("ProductoIdsText", "Requerido");

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Catalogo", redirectResult.ControllerName);
    }

    [Fact]
    public async Task SimularDesdeCatalogo_sin_listas_redirige_a_catalogo_con_error()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, _) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = productoId.ToString(),
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int>() // Sin listas
        };

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Catalogo", redirectResult.ControllerName);
        Assert.Contains("lista", controller.TempData["Error"]?.ToString()?.ToLower() ?? "");
    }

    [Fact]
    public async Task SimularDesdeCatalogo_con_ids_invalidos_redirige_con_error()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = "abc,xyz", // IDs inválidos
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Catalogo", redirectResult.ControllerName);
    }

    [Fact]
    public async Task SimularDesdeCatalogo_con_tipo_fijo_usa_valor_absoluto()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = productoId.ToString(),
            TipoCambio = "Fijo",
            ValorInput = 50,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Preview", redirectResult.ActionName);

        // Verificar el batch creado
        var batchId = (int)redirectResult.RouteValues!["id"]!;
        var batch = await db.Context.PriceChangeBatches.FindAsync(batchId);
        Assert.Equal(TipoCambio.ValorAbsoluto, batch!.TipoCambio);
    }

    [Fact]
    public async Task SimularDesdeCatalogo_con_valor_negativo_aplica_disminucion()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = productoId.ToString(),
            TipoCambio = "Porcentual",
            ValorInput = -10, // Negativo = disminución
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        var batchId = (int)redirectResult.RouteValues!["id"]!;
        var batch = await db.Context.PriceChangeBatches.FindAsync(batchId);
        Assert.Equal(TipoAplicacion.Disminucion, batch!.TipoAplicacion);
        Assert.Equal(10, batch.ValorCambio); // Valor absoluto
    }

    #endregion

    #region TempData y manejo de errores

    [Fact]
    public async Task SimularDesdeCatalogo_exitoso_establece_TempData_success()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = productoId.ToString(),
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        Assert.NotNull(controller.TempData["Success"]);
        Assert.Contains("exitosamente", controller.TempData["Success"]?.ToString()?.ToLower() ?? "");
    }

    [Fact]
    public async Task SimularDesdeCatalogo_exitoso_establece_OrigenCatalogo()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = productoId.ToString(),
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        Assert.True((bool?)controller.TempData["OrigenCatalogo"] ?? false);
    }

    #endregion

    #region ViewModel Validations

    [Fact]
    public void SimularDesdeCatalogoViewModel_ProductoIdsText_es_opcional_para_modo_filtrados()
    {
        // Arrange - Modo filtrados con JSON de filtros
        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = null,  // Sin IDs específicos
            Alcance = "filtrados",
            FiltrosJson = """{"categoriaId":1,"soloActivos":true}""",
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int> { 1 }
        };

        // Act
        var validationResults = ValidateModel(viewModel);

        // Assert - No debe haber error de ProductoIdsText
        Assert.DoesNotContain(validationResults, v => v.MemberNames.Contains("ProductoIdsText"));
        Assert.True(viewModel.TieneDatosParaProcesar);
    }

    [Fact]
    public void SimularDesdeCatalogoViewModel_TieneDatosParaProcesar_true_con_ids()
    {
        // Arrange
        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = "1,2,3",
            Alcance = "seleccionados",
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int> { 1 }
        };

        // Act & Assert
        Assert.True(viewModel.TieneDatosParaProcesar);
    }

    [Fact]
    public void SimularDesdeCatalogoViewModel_TieneDatosParaProcesar_false_sin_datos()
    {
        // Arrange
        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = null,
            Alcance = "seleccionados", // Modo seleccionados sin IDs
            FiltrosJson = null,
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int> { 1 }
        };

        // Act & Assert
        Assert.False(viewModel.TieneDatosParaProcesar);
    }

    [Fact]
    public void SimularDesdeCatalogoViewModel_requiere_ListasPrecioIds()
    {
        // Arrange
        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = "1,2,3",
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = null!
        };

        // Act
        var validationResults = ValidateModel(viewModel);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains("ListasPrecioIds"));
    }

    [Fact]
    public void SimularDesdeCatalogoViewModel_acepta_valores_validos()
    {
        // Arrange
        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = "1,2,3",
            TipoCambio = "Porcentual",
            ValorInput = 10,
            ListasPrecioIds = new List<int> { 1 },
            Nota = "Nota opcional"
        };

        // Act
        var validationResults = ValidateModel(viewModel);

        // Assert
        Assert.Empty(validationResults);
    }

    #endregion

    #region Permisos y Claims

    [Fact]
    public void Controller_requiere_permiso_simulate_para_SimularDesdeCatalogo()
    {
        // Arrange
        var method = typeof(CambiosPreciosController).GetMethod("SimularDesdeCatalogo");

        // Assert
        Assert.NotNull(method);
        var attributes = method.GetCustomAttributes(typeof(TheBuryProject.Filters.PermisoRequeridoAttribute), true);
        Assert.NotEmpty(attributes);

        var permisoAttr = (TheBuryProject.Filters.PermisoRequeridoAttribute)attributes[0];
        Assert.Equal("precios", permisoAttr.Modulo);
        Assert.Equal("simulate", permisoAttr.Accion);
    }

    [Fact]
    public void Controller_tiene_ValidateAntiForgeryToken_en_SimularDesdeCatalogo()
    {
        // Arrange
        var method = typeof(CambiosPreciosController).GetMethod("SimularDesdeCatalogo");

        // Assert
        Assert.NotNull(method);
        var attributes = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute), true);
        Assert.NotEmpty(attributes);
    }

    [Fact]
    public void Controller_requiere_HttpPost_en_SimularDesdeCatalogo()
    {
        // Arrange
        var method = typeof(CambiosPreciosController).GetMethod("SimularDesdeCatalogo");

        // Assert
        Assert.NotNull(method);
        var attributes = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), true);
        Assert.NotEmpty(attributes);
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

    private static async Task<(int productoId, int listaId)> SeedBasicDataWithPrecio(SqliteInMemoryDb db)
    {
        var categoria = new Categoria { Codigo = "CAT", Nombre = "Categoria", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
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

        // Crear precio vigente
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

    private static async Task<int> SeedAdditionalProducto(SqliteInMemoryDb db, int listaId)
    {
        var categoriaId = db.Context.Categorias.First().Id;
        var marcaId = db.Context.Marcas.First().Id;

        var producto = new Producto
        {
            Codigo = "P2",
            Nombre = "Producto 2",
            CategoriaId = categoriaId,
            MarcaId = marcaId,
            PrecioCompra = 150,
            PrecioVenta = 300,
            StockActual = 5,
            Activo = true
        };
        db.Context.Productos.Add(producto);
        await db.Context.SaveChangesAsync();

        db.Context.ProductosPrecios.Add(new ProductoPrecioLista
        {
            ProductoId = producto.Id,
            ListaId = listaId,
            VigenciaDesde = DateTime.UtcNow.AddDays(-1),
            Costo = 150,
            Precio = 300,
            MargenValor = 150,
            MargenPorcentaje = 100,
            EsManual = true,
            EsVigente = true,
            CreadoPor = "seed"
        });
        await db.Context.SaveChangesAsync();

        return producto.Id;
    }

    private static IList<System.ComponentModel.DataAnnotations.ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(model, null, null);
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    #endregion

    #region Modo Filtrados Tests

    [Fact]
    public async Task SimularDesdeCatalogo_modo_filtrados_con_categoria_funciona()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var categoriaId = db.Context.Productos.First(p => p.Id == productoId).CategoriaId;
        var controller = CreateController(db, "precios:simulate");

        var filtrosJson = System.Text.Json.JsonSerializer.Serialize(new { categoriaId = categoriaId, soloActivos = true });

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = null, // Sin IDs
            Alcance = "filtrados",
            FiltrosJson = filtrosJson,
            TipoCambio = "Porcentual",
            ValorInput = 15,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Preview", redirectResult.ActionName);
        Assert.True(redirectResult.RouteValues!.ContainsKey("id"));
    }

    [Fact]
    public async Task SimularDesdeCatalogo_modo_filtrados_sin_coincidencias_redirige_con_error()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        // Filtro con categoría inexistente
        var filtrosJson = System.Text.Json.JsonSerializer.Serialize(new { categoriaId = 9999, soloActivos = true });

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = null,
            Alcance = "filtrados",
            FiltrosJson = filtrosJson,
            TipoCambio = "Porcentual",
            ValorInput = 15,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Catalogo", redirectResult.ControllerName);
        Assert.Contains("productos", controller.TempData["Error"]?.ToString()?.ToLower() ?? "");
    }

    [Fact]
    public async Task SimularDesdeCatalogo_modo_filtrados_con_busqueda_texto()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var productoNombre = db.Context.Productos.First(p => p.Id == productoId).Nombre;
        var controller = CreateController(db, "precios:simulate");

        // Buscar por parte del nombre
        var filtrosJson = System.Text.Json.JsonSerializer.Serialize(new { busqueda = productoNombre.Substring(0, 3) });

        var viewModel = new SimularDesdeCatalogoViewModel
        {
            ProductoIdsText = null,
            Alcance = "filtrados",
            FiltrosJson = filtrosJson,
            TipoCambio = "Porcentual",
            ValorInput = 20,
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularDesdeCatalogo(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Preview", redirectResult.ActionName);
    }

    #endregion
    #region SimularCambioRapido (AJAX Endpoint) Tests

    [Fact]
    public async Task SimularCambioRapido_con_productos_validos_retorna_ok_json()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 10,
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
        Assert.True(doc.RootElement.GetProperty("batchId").GetInt32() > 0);
    }

    [Fact]
    public async Task SimularCambioRapido_sin_listas_usa_predeterminada()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        // Marcar lista como predeterminada
        var lista = db.Context.ListasPrecios.First();
        lista.EsPredeterminada = true;
        await db.Context.SaveChangesAsync();

        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 15,
            ProductoIds = new List<int> { productoId },
            ListasPrecioIds = null // Sin listas - debe usar la predeterminada
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
    public async Task SimularCambioRapido_porcentaje_cero_retorna_bad_request()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 0, // Inválido
            ProductoIds = new List<int> { productoId },
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(badResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task SimularCambioRapido_modo_filtrados_funciona()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var categoriaId = db.Context.Productos.First(p => p.Id == productoId).CategoriaId;
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "filtrados",
            Porcentaje = -5, // Descuento
            CategoriaId = categoriaId,
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
        Assert.True(doc.RootElement.GetProperty("cantidadProductos").GetInt32() > 0);
    }

    [Fact]
    public async Task SimularCambioRapido_sin_productos_retorna_bad_request()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate");

        var request = new SimularCambioRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 10,
            ProductoIds = new List<int>(), // Vacío
            ListasPrecioIds = new List<int> { listaId }
        };

        // Act
        var result = await controller.SimularCambioRapido(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region HistorialApi Tests

    [Fact]
    public async Task HistorialApi_sin_batches_devuelve_lista_vacia()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:view");

        // Act
        var result = await controller.HistorialApi();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(0, doc.RootElement.GetProperty("historial").GetArrayLength());
    }

    [Fact]
    public async Task HistorialApi_con_batches_devuelve_historial()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        
        // Crear un batch de simulación
        var batch = new TheBuryProject.Models.Entities.PriceChangeBatch
        {
            Nombre = "Test Batch",
            TipoCambio = TipoCambio.PorcentajeSobrePrecioActual,
            TipoAplicacion = TipoAplicion.Aumento,
            ValorCambio = 10,
            Estado = EstadoBatch.Simulado,
            SolicitadoPor = "tester",
            FechaSolicitud = DateTime.UtcNow,
            CantidadProductos = 1
        };
        db.Context.PriceChangeBatches.Add(batch);
        await db.Context.SaveChangesAsync();

        var controller = CreateController(db, "precios:view");

        // Act
        var result = await controller.HistorialApi(take: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(1, doc.RootElement.GetProperty("historial").GetArrayLength());
        
        var firstItem = doc.RootElement.GetProperty("historial")[0];
        Assert.Equal("Test Batch", firstItem.GetProperty("nombre").GetString());
        Assert.Equal("Simulado", firstItem.GetProperty("estado").GetString());
        Assert.Equal("bg-info", firstItem.GetProperty("estadoBadgeClass").GetString());
    }

    [Fact]
    public async Task HistorialApi_respeta_parametro_take()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        
        // Crear 5 batches
        for (int i = 1; i <= 5; i++)
        {
            db.Context.PriceChangeBatches.Add(new TheBuryProject.Models.Entities.PriceChangeBatch
            {
                Nombre = $"Batch {i}",
                TipoCambio = TipoCambio.PorcentajeSobrePrecioActual,
                TipoAplicacion = TipoAplicion.Aumento,
                ValorCambio = i * 5,
                Estado = EstadoBatch.Simulado,
                SolicitadoPor = "tester",
                FechaSolicitud = DateTime.UtcNow.AddMinutes(-i),
                CantidadProductos = i
            });
        }
        await db.Context.SaveChangesAsync();

        var controller = CreateController(db, "precios:view");

        // Act - Solicitar solo 3
        var result = await controller.HistorialApi(take: 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(3, doc.RootElement.GetProperty("historial").GetArrayLength());
    }

    #endregion

    #region GetBatchParaRevertirApi Tests

    [Fact]
    public async Task GetBatchParaRevertirApi_batch_aplicado_devuelve_datos()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        
        var batch = new TheBuryProject.Models.Entities.PriceChangeBatch
        {
            Nombre = "Batch para revertir",
            TipoCambio = TipoCambio.PorcentajeSobrePrecioActual,
            TipoAplicacion = TipoAplicion.Aumento,
            ValorCambio = 15,
            Estado = EstadoBatch.Aplicado,
            SolicitadoPor = "tester",
            FechaSolicitud = DateTime.UtcNow.AddHours(-1),
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
        
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        
        var batchData = doc.RootElement.GetProperty("batch");
        Assert.Equal(batch.Id, batchData.GetProperty("id").GetInt32());
        Assert.Equal("Batch para revertir", batchData.GetProperty("nombre").GetString());
        Assert.Equal(5, batchData.GetProperty("cantidadProductos").GetInt32());
        Assert.Equal("+15%", batchData.GetProperty("cambioDisplay").GetString());
        Assert.False(string.IsNullOrEmpty(batchData.GetProperty("rowVersion").GetString()));
    }

    [Fact]
    public async Task GetBatchParaRevertirApi_batch_no_aplicado_devuelve_error()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        
        var batch = new TheBuryProject.Models.Entities.PriceChangeBatch
        {
            Nombre = "Batch simulado",
            TipoCambio = TipoCambio.PorcentajeSobrePrecioActual,
            TipoAplicacion = TipoAplicion.Aumento,
            ValorCambio = 10,
            Estado = EstadoBatch.Simulado, // NO aplicado
            SolicitadoPor = "tester",
            FechaSolicitud = DateTime.UtcNow,
            CantidadProductos = 3
        };
        db.Context.PriceChangeBatches.Add(batch);
        await db.Context.SaveChangesAsync();

        var controller = CreateController(db, "precios:revert");

        // Act
        var result = await controller.GetBatchParaRevertirApi(batch.Id);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(badResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("Aplicado", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetBatchParaRevertirApi_batch_inexistente_devuelve_not_found()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:revert");

        // Act
        var result = await controller.GetBatchParaRevertirApi(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region RevertirApi Tests

    [Fact]
    public async Task RevertirApi_sin_motivo_devuelve_bad_request()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:revert");

        var request = new RevertirApiRequest
        {
            BatchId = 1,
            RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }),
            Motivo = "" // Sin motivo
        };

        // Act
        var result = await controller.RevertirApi(request);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(badResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("motivo", doc.RootElement.GetProperty("error").GetString()?.ToLower());
    }

    [Fact]
    public async Task RevertirApi_sin_rowversion_devuelve_bad_request()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:revert");

        var request = new RevertirApiRequest
        {
            BatchId = 1,
            RowVersion = "", // Sin RowVersion
            Motivo = "Motivo de prueba válido"
        };

        // Act
        var result = await controller.RevertirApi(request);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(badResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("RowVersion", doc.RootElement.GetProperty("error").GetString());
    }

    #endregion

    #region Revertir y Concurrencia

    [Fact]
    public async Task RevertirApi_restaura_precios_originales()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate", "precios:apply", "precios:revert");

        // Precio original
        var precioOriginal = db.Context.ProductosPrecios.First(p => p.ProductoId == productoId && p.ListaId == listaId && p.EsVigente).Precio;

        // Simular y aplicar un aumento del 20%
        var request = new AplicarRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 20,
            ProductoIds = new List<int> { productoId },
            ListasPrecioIds = new List<int> { listaId }
        };
        var aplicarResult = await controller.AplicarRapido(request);
        var okResult = Assert.IsType<OkObjectResult>(aplicarResult);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        var batchId = doc.RootElement.GetProperty("batchId").GetInt32();

        // Verificar precio actualizado
        var precioAumentado = db.Context.ProductosPrecios.First(p => p.ProductoId == productoId && p.ListaId == listaId && p.EsVigente).Precio;
        Assert.Equal(Math.Round(precioOriginal * 1.2m, 2), precioAumentado);

        // Obtener rowVersion
        var batch = db.Context.PriceChangeBatches.First(b => b.Id == batchId);
        var rowVersion = Convert.ToBase64String(batch.RowVersion);

        // Revertir
        var revertirRequest = new RevertirApiRequest
        {
            BatchId = batchId,
            RowVersion = rowVersion,
            Motivo = "Test de reversión"
        };
        var revertirResult = await controller.RevertirApi(revertirRequest);
        var revertirOk = Assert.IsType<OkObjectResult>(revertirResult);
        var revertirJson = System.Text.Json.JsonSerializer.Serialize(revertirOk.Value);
        var revertirDoc = System.Text.Json.JsonDocument.Parse(revertirJson);
        Assert.True(revertirDoc.RootElement.GetProperty("success").GetBoolean());

        // Verificar que el precio volvió al original
        var precioRestaurado = db.Context.ProductosPrecios.First(p => p.ProductoId == productoId && p.ListaId == listaId && p.EsVigente).Precio;
        Assert.Equal(precioOriginal, precioRestaurado);
    }

    [Fact]
    public async Task RevertirApi_conflicto_concurrencia_lanza_conflict()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (productoId, listaId) = await SeedBasicDataWithPrecio(db);
        var controller = CreateController(db, "precios:simulate", "precios:apply", "precios:revert");

        // Simular y aplicar un aumento del 10%
        var request = new AplicarRapidoRequest
        {
            Modo = "seleccionados",
            Porcentaje = 10,
            ProductoIds = new List<int> { productoId },
            ListasPrecioIds = new List<int> { listaId }
        };
        var aplicarResult = await controller.AplicarRapido(request);
        var okResult = Assert.IsType<OkObjectResult>(aplicarResult);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        var batchId = doc.RootElement.GetProperty("batchId").GetInt32();

        // Obtener rowVersion y modificar el batch (simular conflicto)
        var batch = db.Context.PriceChangeBatches.First(b => b.Id == batchId);
        var rowVersion = Convert.ToBase64String(batch.RowVersion);
        batch.Notas = "Cambio externo";
        await db.Context.SaveChangesAsync(); // Esto cambia el rowVersion

        // Intentar revertir con el rowVersion viejo
        var revertirRequest = new RevertirApiRequest
        {
            BatchId = batchId,
            RowVersion = rowVersion,
            Motivo = "Test conflicto"
        };
        var revertirResult = await controller.RevertirApi(revertirRequest);
        var conflict = Assert.IsType<ConflictObjectResult>(revertirResult);
        var conflictJson = System.Text.Json.JsonSerializer.Serialize(conflict.Value);
        Assert.Contains("modificado", conflictJson.ToLower());
    }

    #endregion
}