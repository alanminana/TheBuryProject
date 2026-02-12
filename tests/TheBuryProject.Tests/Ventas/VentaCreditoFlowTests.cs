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

/// <summary>
/// Tests para Etapa 3: Crédito
/// Verifica que las cuotas se crean solo al confirmar y que rechazar/cancelar limpia datos correctamente.
/// </summary>
public class VentaCreditoFlowTests
{
    private static VentaService CreateVentaService(
        SqliteInMemoryDb db,
        AperturaCaja? aperturaActiva = null)
    {
        aperturaActiva ??= db.CrearAperturaCajaActivaAsync().GetAwaiter().GetResult();

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var movimientoStockService = new MovimientoStockService(db.Context, NullLogger<MovimientoStockService>.Instance);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        return new VentaService(
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
            new NoopCajaService(aperturaActiva: aperturaActiva));
    }

    private static async Task<(Cliente cliente, Credito credito, Producto producto)> SetupTestDataAsync(SqliteInMemoryDb db)
    {
        var categoria = new Categoria { Codigo = "CAT", Nombre = "Categoria", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

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

        var credito = new Credito
        {
            ClienteId = cliente.Id,
            Numero = "CRED-001",
            MontoSolicitado = 10000m,
            MontoAprobado = 10000m,
            SaldoPendiente = 10000m,
            Estado = EstadoCredito.Activo,
            FechaSolicitud = DateTime.UtcNow.AddDays(-30),
            FechaAprobacion = DateTime.UtcNow.AddDays(-29)
        };
        db.Context.Creditos.Add(credito);
        await db.Context.SaveChangesAsync();

        var producto = new Producto
        {
            Codigo = "P1",
            Nombre = "Producto Test",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 100,
            PrecioVenta = 200,
            StockActual = 50,
            Activo = true
        };
        db.Context.Productos.Add(producto);
        await db.Context.SaveChangesAsync();

        return (cliente, credito, producto);
    }

    [Fact]
    public async Task Crear_venta_credito_no_crea_cuotas_hasta_confirmar()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, credito, producto) = await SetupTestDataAsync(db);
        var ventaService = CreateVentaService(db);

        // Crear venta con crédito personal - plan guardado como JSON
        var venta = new Venta
        {
            Numero = "VTA-CRED-001",
            ClienteId = cliente.Id,
            CreditoId = credito.Id,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.CreditoPersonal,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 600,
            IVA = 0,
            Total = 600,
            RequiereAutorizacion = false,
            EstadoAutorizacion = EstadoAutorizacionVenta.NoRequiere,
            // Simular el JSON que guarda GuardarPlanCreditoPersonallAsync
            DatosCreditoPersonallJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                CreditoId = credito.Id,
                MontoAFinanciar = 600m,
                CantidadCuotas = 3,
                MontoCuota = 210m,
                TotalAPagar = 630m,
                TasaInteresMensual = 5m,
                FechaPrimeraCuota = DateTime.Today.AddMonths(1),
                InteresTotal = 30m
            }),
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 3,
                    PrecioUnitario = 200,
                    Subtotal = 600
                }
            }
        };

        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        // Act - Verificar que NO hay cuotas antes de confirmar
        var cuotasAntes = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .ToListAsync();

        // Assert - No deben existir cuotas
        Assert.Empty(cuotasAntes);
        Assert.NotNull(venta.DatosCreditoPersonallJson);
    }

    [Fact]
    public async Task Confirmar_venta_credito_crea_cuotas_desde_json()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, credito, producto) = await SetupTestDataAsync(db);
        var ventaService = CreateVentaService(db);

        var fechaPrimeraCuota = DateTime.Today.AddMonths(1);

        var venta = new Venta
        {
            Numero = "VTA-CRED-002",
            ClienteId = cliente.Id,
            CreditoId = credito.Id,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.CreditoPersonal,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 600,
            IVA = 0,
            Total = 600,
            RequiereAutorizacion = false,
            EstadoAutorizacion = EstadoAutorizacionVenta.NoRequiere,
            DatosCreditoPersonallJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                CreditoId = credito.Id,
                MontoAFinanciar = 600m,
                CantidadCuotas = 3,
                MontoCuota = 210m,
                TotalAPagar = 630m,
                TasaInteresMensual = 5m,
                FechaPrimeraCuota = fechaPrimeraCuota,
                InteresTotal = 30m
            }),
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 3,
                    PrecioUnitario = 200,
                    Subtotal = 600
                }
            }
        };

        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        // Act
        var resultado = await ventaService.ConfirmarVentaAsync(venta.Id);

        // Assert
        Assert.True(resultado);

        var cuotasDespues = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .OrderBy(c => c.NumeroCuota)
            .ToListAsync();

        Assert.Equal(3, cuotasDespues.Count);
        Assert.All(cuotasDespues, c => Assert.Equal(210m, c.Monto));
        Assert.All(cuotasDespues, c => Assert.False(c.Pagada));

        // Verificar fechas de vencimiento
        Assert.Equal(fechaPrimeraCuota.Date, cuotasDespues[0].FechaVencimiento.Date);
        Assert.Equal(fechaPrimeraCuota.AddMonths(1).Date, cuotasDespues[1].FechaVencimiento.Date);
        Assert.Equal(fechaPrimeraCuota.AddMonths(2).Date, cuotasDespues[2].FechaVencimiento.Date);

        // Verificar que se limpió el JSON temporal
        var ventaActualizada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.Null(ventaActualizada!.DatosCreditoPersonallJson);

        // Verificar que se descontó del saldo del crédito
        var creditoActualizado = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(10000m - 600m, creditoActualizado!.SaldoPendiente);
    }

    [Fact]
    public async Task Rechazar_venta_limpia_datos_credito_sin_crear_cuotas()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (cliente, credito, producto) = await SetupTestDataAsync(db);
        var ventaService = CreateVentaService(db);

        var saldoOriginal = credito.SaldoPendiente;

        var venta = new Venta
        {
            Numero = "VTA-CRED-003",
            ClienteId = cliente.Id,
            CreditoId = credito.Id,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.CreditoPersonal,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 600,
            IVA = 0,
            Total = 600,
            RequiereAutorizacion = true,
            EstadoAutorizacion = EstadoAutorizacionVenta.PendienteAutorizacion,
            UsuarioSolicita = "seller",
            MotivoAutorizacion = "Supera limite",
            FechaSolicitudAutorizacion = DateTime.UtcNow,
            DatosCreditoPersonallJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                CreditoId = credito.Id,
                MontoAFinanciar = 600m,
                CantidadCuotas = 3,
                MontoCuota = 210m,
                TotalAPagar = 630m,
                TasaInteresMensual = 5m,
                FechaPrimeraCuota = DateTime.Today.AddMonths(1),
                InteresTotal = 30m
            }),
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 3,
                    PrecioUnitario = 200,
                    Subtotal = 600
                }
            }
        };

        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        // Act
        var resultado = await ventaService.RechazarVentaAsync(venta.Id, "admin", "No cumple requisitos");

        // Assert
        Assert.True(resultado);

        // Verificar que NO hay cuotas
        var cuotas = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .ToListAsync();
        Assert.Empty(cuotas);

        // Verificar que se limpió el JSON
        var ventaRechazada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.NotNull(ventaRechazada);
        Assert.Null(ventaRechazada!.DatosCreditoPersonallJson);
        Assert.Null(ventaRechazada.CreditoId);
        Assert.Equal(EstadoAutorizacionVenta.Rechazada, ventaRechazada.EstadoAutorizacion);
        Assert.Equal("No cumple requisitos", ventaRechazada.MotivoRechazo);

        // Verificar que el saldo del crédito no cambió
        var creditoActualizado = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(saldoOriginal, creditoActualizado!.SaldoPendiente);
    }

    [Fact]
    public async Task Cancelar_venta_pendiente_limpia_datos_credito_sin_modificar_saldo()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, credito, producto) = await SetupTestDataAsync(db);
        var ventaService = CreateVentaService(db);

        var saldoOriginal = credito.SaldoPendiente;

        var venta = new Venta
        {
            Numero = "VTA-CRED-004",
            ClienteId = cliente.Id,
            CreditoId = credito.Id,
            Estado = EstadoVenta.Presupuesto, // No confirmada
            TipoPago = TipoPago.CreditoPersonal,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 600,
            IVA = 0,
            Total = 600,
            RequiereAutorizacion = false,
            EstadoAutorizacion = EstadoAutorizacionVenta.NoRequiere,
            DatosCreditoPersonallJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                CreditoId = credito.Id,
                MontoAFinanciar = 600m,
                CantidadCuotas = 3,
                MontoCuota = 210m,
                TotalAPagar = 630m,
                TasaInteresMensual = 5m,
                FechaPrimeraCuota = DateTime.Today.AddMonths(1),
                InteresTotal = 30m
            }),
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 3,
                    PrecioUnitario = 200,
                    Subtotal = 600
                }
            }
        };

        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        // Act
        var resultado = await ventaService.CancelarVentaAsync(venta.Id, "Cliente desistió");

        // Assert
        Assert.True(resultado);

        // Verificar que NO hay cuotas
        var cuotas = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .ToListAsync();
        Assert.Empty(cuotas);

        // Verificar que se limpió el JSON
        var ventaCancelada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.NotNull(ventaCancelada);
        Assert.Null(ventaCancelada!.DatosCreditoPersonallJson);
        Assert.Null(ventaCancelada.CreditoId);
        Assert.Equal(EstadoVenta.Cancelada, ventaCancelada.Estado);
        Assert.Equal("Cliente desistió", ventaCancelada.MotivoCancelacion);

        // El saldo del crédito debe permanecer igual (nunca se descontó)
        var creditoActualizado = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(saldoOriginal, creditoActualizado!.SaldoPendiente);
    }

    [Fact]
    public async Task Cancelar_venta_confirmada_restaura_credito_y_elimina_cuotas()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, credito, producto) = await SetupTestDataAsync(db);
        var ventaService = CreateVentaService(db);

        var saldoOriginal = credito.SaldoPendiente;

        var venta = new Venta
        {
            Numero = "VTA-CRED-005",
            ClienteId = cliente.Id,
            CreditoId = credito.Id,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.CreditoPersonal,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 600,
            IVA = 0,
            Total = 600,
            RequiereAutorizacion = false,
            EstadoAutorizacion = EstadoAutorizacionVenta.NoRequiere,
            DatosCreditoPersonallJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                CreditoId = credito.Id,
                MontoAFinanciar = 600m,
                CantidadCuotas = 3,
                MontoCuota = 210m,
                TotalAPagar = 630m,
                TasaInteresMensual = 5m,
                FechaPrimeraCuota = DateTime.Today.AddMonths(1),
                InteresTotal = 30m
            }),
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 3,
                    PrecioUnitario = 200,
                    Subtotal = 600
                }
            }
        };

        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        // Confirmar primero
        var okConfirmar = await ventaService.ConfirmarVentaAsync(venta.Id);
        Assert.True(okConfirmar);

        // Verificar que se crearon cuotas y se descontó saldo
        var cuotasPostConfirm = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .ToListAsync();
        Assert.Equal(3, cuotasPostConfirm.Count);

        var creditoPostConfirm = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(saldoOriginal - 600m, creditoPostConfirm!.SaldoPendiente);

        // Act - Cancelar la venta confirmada
        var okCancelar = await ventaService.CancelarVentaAsync(venta.Id, "Devolución del cliente");

        // Assert
        Assert.True(okCancelar);

        // Verificar que se eliminaron las cuotas
        var cuotasPostCancelar = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .ToListAsync();
        Assert.Empty(cuotasPostCancelar);

        // Verificar que se restauró el saldo del crédito
        var creditoPostCancelar = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(saldoOriginal, creditoPostCancelar!.SaldoPendiente);

        // Verificar estado de la venta
        var ventaCancelada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.Equal(EstadoVenta.Cancelada, ventaCancelada!.Estado);
    }

    [Fact]
    public async Task Sin_json_credito_confirmar_no_crea_cuotas()
    {
        // E4: Si no hay JSON de crédito, la venta se confirma sin crear cuotas
        // (el flujo correcto requiere JSON para ventas con crédito personal)
        
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, credito, producto) = await SetupTestDataAsync(db);
        var ventaService = CreateVentaService(db);

        // Venta con crédito pero sin JSON (caso edge - no debería ocurrir en flujo normal)
        var venta = new Venta
        {
            Numero = "VTA-CRED-006",
            ClienteId = cliente.Id,
            // E4: CreditoId NO se asigna aquí, se asigna desde JSON al confirmar
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.CreditoPersonal,
            FechaVenta = DateTime.UtcNow,
            Subtotal = 600,
            IVA = 0,
            Total = 600,
            RequiereAutorizacion = false,
            EstadoAutorizacion = EstadoAutorizacionVenta.NoRequiere,
            DatosCreditoPersonallJson = null, // Sin JSON
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 3,
                    PrecioUnitario = 200,
                    Subtotal = 600
                }
            }
        };

        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        // Act - E4: Sin JSON, la venta se confirma pero sin crear cuotas ni asignar CreditoId
        var resultado = await ventaService.ConfirmarVentaAsync(venta.Id);

        // Assert
        Assert.True(resultado);
        
        var ventaConfirmada = await db.Context.Ventas
            .Include(v => v.VentaCreditoCuotas)
            .FirstOrDefaultAsync(v => v.Id == venta.Id);
        
        Assert.Equal(EstadoVenta.Confirmada, ventaConfirmada!.Estado);
        // Sin JSON no se asigna CreditoId ni se crean cuotas
        Assert.Null(ventaConfirmada.CreditoId);
        Assert.Empty(ventaConfirmada.VentaCreditoCuotas);
    }
}

