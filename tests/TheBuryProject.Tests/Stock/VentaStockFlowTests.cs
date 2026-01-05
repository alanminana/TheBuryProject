using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestDoubles;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Stock;

public class VentaStockFlowTests
{
    [Fact]
    public async Task Confirmar_y_cancelar_venta_actualiza_stock_y_registra_movimientos_con_usuario()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var categoria = new Categoria { Codigo = "CAT", Nombre = "Categoria", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);

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
            Nombre = "Producto",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 10,
            PrecioVenta = 20,
            StockActual = 10,
            Activo = true
        };
        db.Context.Productos.Add(producto);
        await db.Context.SaveChangesAsync();

        var venta = new Venta
        {
            Numero = "V-TEST-0001",
            ClienteId = cliente.Id,
            Estado = EstadoVenta.Cotizacion,
            TipoPago = TipoPago.Efectivo,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 60,
            IVA = 0,
            Total = 60,
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 3,
                    PrecioUnitario = 20,
                    Subtotal = 60
                }
            }
        };

        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        IMapper mapper = null!;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var movimientoStockService = new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance);

        var ventaService = new VentaService(
            db.Context,
            mapper,
            NullLogger<VentaService>.Instance,
            new NoopConfiguracionPagoService(),
            new NoopAlertaStockService(),
            movimientoStockService,
            new ThrowingFinancialCalculationService(),
            new NoopVentaValidator(),
            new VentaNumberGenerator(db.Context),
            precioService,
            db.HttpContextAccessor,
            new NoopValidacionVentaService(),
            new NoopCajaService());

        var okConfirmar = await ventaService.ConfirmarVentaAsync(venta.Id);
        Assert.True(okConfirmar);

        var productoTrasConfirmar = await db.Context.Productos.FindAsync(producto.Id);
        Assert.NotNull(productoTrasConfirmar);
        Assert.Equal(7, productoTrasConfirmar!.StockActual);
        Assert.Equal("tester", productoTrasConfirmar.UpdatedBy);

        var movimientosTrasConfirmar = db.Context.MovimientosStock
            .Where(m => m.ProductoId == producto.Id && !m.IsDeleted)
            .ToList();

        Assert.Single(movimientosTrasConfirmar);
        Assert.Equal(TipoMovimiento.Salida, movimientosTrasConfirmar[0].Tipo);
        Assert.Equal(3, movimientosTrasConfirmar[0].Cantidad);
        Assert.Equal("tester", movimientosTrasConfirmar[0].CreatedBy);
        Assert.Contains("Venta", movimientosTrasConfirmar[0].Referencia ?? string.Empty);

        var okCancelar = await ventaService.CancelarVentaAsync(venta.Id, "Cancel test");
        Assert.True(okCancelar);

        var productoTrasCancelar = await db.Context.Productos.FindAsync(producto.Id);
        Assert.NotNull(productoTrasCancelar);
        Assert.Equal(10, productoTrasCancelar!.StockActual);
        Assert.Equal("tester", productoTrasCancelar.UpdatedBy);

        var movimientosFinal = db.Context.MovimientosStock
            .Where(m => m.ProductoId == producto.Id && !m.IsDeleted)
            .OrderBy(m => m.Id)
            .ToList();

        Assert.Equal(2, movimientosFinal.Count);
        Assert.Equal(TipoMovimiento.Salida, movimientosFinal[0].Tipo);
        Assert.Equal(TipoMovimiento.Entrada, movimientosFinal[1].Tipo);
        Assert.Equal("tester", movimientosFinal[1].CreatedBy);
        Assert.Contains("Cancelación", movimientosFinal[1].Referencia ?? string.Empty);
    }
}
