using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Validators;
using TheBuryProject.Tests.TestDoubles;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Ventas;

/// <summary>
/// Tests para E4: Creación de crédito definitivo y cuotas.
/// - Crédito+cuotas se crean solo cuando la venta pasa a Confirmada (post-autorización si aplica).
/// - Si la venta se rechaza/cancela: no debe quedar crédito "fantasma".
/// - Consistencia con cupo manual (descontar al aprobar).
/// </summary>
public class VentaCreditoDefinitivoE4Tests
{
    #region Helper Methods

    private static VentaService CreateVentaService(SqliteInMemoryDb db)
    {
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
            new NoopCajaService());
    }

    private static async Task<(Cliente cliente, Producto producto, Credito credito)> SetupTestDataAsync(SqliteInMemoryDb db)
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

        // Crear crédito con cupo disponible
        var credito = new Credito
        {
            ClienteId = cliente.Id,
            Numero = "CRED-001",
            MontoSolicitado = 50000m,
            MontoAprobado = 50000m,
            SaldoPendiente = 50000m, // Cupo disponible
            Estado = EstadoCredito.Activo,
            FechaSolicitud = DateTime.UtcNow.AddDays(-30),
            FechaAprobacion = DateTime.UtcNow.AddDays(-29),
            TasaInteres = 5m
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

        return (cliente, producto, credito);
    }

    private static Venta CrearVentaConPlanCredito(
        Cliente cliente, 
        Producto producto, 
        Credito credito,
        decimal montoTotal,
        bool requiereAutorizacion,
        EstadoAutorizacionVenta estadoAutorizacion)
    {
        var planCredito = System.Text.Json.JsonSerializer.Serialize(new
        {
            CreditoId = credito.Id,
            MontoAFinanciar = montoTotal,
            CantidadCuotas = 3,
            MontoCuota = montoTotal / 3,
            TotalAPagar = montoTotal * 1.05m,
            TasaInteresMensual = 5m,
            FechaPrimeraCuota = DateTime.Today.AddMonths(1),
            InteresTotal = montoTotal * 0.05m
        });

        return new Venta
        {
            Numero = $"VTA-E4-{Guid.NewGuid():N}".Substring(0, 20),
            ClienteId = cliente.Id,
            FechaVenta = DateTime.UtcNow,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.CreditoPersonall,
            Subtotal = montoTotal,
            IVA = 0,
            Total = montoTotal,
            RequiereAutorizacion = requiereAutorizacion,
            EstadoAutorizacion = estadoAutorizacion,
            DatosCreditoPersonallJson = planCredito,
            // CreditoId NO se asigna aquí - se asigna al confirmar (E4)
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = (int)(montoTotal / producto.PrecioVenta),
                    PrecioUnitario = producto.PrecioVenta,
                    Subtotal = montoTotal
                }
            }
        };
    }

    #endregion

    #region Tests: Creación de crédito al confirmar (sin autorización requerida)

    [Fact]
    public async Task ConfirmarVenta_SinAutorizacionRequerida_CreaCuotas()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, 600m, 
            requiereAutorizacion: false, 
            estadoAutorizacion: EstadoAutorizacionVenta.NoRequiere);
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act
        var resultado = await ventaService.ConfirmarVentaAsync(venta.Id);

        // Assert
        Assert.True(resultado);
        
        var cuotas = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .ToListAsync();
        
        Assert.Equal(3, cuotas.Count);
        Assert.All(cuotas, c => Assert.Equal(credito.Id, c.CreditoId));
    }

    [Fact]
    public async Task ConfirmarVenta_SinAutorizacionRequerida_AsignaCreditoId()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, 600m, 
            requiereAutorizacion: false, 
            estadoAutorizacion: EstadoAutorizacionVenta.NoRequiere);
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.ConfirmarVentaAsync(venta.Id);

        // Assert - CreditoId se asigna al confirmar
        var ventaActualizada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.Equal(credito.Id, ventaActualizada?.CreditoId);
    }

    [Fact]
    public async Task ConfirmarVenta_SinAutorizacionRequerida_DescuentaCupo()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var cupoInicial = credito.SaldoPendiente;
        var montoVenta = 600m;
        
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, montoVenta, 
            requiereAutorizacion: false, 
            estadoAutorizacion: EstadoAutorizacionVenta.NoRequiere);
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.ConfirmarVentaAsync(venta.Id);

        // Assert - Cupo descontado
        var creditoActualizado = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(cupoInicial - montoVenta, creditoActualizado?.SaldoPendiente);
    }

    #endregion

    #region Tests: Creación de crédito post-autorización

    [Fact]
    public async Task ConfirmarVenta_ConAutorizacionAprobada_CreaCuotas()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, 1000m, 
            requiereAutorizacion: true, 
            estadoAutorizacion: EstadoAutorizacionVenta.Autorizada);
        venta.UsuarioAutoriza = "admin";
        venta.FechaAutorizacion = DateTime.Now;
        venta.MotivoAutorizacion = "Aprobado por excepción";
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act
        var resultado = await ventaService.ConfirmarVentaAsync(venta.Id);

        // Assert
        Assert.True(resultado);
        
        var cuotas = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .ToListAsync();
        
        Assert.Equal(3, cuotas.Count);
    }

    [Fact]
    public async Task ConfirmarVenta_ConAutorizacionAprobada_DescuentaCupo()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var cupoInicial = credito.SaldoPendiente;
        var montoVenta = 1000m;
        
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, montoVenta, 
            requiereAutorizacion: true, 
            estadoAutorizacion: EstadoAutorizacionVenta.Autorizada);
        venta.UsuarioAutoriza = "admin";
        venta.FechaAutorizacion = DateTime.Now;
        venta.MotivoAutorizacion = "Aprobado";
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.ConfirmarVentaAsync(venta.Id);

        // Assert
        var creditoActualizado = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(cupoInicial - montoVenta, creditoActualizado?.SaldoPendiente);
    }

    [Fact]
    public async Task ConfirmarVenta_PendienteAutorizacion_Falla()
    {
        // Arrange - Venta requiere autorización pero aún está pendiente
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, 1000m, 
            requiereAutorizacion: true, 
            estadoAutorizacion: EstadoAutorizacionVenta.PendienteAutorizacion);
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ventaService.ConfirmarVentaAsync(venta.Id));

        Assert.Contains("autoriza", ex.Message.ToLower());
        
        // Verificar que NO se crearon cuotas
        var cuotas = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .CountAsync();
        Assert.Equal(0, cuotas);
    }

    #endregion

    #region Tests: Evitar créditos fantasma (rechazo/cancelación)

    [Fact]
    public async Task RechazarVenta_NoCreaCredito()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var cupoInicial = credito.SaldoPendiente;
        
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, 1000m, 
            requiereAutorizacion: true, 
            estadoAutorizacion: EstadoAutorizacionVenta.PendienteAutorizacion);
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.RechazarVentaAsync(venta.Id, "admin", "No cumple requisitos");

        // Assert - No debe haber CreditoId ni cuotas
        var ventaRechazada = await db.Context.Ventas
            .Include(v => v.VentaCreditoCuotas)
            .FirstOrDefaultAsync(v => v.Id == venta.Id);
        
        Assert.Null(ventaRechazada?.CreditoId);
        Assert.Empty(ventaRechazada?.VentaCreditoCuotas ?? new List<VentaCreditoCuota>());
        Assert.Null(ventaRechazada?.DatosCreditoPersonallJson);

        // Cupo NO debe haberse descontado
        var creditoActualizado = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(cupoInicial, creditoActualizado?.SaldoPendiente);
    }

    [Fact]
    public async Task CancelarVenta_AntesDeConfirmar_NoCreaCredito()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var cupoInicial = credito.SaldoPendiente;
        
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, 1000m, 
            requiereAutorizacion: false, 
            estadoAutorizacion: EstadoAutorizacionVenta.NoRequiere);
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.CancelarVentaAsync(venta.Id, "Cambio de opinión");

        // Assert
        var ventaCancelada = await db.Context.Ventas
            .Include(v => v.VentaCreditoCuotas)
            .FirstOrDefaultAsync(v => v.Id == venta.Id);
        
        Assert.Null(ventaCancelada?.CreditoId);
        Assert.Empty(ventaCancelada?.VentaCreditoCuotas ?? new List<VentaCreditoCuota>());

        // Cupo intacto
        var creditoActualizado = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(cupoInicial, creditoActualizado?.SaldoPendiente);
    }

    #endregion

    #region Tests: Consistencia de cupo

    [Fact]
    public async Task ConfirmarVenta_SaldoInsuficiente_Falla()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        
        // Reducir el saldo disponible
        credito.SaldoPendiente = 100m;
        await db.Context.SaveChangesAsync();
        
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, 600m, 
            requiereAutorizacion: false, 
            estadoAutorizacion: EstadoAutorizacionVenta.NoRequiere);
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ventaService.ConfirmarVentaAsync(venta.Id));

        Assert.Contains("insuficiente", ex.Message.ToLower());
    }

    [Fact]
    public async Task ConfirmarMultiplesVentas_DescuentaCupoAcumulado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        
        // Asegurar suficiente stock para las dos ventas
        producto.StockActual = 200;
        await db.Context.SaveChangesAsync();
        
        var cupoInicial = credito.SaldoPendiente; // 50000
        
        var venta1 = CrearVentaConPlanCredito(cliente, producto, credito, 10000m, 
            requiereAutorizacion: false, 
            estadoAutorizacion: EstadoAutorizacionVenta.NoRequiere);
        var venta2 = CrearVentaConPlanCredito(cliente, producto, credito, 15000m, 
            requiereAutorizacion: false, 
            estadoAutorizacion: EstadoAutorizacionVenta.NoRequiere);
        
        db.Context.Ventas.AddRange(venta1, venta2);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.ConfirmarVentaAsync(venta1.Id);
        await ventaService.ConfirmarVentaAsync(venta2.Id);

        // Assert
        var creditoActualizado = await db.Context.Creditos.FindAsync(credito.Id);
        Assert.Equal(cupoInicial - 10000m - 15000m, creditoActualizado?.SaldoPendiente);
    }

    [Fact]
    public async Task ConfirmarVenta_LimpiaDatosJsonTemporal()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, 600m, 
            requiereAutorizacion: false, 
            estadoAutorizacion: EstadoAutorizacionVenta.NoRequiere);
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        // Verificar que tiene JSON antes de confirmar
        Assert.NotNull(venta.DatosCreditoPersonallJson);

        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.ConfirmarVentaAsync(venta.Id);

        // Assert - JSON temporal debe haberse limpiado
        var ventaConfirmada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.Null(ventaConfirmada?.DatosCreditoPersonallJson);
    }

    #endregion

    #region Tests: Cuotas correctas

    [Fact]
    public async Task ConfirmarVenta_CuotasTienenDatosCorrectos()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "tester");
        var (cliente, producto, credito) = await SetupTestDataAsync(db);
        var montoVenta = 600m;
        var cantidadCuotas = 3;
        var montoCuota = montoVenta / cantidadCuotas;
        
        var venta = CrearVentaConPlanCredito(cliente, producto, credito, montoVenta, 
            requiereAutorizacion: false, 
            estadoAutorizacion: EstadoAutorizacionVenta.NoRequiere);
        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.ConfirmarVentaAsync(venta.Id);

        // Assert
        var cuotas = await db.Context.VentaCreditoCuotas
            .Where(c => c.VentaId == venta.Id)
            .OrderBy(c => c.NumeroCuota)
            .ToListAsync();

        Assert.Equal(cantidadCuotas, cuotas.Count);
        
        for (int i = 0; i < cantidadCuotas; i++)
        {
            Assert.Equal(i + 1, cuotas[i].NumeroCuota);
            Assert.Equal(montoCuota, cuotas[i].Monto);
            Assert.False(cuotas[i].Pagada);
            Assert.Equal(credito.Id, cuotas[i].CreditoId);
        }
    }

    #endregion
}
