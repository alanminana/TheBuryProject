using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Precios;

public class PrecioServiceBatchFlowTests
{
    [Fact]
    public async Task SimularBatch_setea_CantidadProductos_como_distinct_productos_afectados()
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
            PrecioCompra = 100,
            PrecioVenta = 200,
            StockActual = 0,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var lista = new ListaPrecio
        {
            Codigo = "LP1",
            Nombre = "Lista 1",
            Activa = true,
            Orden = 1
        };
        db.Context.ListasPrecios.Add(lista);
        await db.Context.SaveChangesAsync();

        // Precio vigente inicial
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

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "test",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 10,
            listasIds: new List<int> { lista.Id });

        Assert.Equal(1, batch.CantidadProductos);
        Assert.Equal(EstadoBatch.Simulado, batch.Estado);
    }

    [Fact]
    public async Task Aplicar_y_revertir_batch_no_deja_dos_precios_vigentes_por_producto_y_lista()
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
            PrecioCompra = 100,
            PrecioVenta = 200,
            StockActual = 0,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var lista = new ListaPrecio
        {
            Codigo = "LP1",
            Nombre = "Lista 1",
            Activa = true,
            Orden = 1
        };
        db.Context.ListasPrecios.Add(lista);
        await db.Context.SaveChangesAsync();

        var precioInicial = new ProductoPrecioLista
        {
            ProductoId = producto.Id,
            ListaId = lista.Id,
            VigenciaDesde = DateTime.UtcNow.AddDays(-2),
            Costo = 100,
            Precio = 200,
            MargenValor = 100,
            MargenPorcentaje = 100,
            EsManual = true,
            EsVigente = true,
            CreadoPor = "seed"
        };
        db.Context.ProductosPrecios.Add(precioInicial);
        await db.Context.SaveChangesAsync();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "test",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 10,
            listasIds: new List<int> { lista.Id });

        var rowVersionSimulado = batch.RowVersion;
        var aprobado = await precioService.AprobarBatchAsync(batch.Id, aprobadoPor: "boss", rowVersion: rowVersionSimulado);

        var fechaVigencia = DateTime.UtcNow;
        var rowVersionAprobado = aprobado.RowVersion;
        var aplicado = await precioService.AplicarBatchAsync(batch.Id, aplicadoPor: "boss", rowVersion: rowVersionAprobado, fechaVigencia: fechaVigencia);

        var vigentesTrasAplicar = db.Context.ProductosPrecios
            .Where(p => p.ProductoId == producto.Id && p.ListaId == lista.Id && p.EsVigente && !p.IsDeleted)
            .ToList();

        Assert.Single(vigentesTrasAplicar);
        Assert.Equal(batch.Id, vigentesTrasAplicar[0].BatchId);
        Assert.Equal(220, vigentesTrasAplicar[0].Precio);

        // Revertir
        var rowVersionAplicado = aplicado.RowVersion;
        await precioService.RevertirBatchAsync(batch.Id, revertidoPor: "boss", rowVersion: rowVersionAplicado, motivo: "test revert");

        var vigentesTrasRevertir = db.Context.ProductosPrecios
            .Where(p => p.ProductoId == producto.Id && p.ListaId == lista.Id && p.EsVigente && !p.IsDeleted)
            .ToList();

        Assert.Single(vigentesTrasRevertir);
        Assert.Equal(200, vigentesTrasRevertir[0].Precio);
    }

    [Fact]
    public async Task ExportarHistorialPrecios_devuelve_xlsx_con_contenido()
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
            PrecioCompra = 100,
            PrecioVenta = 200,
            StockActual = 0,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var lista = new ListaPrecio
        {
            Codigo = "LP1",
            Nombre = "Lista 1",
            Activa = true,
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

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var bytes = await precioService.ExportarHistorialPreciosAsync(
            productoIds: new List<int> { producto.Id },
            fechaDesde: DateTime.UtcNow.AddDays(-10),
            fechaHasta: DateTime.UtcNow.AddDays(10));

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        // XLSX es un ZIP, típicamente comienza con 'PK'
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }
}
