using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Precios;

public class PrecioServiceCambioPrecioDirectoTests
{
    [Fact]
    public async Task AplicarCambioPrecioDirecto_actualiza_precio_10_por_ciento_en_seleccionados()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var (categoria, marca) = await SeedCategoriaMarcaAsync(db);

        var producto1 = new Producto
        {
            Codigo = "P-100",
            Nombre = "Prod 100",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 50m,
            PrecioVenta = 100m,
            StockActual = 0,
            Activo = true
        };

        var producto2 = new Producto
        {
            Codigo = "P-200",
            Nombre = "Prod 200",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 80m,
            PrecioVenta = 200m,
            StockActual = 0,
            Activo = true
        };

        db.Context.Productos.AddRange(producto1, producto2);
        await db.Context.SaveChangesAsync();

        var service = CreatePrecioService(db);

        var model = new AplicarCambioPrecioDirectoViewModel
        {
            Alcance = "seleccionados",
            ValorPorcentaje = 10m,
            ProductoIdsText = $"{producto1.Id},{producto2.Id}",
            Motivo = "Test aumento"
        };

        var result = await service.AplicarCambioPrecioDirectoAsync(model);

        Assert.True(result.Exitoso);
        Assert.Equal(2, result.ProductosActualizados);
        Assert.NotNull(result.CambioPrecioEventoId);

        var productos = db.Context.Productos
            .Where(p => p.Id == producto1.Id || p.Id == producto2.Id)
            .OrderBy(p => p.Id)
            .ToList();

        Assert.Equal(110m, productos[0].PrecioVenta);
        Assert.Equal(220m, productos[1].PrecioVenta);
    }

    [Fact]
    public async Task RevertirCambioPrecioEvento_restaura_precios_originales()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var (categoria, marca) = await SeedCategoriaMarcaAsync(db);

        var producto = new Producto
        {
            Codigo = "P-REV",
            Nombre = "Prod Revertir",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 40m,
            PrecioVenta = 100m,
            StockActual = 0,
            Activo = true
        };

        db.Context.Productos.Add(producto);
        await db.Context.SaveChangesAsync();

        var service = CreatePrecioService(db);

        var applyResult = await service.AplicarCambioPrecioDirectoAsync(new AplicarCambioPrecioDirectoViewModel
        {
            Alcance = "seleccionados",
            ValorPorcentaje = 10m,
            ProductoIdsText = producto.Id.ToString(),
            Motivo = "Test revert"
        });

        Assert.True(applyResult.Exitoso);
        Assert.NotNull(applyResult.CambioPrecioEventoId);

        var revertResult = await service.RevertirCambioPrecioEventoAsync(applyResult.CambioPrecioEventoId!.Value);

        Assert.True(revertResult.Exitoso);

        var refreshed = await db.Context.Productos.FindAsync(producto.Id);
        Assert.NotNull(refreshed);
        Assert.Equal(100m, refreshed!.PrecioVenta);
    }

    [Fact]
    public async Task AplicarCambioPrecioDirecto_filtrados_usa_filtros_json()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var categoria = new Categoria { Codigo = "CAT1", Nombre = "Cat 1", Activo = true };
        var otraCategoria = new Categoria { Codigo = "CAT2", Nombre = "Cat 2", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
        db.Context.Categorias.AddRange(categoria, otraCategoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

        var match = new Producto
        {
            Codigo = "MATCH",
            Nombre = "Producto Match",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 10m,
            PrecioVenta = 50m,
            StockActual = 1,
            StockMinimo = 5,
            Activo = true
        };

        var noMatch = new Producto
        {
            Codigo = "NOMATCH",
            Nombre = "Producto No Match",
            CategoriaId = otraCategoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 10m,
            PrecioVenta = 50m,
            StockActual = 10,
            StockMinimo = 5,
            Activo = true
        };

        db.Context.Productos.AddRange(match, noMatch);
        await db.Context.SaveChangesAsync();

        var service = CreatePrecioService(db);

        var filtros = new
        {
            CategoriaId = categoria.Id,
            SoloActivos = true,
            StockBajo = true
        };

        var model = new AplicarCambioPrecioDirectoViewModel
        {
            Alcance = "filtrados",
            ValorPorcentaje = 10m,
            FiltrosJson = JsonSerializer.Serialize(filtros)
        };

        var result = await service.AplicarCambioPrecioDirectoAsync(model);

        Assert.True(result.Exitoso);
        Assert.Equal(1, result.ProductosActualizados);

        var refrescadoMatch = await db.Context.Productos.FindAsync(match.Id);
        var refrescadoNoMatch = await db.Context.Productos.FindAsync(noMatch.Id);

        Assert.Equal(55m, refrescadoMatch!.PrecioVenta);
        Assert.Equal(50m, refrescadoNoMatch!.PrecioVenta);
    }

    private static PrecioService CreatePrecioService(SqliteInMemoryDb db)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        return new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);
    }

    private static async Task<(Categoria categoria, Marca marca)> SeedCategoriaMarcaAsync(SqliteInMemoryDb db)
    {
        var categoria = new Categoria { Codigo = "CAT", Nombre = "Categoria", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

        return (categoria, marca);
    }
}
