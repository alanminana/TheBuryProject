using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Devoluciones;

public class DevolucionConcurrencyTests
{
    [Fact]
    public async Task AprobarDevolucionAsync_con_RowVersion_viejo_lanza_conflicto_concurrencia_y_no_genera_nota_credito()
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
            Estado = EstadoDevolucion.Pendiente,
            TotalDevolucion = 100
        };
        db.Context.Devoluciones.Add(devolucion);
        await db.Context.SaveChangesAsync();

        var rowVersionViejo = devolucion.RowVersion;
        Assert.NotNull(rowVersionViejo);

        // Simular otra sesión que actualiza la devolución y cambia RowVersion
        await using (var ctx2 = db.CreateNewContext())
        {
            var devOtraSesion = await ctx2.Devoluciones.SingleAsync(d => d.Id == devolucion.Id);
            devOtraSesion.ObservacionesInternas = "Cambio por otro usuario";
            devOtraSesion.RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };
            await ctx2.SaveChangesAsync();
        }

        var devolucionService = new DevolucionService(
            db.Context,
            new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance),
            db.HttpContextAccessor);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await devolucionService.AprobarDevolucionAsync(devolucion.Id, "tester", rowVersionViejo!));

        Assert.Contains("modificada por otro usuario", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await db.Context.NotasCredito.AnyAsync(nc => nc.DevolucionId == devolucion.Id && !nc.IsDeleted));
    }

    [Fact]
    public async Task CompletarDevolucionAsync_con_RowVersion_viejo_lanza_conflicto_concurrencia_y_no_actualiza_stock()
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

        var rowVersionViejo = devolucion.RowVersion;
        Assert.NotNull(rowVersionViejo);

        // Simular otra sesión que cambia RowVersion antes de completar
        await using (var ctx2 = db.CreateNewContext())
        {
            var devOtraSesion = await ctx2.Devoluciones.SingleAsync(d => d.Id == devolucion.Id);
            devOtraSesion.ObservacionesInternas = "Cambio por otro usuario";
            devOtraSesion.RowVersion = new byte[] { 8, 8, 8, 8, 8, 8, 8, 8 };
            await ctx2.SaveChangesAsync();
        }

        var movimientoStockService = new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance);
        var devolucionService = new DevolucionService(db.Context, movimientoStockService, db.HttpContextAccessor);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await devolucionService.CompletarDevolucionAsync(devolucion.Id, rowVersionViejo!));

        Assert.Contains("modificada por otro usuario", ex.Message, StringComparison.OrdinalIgnoreCase);

        var productoActualizado = await db.Context.Productos.FindAsync(producto.Id);
        Assert.NotNull(productoActualizado);
        Assert.Equal(0, productoActualizado!.StockActual);
        Assert.False(await db.Context.MovimientosStock.AnyAsync(m => m.ProductoId == producto.Id && !m.IsDeleted));
    }
}
