using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

namespace TheBuryProject.Tests.Ventas;

public class VentaServicePrecioVigenteEnDetallesTests
{
    [Fact]
    public async Task CreateAsync_setea_PrecioUnitario_al_precio_vigente_de_lista_predeterminada()
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
            PrecioVenta = 999,
            StockActual = 10,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var listaPredeterminada = new ListaPrecio
        {
            Codigo = "LP_DEF",
            Nombre = "Lista Default",
            Activa = true,
            EsPredeterminada = true,
            Orden = 1
        };
        db.Context.ListasPrecios.Add(listaPredeterminada);
        await db.Context.SaveChangesAsync();

        db.Context.ProductosPrecios.Add(new ProductoPrecioLista
        {
            ProductoId = producto.Id,
            ListaId = listaPredeterminada.Id,
            VigenciaDesde = DateTime.UtcNow.AddDays(-1),
            Costo = 10,
            Precio = 123,
            MargenValor = 113,
            MargenPorcentaje = 1130,
            EsManual = true,
            EsVigente = true,
            CreadoPor = "seed"
        });
        await db.Context.SaveChangesAsync();

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var movimientoStockService = new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var ventaService = new VentaService(
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
            db.HttpContextAccessor);

        var vm = new VentaViewModel
        {
            ClienteId = cliente.Id,
            Estado = EstadoVenta.Cotizacion,
            TipoPago = TipoPago.Efectivo,
            FechaVenta = DateTime.UtcNow,
            Detalles = new List<VentaDetalleViewModel>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 2,
                    PrecioUnitario = 999,
                    Descuento = 0
                }
            }
        };

        var creada = await ventaService.CreateAsync(vm);

        var detalleDb = db.Context.VentaDetalles
            .Where(d => d.VentaId == creada.Id && !d.IsDeleted)
            .Select(d => new { d.PrecioUnitario, d.Cantidad })
            .Single();

        Assert.Equal(123m, detalleDb.PrecioUnitario);
        Assert.Equal(2, detalleDb.Cantidad);
    }

    [Fact]
    public async Task CreateAsync_con_dos_productos_setea_precios_vigentes_por_producto()
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

        var producto1 = new Producto
        {
            Codigo = "P1",
            Nombre = "Producto 1",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 10,
            PrecioVenta = 1000,
            StockActual = 10,
            Activo = true
        };

        var producto2 = new Producto
        {
            Codigo = "P2",
            Nombre = "Producto 2",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 20,
            PrecioVenta = 2000,
            StockActual = 10,
            Activo = true
        };

        db.Context.Productos.AddRange(producto1, producto2);

        var listaPredeterminada = new ListaPrecio
        {
            Codigo = "LP_DEF",
            Nombre = "Lista Default",
            Activa = true,
            EsPredeterminada = true,
            Orden = 1
        };
        db.Context.ListasPrecios.Add(listaPredeterminada);
        await db.Context.SaveChangesAsync();

        db.Context.ProductosPrecios.AddRange(
            new ProductoPrecioLista
            {
                ProductoId = producto1.Id,
                ListaId = listaPredeterminada.Id,
                VigenciaDesde = DateTime.UtcNow.AddDays(-1),
                Costo = 10,
                Precio = 111,
                MargenValor = 101,
                MargenPorcentaje = 1010,
                EsManual = true,
                EsVigente = true,
                CreadoPor = "seed"
            },
            new ProductoPrecioLista
            {
                ProductoId = producto2.Id,
                ListaId = listaPredeterminada.Id,
                VigenciaDesde = DateTime.UtcNow.AddDays(-1),
                Costo = 20,
                Precio = 222,
                MargenValor = 202,
                MargenPorcentaje = 1010,
                EsManual = true,
                EsVigente = true,
                CreadoPor = "seed"
            });
        await db.Context.SaveChangesAsync();

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var movimientoStockService = new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var ventaService = new VentaService(
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
            db.HttpContextAccessor);

        var creada = await ventaService.CreateAsync(new VentaViewModel
        {
            ClienteId = cliente.Id,
            Estado = EstadoVenta.Cotizacion,
            TipoPago = TipoPago.Efectivo,
            FechaVenta = DateTime.UtcNow,
            Detalles = new List<VentaDetalleViewModel>
            {
                new()
                {
                    ProductoId = producto1.Id,
                    Cantidad = 1,
                    PrecioUnitario = 9999,
                    Descuento = 0
                },
                new()
                {
                    ProductoId = producto2.Id,
                    Cantidad = 2,
                    PrecioUnitario = 8888,
                    Descuento = 0
                }
            }
        });

        var detallesDb = db.Context.VentaDetalles
            .Where(d => d.VentaId == creada.Id && !d.IsDeleted)
            .Select(d => new { d.ProductoId, d.PrecioUnitario, d.Cantidad })
            .ToList();

        Assert.Equal(2, detallesDb.Count);
        Assert.Contains(detallesDb, d => d.ProductoId == producto1.Id && d.PrecioUnitario == 111m && d.Cantidad == 1);
        Assert.Contains(detallesDb, d => d.ProductoId == producto2.Id && d.PrecioUnitario == 222m && d.Cantidad == 2);
    }

    [Fact]
    public async Task UpdateAsync_setea_PrecioUnitario_al_precio_vigente_actual_de_lista_predeterminada()
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
            PrecioVenta = 999,
            StockActual = 10,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var listaPredeterminada = new ListaPrecio
        {
            Codigo = "LP_DEF",
            Nombre = "Lista Default",
            Activa = true,
            EsPredeterminada = true,
            Orden = 1
        };
        db.Context.ListasPrecios.Add(listaPredeterminada);
        await db.Context.SaveChangesAsync();

        db.Context.ProductosPrecios.Add(new ProductoPrecioLista
        {
            ProductoId = producto.Id,
            ListaId = listaPredeterminada.Id,
            VigenciaDesde = DateTime.UtcNow.AddDays(-1),
            Costo = 10,
            Precio = 123,
            MargenValor = 113,
            MargenPorcentaje = 1130,
            EsManual = true,
            EsVigente = true,
            CreadoPor = "seed"
        });
        await db.Context.SaveChangesAsync();

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var movimientoStockService = new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var ventaService = new VentaService(
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
            db.HttpContextAccessor);

        var creada = await ventaService.CreateAsync(new VentaViewModel
        {
            ClienteId = cliente.Id,
            Estado = EstadoVenta.Cotizacion,
            TipoPago = TipoPago.Efectivo,
            FechaVenta = DateTime.UtcNow,
            Detalles = new List<VentaDetalleViewModel>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 2,
                    PrecioUnitario = 999,
                    Descuento = 0
                }
            }
        });

        await precioService.SetPrecioManualAsync(
            producto.Id,
            listaPredeterminada.Id,
            precio: 456,
            costo: 10,
            vigenciaDesde: DateTime.UtcNow.AddSeconds(-1));

        var vmParaEditar = await ventaService.GetByIdAsync(creada.Id);
        Assert.NotNull(vmParaEditar);

        // En SQLite in-memory, el RowVersion puede no estar disponible en la entidad trackeada/mapeada.
        // Para simular el roundtrip real, lo leemos desde la DB.
        vmParaEditar!.RowVersion = await db.Context.Ventas
            .AsNoTracking()
            .Where(v => v.Id == creada.Id)
            .Select(v => v.RowVersion)
            .SingleAsync();

        if (vmParaEditar.RowVersion == null || vmParaEditar.RowVersion.Length == 0)
        {
            // Fallback: forzar un RowVersion no-vacío para poder ejercitar UpdateAsync en SQLite.
            await db.Context.Database.ExecuteSqlRawAsync(
                "UPDATE Ventas SET RowVersion = randomblob(8) WHERE Id = {0}",
                creada.Id);

            vmParaEditar.RowVersion = await db.Context.Ventas
                .AsNoTracking()
                .Where(v => v.Id == creada.Id)
                .Select(v => v.RowVersion)
                .SingleAsync();
        }

        Assert.NotNull(vmParaEditar.RowVersion);
        Assert.NotEmpty(vmParaEditar.RowVersion);

        vmParaEditar.Detalles.Single().PrecioUnitario = 9999;

        var actualizada = await ventaService.UpdateAsync(creada.Id, vmParaEditar);
        Assert.NotNull(actualizada);

        var detalleDb = db.Context.VentaDetalles
            .Where(d => d.VentaId == creada.Id && !d.IsDeleted)
            .Select(d => new { d.PrecioUnitario, d.Cantidad })
            .Single();

        Assert.Equal(456m, detalleDb.PrecioUnitario);
        Assert.Equal(2, detalleDb.Cantidad);
    }
}
