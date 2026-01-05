using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Validators;
using TheBuryProject.Tests.TestDoubles;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Ventas;

public class VentaFacturacionAutorizacionFlowTests
{
    [Fact]
    public async Task Confirmar_y_facturar_no_modifica_stock_y_crea_factura()
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
            Numero = "VTA-TEST-000001",
            ClienteId = cliente.Id,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.Efectivo,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 60,
            IVA = 0,
            Total = 60,
            RequiereAutorizacion = false,
            EstadoAutorizacion = EstadoAutorizacionVenta.NoRequiere,
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
            db.HttpContextAccessor,
            new NoopValidacionVentaService(),
            new NoopCajaService());

        var okConfirmar = await ventaService.ConfirmarVentaAsync(venta.Id);
        Assert.True(okConfirmar);

        var productoTrasConfirmar = await db.Context.Productos.FindAsync(producto.Id);
        Assert.NotNull(productoTrasConfirmar);
        Assert.Equal(7, productoTrasConfirmar!.StockActual);

        var movimientosTrasConfirmar = db.Context.MovimientosStock
            .Where(m => m.ProductoId == producto.Id && !m.IsDeleted)
            .ToList();
        Assert.Single(movimientosTrasConfirmar);

        var facturaVm = new FacturaViewModel
        {
            VentaId = venta.Id,
            Numero = "MANUAL-NO-DEBE-USARSE",
            Tipo = TipoFactura.B,
            FechaEmision = DateTime.Today,
            Subtotal = venta.Subtotal,
            IVA = venta.IVA,
            Total = venta.Total
        };

        var okFacturar = await ventaService.FacturarVentaAsync(venta.Id, facturaVm);
        Assert.True(okFacturar);

        var ventaFacturada = await db.Context.Ventas
            .Where(v => v.Id == venta.Id)
            .Select(v => new { v.Estado, v.FechaFacturacion })
            .FirstAsync();

        Assert.Equal(EstadoVenta.Facturada, ventaFacturada.Estado);
        Assert.NotNull(ventaFacturada.FechaFacturacion);

        var facturas = db.Context.Facturas.Where(f => f.VentaId == venta.Id && !f.IsDeleted).ToList();
        Assert.Single(facturas);

        var periodo = DateTime.Now.ToString(VentaConstants.FORMATO_PERIODO);
        Assert.StartsWith($"{VentaConstants.FacturaPrefijos.TIPO_B}-{periodo}-", facturas[0].Numero);
        Assert.NotEqual("MANUAL-NO-DEBE-USARSE", facturas[0].Numero);

        var productoTrasFacturar = await db.Context.Productos.FindAsync(producto.Id);
        Assert.NotNull(productoTrasFacturar);
        Assert.Equal(7, productoTrasFacturar!.StockActual);

        var movimientosFinal = db.Context.MovimientosStock
            .Where(m => m.ProductoId == producto.Id && !m.IsDeleted)
            .ToList();

        Assert.Single(movimientosFinal);
    }

    [Fact]
    public async Task Autorizar_y_rechazar_no_pisan_motivo_de_solicitud_y_guardan_resultado()
    {
        using var db = new SqliteInMemoryDb(userName: "boss");

        var cliente = new Cliente
        {
            TipoDocumento = "DNI",
            NumeroDocumento = "888",
            Apellido = "Boss",
            Nombre = "User",
            Telefono = "123",
            Domicilio = "Calle",
            Activo = true
        };
        db.Context.Clientes.Add(cliente);
        await db.Context.SaveChangesAsync();

        var venta1 = new Venta
        {
            Numero = "VTA-AUTH-0001",
            ClienteId = cliente.Id,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.CreditoPersonall,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 100,
            IVA = 0,
            Total = 100,
            RequiereAutorizacion = true,
            EstadoAutorizacion = EstadoAutorizacionVenta.PendienteAutorizacion,
            UsuarioSolicita = "seller",
            MotivoAutorizacion = "Supera limite",
            FechaSolicitudAutorizacion = DateTime.UtcNow
        };

        var venta2 = new Venta
        {
            Numero = "VTA-AUTH-0002",
            ClienteId = cliente.Id,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.CreditoPersonall,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 200,
            IVA = 0,
            Total = 200,
            RequiereAutorizacion = true,
            EstadoAutorizacion = EstadoAutorizacionVenta.PendienteAutorizacion,
            UsuarioSolicita = "seller",
            MotivoAutorizacion = "Supera limite 2",
            FechaSolicitudAutorizacion = DateTime.UtcNow
        };

        db.Context.Ventas.AddRange(venta1, venta2);
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
            db.HttpContextAccessor,
            new NoopValidacionVentaService(),
            new NoopCajaService());

        var okAutorizar = await ventaService.AutorizarVentaAsync(venta1.Id, "admin", "OK por excepcion");
        Assert.True(okAutorizar);

        var venta1Db = await db.Context.Ventas.FindAsync(venta1.Id);
        Assert.NotNull(venta1Db);
        Assert.Equal(EstadoAutorizacionVenta.Autorizada, venta1Db!.EstadoAutorizacion);
        Assert.Equal("admin", venta1Db.UsuarioAutoriza);
        // E3: MotivoAutorizacion ahora guarda la observación del autorizador
        Assert.Equal("OK por excepcion", venta1Db.MotivoAutorizacion);

        var okRechazar = await ventaService.RechazarVentaAsync(venta2.Id, "admin", "No cumple requisitos");
        Assert.True(okRechazar);

        var venta2Db = await db.Context.Ventas.FindAsync(venta2.Id);
        Assert.NotNull(venta2Db);
        Assert.Equal(EstadoAutorizacionVenta.Rechazada, venta2Db!.EstadoAutorizacion);
        Assert.Equal("admin", venta2Db.UsuarioAutoriza);
        Assert.Equal("No cumple requisitos", venta2Db.MotivoRechazo);
    }
}
