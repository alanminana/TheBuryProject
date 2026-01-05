using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Validators;
using TheBuryProject.Tests.TestDoubles;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.CajaTests;

/// <summary>
/// Tests para validar que las ventas requieren caja abierta.
/// </summary>
public class VentaRequiereCajaAbiertaTests
{
    [Fact]
    public async Task CreateAsync_SinCajaAbierta_LanzaExcepcion()
    {
        using var db = new SqliteInMemoryDb("tester");
        await db.Context.Database.EnsureCreatedAsync();

        var (cliente, producto) = await SetupTestDataAsync(db);

        // ✅ Crear VentaService con NoopCajaService que indica NO hay caja abierta
        var ventaService = CreateVentaService(db, hayCajaAbierta: false);

        var ventaViewModel = new VentaViewModel
        {
            ClienteId = cliente.Id,
            FechaVenta = DateTime.UtcNow,
            TipoPago = TipoPago.Efectivo,
            Estado = EstadoVenta.Presupuesto,
            Detalles = new List<VentaDetalleViewModel>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 1,
                    PrecioUnitario = 100
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            ventaService.CreateAsync(ventaViewModel));

        Assert.Contains("no hay ninguna caja abierta", ex.Message);
        Assert.Contains("abra una caja", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ConCajaAbierta_PermiteVenta()
    {
        using var db = new SqliteInMemoryDb("tester");
        await db.Context.Database.EnsureCreatedAsync();

        var (cliente, producto) = await SetupTestDataAsync(db);

        // ✅ Crear VentaService con NoopCajaService que indica SÍ hay caja abierta
        var ventaService = CreateVentaService(db, hayCajaAbierta: true);

        var ventaViewModel = new VentaViewModel
        {
            ClienteId = cliente.Id,
            FechaVenta = DateTime.UtcNow,
            TipoPago = TipoPago.Efectivo,
            Estado = EstadoVenta.Presupuesto,
            Detalles = new List<VentaDetalleViewModel>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 1,
                    PrecioUnitario = 100
                }
            }
        };

        // Act - No debería lanzar excepción
        var resultado = await ventaService.CreateAsync(ventaViewModel);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(resultado.Id > 0);
    }

    #region Helpers

    private static VentaService CreateVentaService(SqliteInMemoryDb db, bool hayCajaAbierta)
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
            new NoopValidacionVentaService(),
            new NoopCajaService(hayCajaAbierta)); // ✅ Parámetro para controlar si hay caja abierta
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

    #endregion
}
