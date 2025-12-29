using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Stock;

public class OrdenCompraStockFlowTests
{
    [Fact]
    public async Task RecepcionarAsync_actualiza_stock_y_registra_movimiento_con_usuario_y_orden()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var categoria = new Categoria { Codigo = "CAT", Nombre = "Categoria", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

        var producto = new Producto
        {
            Codigo = "P1",
            Nombre = "Producto",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 10,
            PrecioVenta = 20,
            StockActual = 0,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var proveedor = new Proveedor { Cuit = "20304050607", RazonSocial = "Proveedor" };
        db.Context.Proveedores.Add(proveedor);
        await db.Context.SaveChangesAsync();

        var orden = new OrdenCompra
        {
            Numero = "OC-TEST-0001",
            ProveedorId = proveedor.Id,
            Estado = EstadoOrdenCompra.Confirmada,
            Detalles = new List<OrdenCompraDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 10,
                    CantidadRecibida = 0,
                    PrecioUnitario = 10,
                    Subtotal = 100
                }
            }
        };

        db.Context.OrdenesCompra.Add(orden);
        await db.Context.SaveChangesAsync();

        var movimientoStockService = new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance);
        var ordenCompraService = new OrdenCompraService(
            db.Context,
            NullLogger<OrdenCompraService>.Instance,
            movimientoStockService,
            db.HttpContextAccessor);

        var detalleId = orden.Detalles.First().Id;
        await ordenCompraService.RecepcionarAsync(
            orden.Id,
            orden.RowVersion,
            new List<RecepcionDetalleViewModel>
            {
                new()
                {
                    DetalleId = detalleId,
                    ProductoId = producto.Id,
                    CantidadSolicitada = 10,
                    CantidadYaRecibida = 0,
                    CantidadARecepcionar = 5
                }
            });

        var productoActualizado = await db.Context.Productos.FindAsync(producto.Id);
        Assert.NotNull(productoActualizado);
        Assert.Equal(5, productoActualizado!.StockActual);
        Assert.Equal("tester", productoActualizado.UpdatedBy);

        var movimiento = db.Context.MovimientosStock.Single(m => m.ProductoId == producto.Id && !m.IsDeleted);
        Assert.Equal(TipoMovimiento.Entrada, movimiento.Tipo);
        Assert.Equal(5, movimiento.Cantidad);
        Assert.Equal("tester", movimiento.CreatedBy);
        Assert.Equal(orden.Id, movimiento.OrdenCompraId);
    }
}
