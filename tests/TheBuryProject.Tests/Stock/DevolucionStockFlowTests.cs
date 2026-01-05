using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Stock;

public class DevolucionStockFlowTests
{
    [Fact]
    public async Task CompletarDevolucionAsync_reintegra_stock_y_registra_movimiento_con_usuario()
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
            StockActual = 0,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var venta = new Venta
        {
            Numero = "V-0001",
            ClienteId = cliente.Id,
            Estado = EstadoVenta.Confirmada,
            TipoPago = TipoPago.Efectivo,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 0,
            IVA = 0,
            Total = 0
        };
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var devolucion = new Devolucion
        {
            VentaId = venta.Id,
            ClienteId = cliente.Id,
            NumeroDevolucion = "DEV-0001",
            Motivo = MotivoDevolucion.Otro,
            Descripcion = "Test",
            Estado = EstadoDevolucion.Aprobada,
            TotalDevolucion = 200
        };
        db.Context.Devoluciones.Add(devolucion);
        await db.Context.SaveChangesAsync();

        db.Context.DevolucionDetalles.Add(new DevolucionDetalle
        {
            DevolucionId = devolucion.Id,
            ProductoId = producto.Id,
            Cantidad = 2,
            PrecioUnitario = 100,
            Subtotal = 200,
            EstadoProducto = EstadoProductoDevuelto.Nuevo,
            AccionRecomendada = AccionProducto.ReintegrarStock
        });
        await db.Context.SaveChangesAsync();

        var movimientoStockService = new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance);
        var devolucionService = new DevolucionService(db.Context, movimientoStockService, db.HttpContextAccessor);

        var rowVersion = devolucion.RowVersion;
        Assert.NotNull(rowVersion);
        await devolucionService.CompletarDevolucionAsync(devolucion.Id, rowVersion!);

        var productoActualizado = await db.Context.Productos.FindAsync(producto.Id);
        Assert.NotNull(productoActualizado);
        Assert.Equal(2, productoActualizado!.StockActual);
        Assert.Equal("tester", productoActualizado.UpdatedBy);

        var movimiento = db.Context.MovimientosStock.Single(m => m.ProductoId == producto.Id && !m.IsDeleted);
        Assert.Equal(TipoMovimiento.Entrada, movimiento.Tipo);
        Assert.Equal(2, movimiento.Cantidad);
        Assert.Equal("tester", movimiento.CreatedBy);
        Assert.NotNull(movimiento.Referencia);
        Assert.Contains("DEV-", movimiento.Referencia!);
    }
}
