using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TheBuryProject.Data;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.Services.Validators;
using TheBuryProject.Tests.TestDoubles;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Ventas;

/// <summary>
/// Tests para E2: Guardado condicional basado en prevalidación.
/// - NoViable → NO guardar y mostrar razones
/// - RequiereAutorizacion → Guardar en estado PendienteAutorizacion con razones
/// - Aprobable → Guardar sin crear crédito definitivo
/// </summary>
public class VentaCreditoGuardadoE2Tests
{
    #region Helper Methods

    private static VentaService CreateVentaService(SqliteInMemoryDb db, Mock<IValidacionVentaService> mockValidacionVenta)
    {
        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var movimientoStockService = new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        return new VentaService(
            db.Context,
            mapper,
            NullLogger<VentaService>.Instance,
            new NoopConfiguracionPagoService(),
            new NoopAlertaStockService(),
            movimientoStockService,
            new ThrowingFinancialCalculationService(),
            new VentaValidator(),
            new VentaNumberGenerator(db.Context),
            precioService,
            db.HttpContextAccessor,
            mockValidacionVenta.Object);
    }

    private static async Task<(Cliente cliente, Producto producto)> SetupTestDataAsync(SqliteInMemoryDb db)
    {
        var categoria = new Categoria { Codigo = "CAT", Nombre = "Categoria", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

        var cliente = new Cliente
        {
            TipoDocumento = "DNI",
            NumeroDocumento = "12345678",
            Apellido = "Perez",
            Nombre = "Juan",
            Telefono = "123",
            Domicilio = "Calle 123",
            Activo = true
        };
        db.Context.Clientes.Add(cliente);
        await db.Context.SaveChangesAsync();

        var producto = new Producto
        {
            Codigo = "P1",
            Nombre = "Producto Test",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 100,
            PrecioVenta = 200,
            StockActual = 50,
            Activo = true
        };
        db.Context.Productos.Add(producto);
        await db.Context.SaveChangesAsync();

        return (cliente, producto);
    }

    private static VentaViewModel CrearVentaViewModel(int clienteId, int productoId)
    {
        return new VentaViewModel
        {
            ClienteId = clienteId,
            FechaVenta = DateTime.Now,
            TipoPago = TipoPago.CreditoPersonall,
            Estado = EstadoVenta.Presupuesto,
            Detalles = new List<VentaDetalleViewModel>
            {
                new()
                {
                    ProductoId = productoId,
                    Cantidad = 1,
                    PrecioUnitario = 200m,
                    Descuento = 0,
                    Subtotal = 200m
                }
            }
        };
    }

    #endregion

    #region Tests: NoViable - NO guardar

    [Fact]
    public async Task CreateAsync_CreditoPersonal_NoViable_LanzaExcepcion()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var mockValidacionVenta = new Mock<IValidacionVentaService>();
        var (cliente, producto) = await SetupTestDataAsync(db);
        var viewModel = CrearVentaViewModel(cliente.Id, producto.Id);
        var ventaService = CreateVentaService(db, mockValidacionVenta);

        mockValidacionVenta.Setup(x => x.ValidarVentaCreditoPersonalAsync(
                cliente.Id, It.IsAny<decimal>(), It.IsAny<int?>()))
            .ReturnsAsync(new ValidacionVentaResult
            {
                NoViable = true,
                PendienteRequisitos = true,
                EstadoAptitud = EstadoCrediticioCliente.NoApto,
                RequisitosPendientes = new List<RequisitoPendiente>
                {
                    new() 
                    { 
                        Tipo = TipoRequisitoPendiente.DocumentacionFaltante,
                        Descripcion = "Faltan documentos obligatorios",
                        AccionRequerida = "Cargar documentación"
                    }
                }
            });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ventaService.CreateAsync(viewModel));

        Assert.Contains("No es posible crear la venta con crédito personal", ex.Message);
        Assert.Contains("Faltan documentos obligatorios", ex.Message);

        // Verificar que NO se guardó la venta
        var ventasEnDb = await db.Context.Ventas.CountAsync();
        Assert.Equal(0, ventasEnDb);
    }

    [Fact]
    public async Task CreateAsync_CreditoPersonal_NoViable_SinLimiteCredito_LanzaExcepcion()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var mockValidacionVenta = new Mock<IValidacionVentaService>();
        var (cliente, producto) = await SetupTestDataAsync(db);
        var viewModel = CrearVentaViewModel(cliente.Id, producto.Id);
        var ventaService = CreateVentaService(db, mockValidacionVenta);

        mockValidacionVenta.Setup(x => x.ValidarVentaCreditoPersonalAsync(
                cliente.Id, It.IsAny<decimal>(), It.IsAny<int?>()))
            .ReturnsAsync(new ValidacionVentaResult
            {
                NoViable = true,
                PendienteRequisitos = true,
                EstadoAptitud = EstadoCrediticioCliente.NoEvaluado,
                RequisitosPendientes = new List<RequisitoPendiente>
                {
                    new() 
                    { 
                        Tipo = TipoRequisitoPendiente.SinLimiteCredito,
                        Descripcion = "Cliente sin límite de crédito asignado",
                        AccionRequerida = "Asignar límite de crédito"
                    }
                }
            });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ventaService.CreateAsync(viewModel));

        Assert.Contains("No es posible crear la venta", ex.Message);
        
        // Verificar que NO se guardó la venta
        var ventasEnDb = await db.Context.Ventas.CountAsync();
        Assert.Equal(0, ventasEnDb);
    }

    #endregion

    #region Tests: RequiereAutorizacion - Guardar con estado PendienteAutorizacion

    [Fact]
    public async Task CreateAsync_CreditoPersonal_RequiereAutorizacion_GuardaConEstadoPendiente()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var mockValidacionVenta = new Mock<IValidacionVentaService>();
        var (cliente, producto) = await SetupTestDataAsync(db);
        var viewModel = CrearVentaViewModel(cliente.Id, producto.Id);
        var ventaService = CreateVentaService(db, mockValidacionVenta);

        mockValidacionVenta.Setup(x => x.ValidarVentaCreditoPersonalAsync(
                cliente.Id, It.IsAny<decimal>(), It.IsAny<int?>()))
            .ReturnsAsync(new ValidacionVentaResult
            {
                RequiereAutorizacion = true,
                EstadoAptitud = EstadoCrediticioCliente.RequiereAutorizacion,
                RazonesAutorizacion = new List<RazonAutorizacion>
                {
                    new() 
                    { 
                        Tipo = TipoRazonAutorizacion.MoraActiva,
                        Descripcion = "Cliente tiene mora activa",
                        DetalleAdicional = "15 días de mora"
                    }
                }
            });

        // Act
        var resultado = await ventaService.CreateAsync(viewModel);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(resultado.RequiereAutorizacion);
        Assert.Equal(EstadoAutorizacionVenta.PendienteAutorizacion, resultado.EstadoAutorizacion);

        // Verificar en base de datos
        var ventaEnDb = await db.Context.Ventas.FirstOrDefaultAsync(v => v.Id == resultado.Id);
        Assert.NotNull(ventaEnDb);
        Assert.True(ventaEnDb.RequiereAutorizacion);
        Assert.Equal(EstadoAutorizacionVenta.PendienteAutorizacion, ventaEnDb.EstadoAutorizacion);
        Assert.NotNull(ventaEnDb.RazonesAutorizacionJson);
        Assert.Contains("Cliente tiene mora activa", ventaEnDb.RazonesAutorizacionJson);
    }

    [Fact]
    public async Task CreateAsync_CreditoPersonal_RequiereAutorizacion_PersisteRazones()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var mockValidacionVenta = new Mock<IValidacionVentaService>();
        var (cliente, producto) = await SetupTestDataAsync(db);
        var viewModel = CrearVentaViewModel(cliente.Id, producto.Id);
        var ventaService = CreateVentaService(db, mockValidacionVenta);

        mockValidacionVenta.Setup(x => x.ValidarVentaCreditoPersonalAsync(
                cliente.Id, It.IsAny<decimal>(), It.IsAny<int?>()))
            .ReturnsAsync(new ValidacionVentaResult
            {
                RequiereAutorizacion = true,
                EstadoAptitud = EstadoCrediticioCliente.RequiereAutorizacion,
                RazonesAutorizacion = new List<RazonAutorizacion>
                {
                    new() 
                    { 
                        Tipo = TipoRazonAutorizacion.ExcedeCupo,
                        Descripcion = "Monto excede cupo disponible",
                        DetalleAdicional = "Solicitado: $50000, Disponible: $30000",
                        ValorAsociado = 50000,
                        ValorLimite = 30000
                    }
                }
            });

        // Act
        var resultado = await ventaService.CreateAsync(viewModel);

        // Assert
        var ventaEnDb = await db.Context.Ventas.FirstOrDefaultAsync(v => v.Id == resultado.Id);
        Assert.NotNull(ventaEnDb?.RazonesAutorizacionJson);
        
        // Parsear el JSON y verificar contenido
        var razones = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(ventaEnDb.RazonesAutorizacionJson);
        Assert.NotNull(razones);
        Assert.Single(razones);
    }

    [Fact]
    public async Task CreateAsync_CreditoPersonal_RequiereAutorizacion_NoCreaCredito()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var mockValidacionVenta = new Mock<IValidacionVentaService>();
        var (cliente, producto) = await SetupTestDataAsync(db);
        var viewModel = CrearVentaViewModel(cliente.Id, producto.Id);
        var ventaService = CreateVentaService(db, mockValidacionVenta);

        mockValidacionVenta.Setup(x => x.ValidarVentaCreditoPersonalAsync(
                cliente.Id, It.IsAny<decimal>(), It.IsAny<int?>()))
            .ReturnsAsync(new ValidacionVentaResult
            {
                RequiereAutorizacion = true,
                EstadoAptitud = EstadoCrediticioCliente.RequiereAutorizacion,
                RazonesAutorizacion = new List<RazonAutorizacion>
                {
                    new() { Tipo = TipoRazonAutorizacion.MoraActiva, Descripcion = "Mora activa" }
                }
            });

        // Act
        var resultado = await ventaService.CreateAsync(viewModel);

        // Assert - No debe haber CreditoId asignado ni cuotas creadas
        var ventaEnDb = await db.Context.Ventas
            .Include(v => v.VentaCreditoCuotas)
            .FirstOrDefaultAsync(v => v.Id == resultado.Id);
        
        Assert.Null(ventaEnDb?.CreditoId);
        Assert.Empty(ventaEnDb?.VentaCreditoCuotas ?? new List<VentaCreditoCuota>());
    }

    #endregion

    #region Tests: Aprobable - Guardar sin crédito definitivo

    [Fact]
    public async Task CreateAsync_CreditoPersonal_Aprobable_GuardaCorrectamente()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var mockValidacionVenta = new Mock<IValidacionVentaService>();
        var (cliente, producto) = await SetupTestDataAsync(db);
        var viewModel = CrearVentaViewModel(cliente.Id, producto.Id);
        var ventaService = CreateVentaService(db, mockValidacionVenta);

        mockValidacionVenta.Setup(x => x.ValidarVentaCreditoPersonalAsync(
                cliente.Id, It.IsAny<decimal>(), It.IsAny<int?>()))
            .ReturnsAsync(new ValidacionVentaResult
            {
                // Aprobable = no NoViable, no RequiereAutorizacion, no PendienteRequisitos
                EstadoAptitud = EstadoCrediticioCliente.Apto
            });

        // Act
        var resultado = await ventaService.CreateAsync(viewModel);

        // Assert
        Assert.NotNull(resultado);
        Assert.False(resultado.RequiereAutorizacion);
        Assert.Equal(EstadoAutorizacionVenta.NoRequiere, resultado.EstadoAutorizacion);

        // Verificar que se guardó
        var ventaEnDb = await db.Context.Ventas.FirstOrDefaultAsync(v => v.Id == resultado.Id);
        Assert.NotNull(ventaEnDb);
        Assert.False(ventaEnDb.RequiereAutorizacion);
    }

    [Fact]
    public async Task CreateAsync_CreditoPersonal_Aprobable_NoCreaCredito()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var mockValidacionVenta = new Mock<IValidacionVentaService>();
        var (cliente, producto) = await SetupTestDataAsync(db);
        var viewModel = CrearVentaViewModel(cliente.Id, producto.Id);
        var ventaService = CreateVentaService(db, mockValidacionVenta);

        mockValidacionVenta.Setup(x => x.ValidarVentaCreditoPersonalAsync(
                cliente.Id, It.IsAny<decimal>(), It.IsAny<int?>()))
            .ReturnsAsync(new ValidacionVentaResult
            {
                EstadoAptitud = EstadoCrediticioCliente.Apto
            });

        // Act
        var resultado = await ventaService.CreateAsync(viewModel);

        // Assert - No debe haber CreditoId ni cuotas creadas todavía
        var ventaEnDb = await db.Context.Ventas
            .Include(v => v.VentaCreditoCuotas)
            .FirstOrDefaultAsync(v => v.Id == resultado.Id);
        
        Assert.Null(ventaEnDb?.CreditoId);
        Assert.Empty(ventaEnDb?.VentaCreditoCuotas ?? new List<VentaCreditoCuota>());
    }

    [Fact]
    public async Task CreateAsync_CreditoPersonal_Aprobable_MantieneTipoPago()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var mockValidacionVenta = new Mock<IValidacionVentaService>();
        var (cliente, producto) = await SetupTestDataAsync(db);
        var viewModel = CrearVentaViewModel(cliente.Id, producto.Id);
        var ventaService = CreateVentaService(db, mockValidacionVenta);

        mockValidacionVenta.Setup(x => x.ValidarVentaCreditoPersonalAsync(
                cliente.Id, It.IsAny<decimal>(), It.IsAny<int?>()))
            .ReturnsAsync(new ValidacionVentaResult
            {
                EstadoAptitud = EstadoCrediticioCliente.Apto
            });

        // Act
        var resultado = await ventaService.CreateAsync(viewModel);

        // Assert - Tipo de pago debe mantenerse
        var ventaEnDb = await db.Context.Ventas.FirstOrDefaultAsync(v => v.Id == resultado.Id);
        Assert.Equal(TipoPago.CreditoPersonall, ventaEnDb?.TipoPago);
    }

    #endregion

    #region Tests: Otros tipos de pago no afectados

    [Fact]
    public async Task CreateAsync_Efectivo_NoLlamaValidacionCredito()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var mockValidacionVenta = new Mock<IValidacionVentaService>();
        var (cliente, producto) = await SetupTestDataAsync(db);
        var viewModel = CrearVentaViewModel(cliente.Id, producto.Id);
        viewModel.TipoPago = TipoPago.Efectivo; // Cambiar a efectivo
        var ventaService = CreateVentaService(db, mockValidacionVenta);

        // Act
        var resultado = await ventaService.CreateAsync(viewModel);

        // Assert
        Assert.NotNull(resultado);
        
        // Verificar que NO se llamó a la validación de crédito
        mockValidacionVenta.Verify(
            x => x.ValidarVentaCreditoPersonalAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int?>()),
            Times.Never);
    }

    #endregion
}
