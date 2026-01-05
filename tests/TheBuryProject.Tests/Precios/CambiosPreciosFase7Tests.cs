using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Precios;

/// <summary>
/// Tests para la funcionalidad de Cambios de Precios - FASE 7
/// Verifica: preselección de filtros, navegación y estados de batch
/// </summary>
public class CambiosPreciosFase7Tests
{
    #region Preselección de filtros en Simular

    [Fact]
    public async Task SimularCambioMasivo_acepta_filtros_por_categorias()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (categoriaId, listaId) = await SeedBasicDataWithPrecio(db);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        // Act - Simular con filtro por categoría
        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "Test con categoría",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 10,
            listasIds: new List<int> { listaId },
            categoriaIds: new List<int> { categoriaId });

        // Assert
        Assert.NotNull(batch);
        Assert.Equal(1, batch.CantidadProductos);
        Assert.Equal(EstadoBatch.Simulado, batch.Estado);
    }

    [Fact]
    public async Task SimularCambioMasivo_acepta_filtros_por_marcas()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);
        var marcaId = db.Context.Marcas.First().Id;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        // Act - Simular con filtro por marca
        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "Test con marca",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 15,
            listasIds: new List<int> { listaId },
            marcaIds: new List<int> { marcaId });

        // Assert
        Assert.NotNull(batch);
        Assert.Equal(1, batch.CantidadProductos);
    }

    [Fact]
    public async Task SimularCambioMasivo_acepta_filtros_por_productos_especificos()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);
        var productoId = db.Context.Productos.First().Id;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        // Act - Simular con productos específicos
        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "Test con productos específicos",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 20,
            listasIds: new List<int> { listaId },
            productoIds: new List<int> { productoId });

        // Assert
        Assert.NotNull(batch);
        Assert.Equal(1, batch.CantidadProductos);
    }

    [Fact]
    public async Task SimularCambioMasivo_con_multiples_filtros_aplica_todos()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (categoriaId, listaId) = await SeedBasicDataWithPrecio(db);
        var marcaId = db.Context.Marcas.First().Id;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        // Act - Simular con múltiples filtros
        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "Test con múltiples filtros",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 25,
            listasIds: new List<int> { listaId },
            categoriaIds: new List<int> { categoriaId },
            marcaIds: new List<int> { marcaId });

        // Assert
        Assert.NotNull(batch);
        Assert.Equal(1, batch.CantidadProductos);
    }

    #endregion

    #region Estados de batch

    [Fact]
    public async Task Batch_simulado_puede_ser_aprobado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "Test aprobar",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 10,
            listasIds: new List<int> { listaId });

        // Act
        var batchAprobado = await precioService.AprobarBatchAsync(batch.Id, "approver", batch.RowVersion);

        // Assert
        Assert.NotNull(batchAprobado);
        Assert.Equal(EstadoBatch.Aprobado, batchAprobado.Estado);
    }

    [Fact]
    public async Task Batch_simulado_puede_ser_rechazado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "Test rechazar",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 10,
            listasIds: new List<int> { listaId });

        // Act
        var batchRechazado = await precioService.RechazarBatchAsync(batch.Id, "approver", batch.RowVersion, "Motivo de rechazo");

        // Assert
        Assert.NotNull(batchRechazado);
        Assert.Equal(EstadoBatch.Rechazado, batchRechazado.Estado);
        Assert.Equal("Motivo de rechazo", batchRechazado.MotivoRechazo);
    }

    [Fact]
    public async Task Batch_aprobado_puede_ser_aplicado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "Test aplicar",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 10,
            listasIds: new List<int> { listaId });

        var batchAprobado = await precioService.AprobarBatchAsync(batch.Id, "approver", batch.RowVersion);

        // Act
        var batchAplicado = await precioService.AplicarBatchAsync(
            batch.Id, 
            "applier",
            batchAprobado.RowVersion, 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        Assert.NotNull(batchAplicado);
        Assert.Equal(EstadoBatch.Aplicado, batchAplicado.Estado);
    }

    [Fact]
    public async Task Batch_rechazado_no_permite_aprobar()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (_, listaId) = await SeedBasicDataWithPrecio(db);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var batch = await precioService.SimularCambioMasivoAsync(
            nombre: "Test rechazado",
            tipoCambio: TipoCambio.PorcentajeSobrePrecioActual,
            tipoAplicacion: TipoAplicacion.Aumento,
            valorCambio: 10,
            listasIds: new List<int> { listaId });

        var batchRechazado = await precioService.RechazarBatchAsync(batch.Id, "approver", batch.RowVersion, "Rechazado");

        // Act & Assert - Intentar aprobar un batch rechazado debería lanzar excepción
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await precioService.AprobarBatchAsync(batch.Id, "approver", batchRechazado.RowVersion));
    }

    #endregion

    #region Helpers

    private static async Task<(int categoriaId, int listaId)> SeedBasicDataWithPrecio(SqliteInMemoryDb db)
    {
        var categoria = new Categoria { Codigo = "CAT", Nombre = "Categoria", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

        var producto = new Producto
        {
            Codigo = "P1",
            Nombre = "Producto Test",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 100,
            PrecioVenta = 200,
            StockActual = 10,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var lista = new ListaPrecio
        {
            Codigo = "LP1",
            Nombre = "Lista Test",
            Activa = true,
            EsPredeterminada = true,
            Orden = 1
        };
        db.Context.ListasPrecios.Add(lista);
        await db.Context.SaveChangesAsync();

        // Crear precio vigente
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

        return (categoria.Id, lista.Id);
    }

    #endregion
}
