using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Alertas;

public class AlertaStockConcurrencyTests
{
    [Fact]
    public async Task ResolverAlertaAsync_con_RowVersion_viejo_lanza_conflicto_concurrencia_y_no_cambia_estado_en_db()
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
            StockActual = 1,
            Activo = true
        };
        db.Context.Productos.Add(producto);
        await db.Context.SaveChangesAsync();

        var alerta = new AlertaStock
        {
            ProductoId = producto.Id,
            Tipo = TipoAlertaStock.StockBajo,
            Prioridad = PrioridadAlerta.Media,
            Estado = EstadoAlerta.Pendiente,
            Mensaje = "Test",
            StockActual = 1,
            StockMinimo = 5,
            FechaAlerta = DateTime.UtcNow.AddDays(-1),
            RowVersion = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 }
        };
        db.Context.AlertasStock.Add(alerta);
        await db.Context.SaveChangesAsync();

        var rowVersionViejo = alerta.RowVersion;
        Assert.NotNull(rowVersionViejo);
        Assert.NotEmpty(rowVersionViejo);

        // Simular otra sesión que actualiza la alerta y cambia RowVersion
        await using (var ctx2 = db.CreateNewContext())
        {
            var alertaOtraSesion = await ctx2.AlertasStock.SingleAsync(a => a.Id == alerta.Id);
            alertaOtraSesion.Observaciones = "Cambio por otro usuario";
            alertaOtraSesion.RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };
            await ctx2.SaveChangesAsync();
        }

        var service = new AlertaStockService(db.Context, NullLogger<AlertaStockService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.ResolverAlertaAsync(alerta.Id, "tester", "ok", rowVersionViejo));

        Assert.Contains("modificada por otro usuario", ex.Message, StringComparison.OrdinalIgnoreCase);

        await using var ctx3 = db.CreateNewContext();
        var alertaDb = await ctx3.AlertasStock.AsNoTracking().SingleAsync(a => a.Id == alerta.Id);
        Assert.Equal(EstadoAlerta.Pendiente, alertaDb.Estado);
        Assert.Null(alertaDb.FechaResolucion);
        Assert.Null(alertaDb.UsuarioResolucion);
    }

    [Fact]
    public async Task IgnorarAlertaAsync_con_RowVersion_viejo_lanza_conflicto_concurrencia_y_no_cambia_estado_en_db()
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
            StockActual = 1,
            Activo = true
        };
        db.Context.Productos.Add(producto);
        await db.Context.SaveChangesAsync();

        var alerta = new AlertaStock
        {
            ProductoId = producto.Id,
            Tipo = TipoAlertaStock.StockBajo,
            Prioridad = PrioridadAlerta.Media,
            Estado = EstadoAlerta.Pendiente,
            Mensaje = "Test",
            StockActual = 1,
            StockMinimo = 5,
            FechaAlerta = DateTime.UtcNow.AddDays(-1),
            RowVersion = new byte[] { 2, 2, 2, 2, 2, 2, 2, 2 }
        };
        db.Context.AlertasStock.Add(alerta);
        await db.Context.SaveChangesAsync();

        var rowVersionViejo = alerta.RowVersion;
        Assert.NotNull(rowVersionViejo);
        Assert.NotEmpty(rowVersionViejo);

        // Simular otra sesión que actualiza la alerta y cambia RowVersion
        await using (var ctx2 = db.CreateNewContext())
        {
            var alertaOtraSesion = await ctx2.AlertasStock.SingleAsync(a => a.Id == alerta.Id);
            alertaOtraSesion.Observaciones = "Cambio por otro usuario";
            alertaOtraSesion.RowVersion = new byte[] { 8, 8, 8, 8, 8, 8, 8, 8 };
            await ctx2.SaveChangesAsync();
        }

        var service = new AlertaStockService(db.Context, NullLogger<AlertaStockService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.IgnorarAlertaAsync(alerta.Id, "tester", "motivo", rowVersionViejo));

        Assert.Contains("modificada por otro usuario", ex.Message, StringComparison.OrdinalIgnoreCase);

        await using var ctx3 = db.CreateNewContext();
        var alertaDb = await ctx3.AlertasStock.AsNoTracking().SingleAsync(a => a.Id == alerta.Id);
        Assert.Equal(EstadoAlerta.Pendiente, alertaDb.Estado);
        Assert.Null(alertaDb.FechaResolucion);
        Assert.Null(alertaDb.UsuarioResolucion);
    }
}
