using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Precios;

public class PrecioServiceManualPriceFlowTests
{
    [Fact]
    public async Task SetPrecioManualAsync_no_deja_dos_precios_vigentes_por_producto_y_lista()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var categoria = new Categoria { Codigo = "CAT", Nombre = "Categoria", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

        var producto = new Producto
        {
            Codigo = "P-001",
            Nombre = "Prod",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 10,
            PrecioVenta = 20,
            StockActual = 0,
            Activo = true
        };

        var lista = new ListaPrecio
        {
            Nombre = "Minorista",
            Codigo = "MIN",
            Descripcion = "Lista",
            Activa = true,
            EsPredeterminada = false,
            Orden = 1
        };

        db.Context.Productos.Add(producto);
        db.Context.ListasPrecios.Add(lista);
        await db.Context.SaveChangesAsync();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        await precioService.SetPrecioManualAsync(producto.Id, lista.Id, precio: 100m, costo: 80m);
        await precioService.SetPrecioManualAsync(producto.Id, lista.Id, precio: 120m, costo: 90m);

        var vigentes = db.Context.ProductosPrecios
            .Where(p => p.ProductoId == producto.Id && p.ListaId == lista.Id && p.EsVigente && !p.IsDeleted)
            .ToList();

        Assert.Single(vigentes);
        Assert.Equal(120m, vigentes[0].Precio);
    }
}
