using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.Services.Validators;
using TheBuryProject.Tests.TestDoubles;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Ventas;

/// <summary>
/// Tests para E3: Autorización de ventas con crédito personal.
/// - Solo se puede autorizar si la venta está en PendienteAutorizacion.
/// - Motivo/observación es obligatorio.
/// - Registrar auditoría: quién, cuándo, razones autorizadas, observación.
/// - Tras autorizar: pasar venta a Autorizada (habilitar E4).
/// </summary>
public class VentaAutorizacionE3Tests
{
    #region Helper Methods

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

    private static async Task<(Cliente cliente, Producto producto, Venta venta)> SetupVentaPendienteAutorizacionAsync(SqliteInMemoryDb db)
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

        // Crear venta en estado PendienteAutorizacion con razones
        var razones = System.Text.Json.JsonSerializer.Serialize(new[]
        {
            new { Tipo = (int)TipoRazonAutorizacion.MoraActiva, Descripcion = "Cliente tiene mora activa de 15 días" },
            new { Tipo = (int)TipoRazonAutorizacion.ExcedeCupo, Descripcion = "Monto excede cupo disponible" }
        });

        var venta = new Venta
        {
            Numero = "VTA-AUTH-001",
            ClienteId = cliente.Id,
            FechaVenta = DateTime.UtcNow,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.CreditoPersonal,
            Subtotal = 1000,
            IVA = 0,
            Total = 1000,
            RequiereAutorizacion = true,
            EstadoAutorizacion = EstadoAutorizacionVenta.PendienteAutorizacion,
            RazonesAutorizacionJson = razones,
            Detalles = new List<VentaDetalle>
            {
                new()
                {
                    ProductoId = producto.Id,
                    Cantidad = 5,
                    PrecioUnitario = 200,
                    Subtotal = 1000
                }
            }
        };

        db.Context.Ventas.Add(venta);
        await db.Context.SaveChangesAsync();

        return (cliente, producto, venta);
    }

    #endregion

    #region Tests: Validación de estado

    [Fact]
    public async Task AutorizarVentaAsync_VentaEnPendienteAutorizacion_Exito()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);

        // Act
        var resultado = await ventaService.AutorizarVentaAsync(
            venta.Id, 
            "admin.test", 
            "Aprobado por excepción comercial");

        // Assert
        Assert.True(resultado);
        
        var ventaActualizada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.NotNull(ventaActualizada);
        Assert.Equal(EstadoAutorizacionVenta.Autorizada, ventaActualizada!.EstadoAutorizacion);
    }

    [Fact]
    public async Task AutorizarVentaAsync_VentaNoRequiereAutorizacion_Falla()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (cliente, producto, _) = await SetupVentaPendienteAutorizacionAsync(db);
        
        // Crear venta que NO requiere autorización
        var ventaSinAuth = new Venta
        {
            Numero = "VTA-NOAUTH-001",
            ClienteId = cliente.Id,
            FechaVenta = DateTime.UtcNow,
            Estado = EstadoVenta.Presupuesto,
            TipoPago = TipoPago.Efectivo,
            Subtotal = 500,
            Total = 500,
            RequiereAutorizacion = false,
            EstadoAutorizacion = EstadoAutorizacionVenta.NoRequiere,
            Detalles = new List<VentaDetalle>
            {
                new() { ProductoId = producto.Id, Cantidad = 2, PrecioUnitario = 200, Subtotal = 400 }
            }
        };
        db.Context.Ventas.Add(ventaSinAuth);
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ventaService.AutorizarVentaAsync(ventaSinAuth.Id, "admin", "Motivo"));

        Assert.Contains("PendienteAutorizacion", ex.Message);
    }

    [Fact]
    public async Task AutorizarVentaAsync_VentaYaAutorizada_Falla()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        
        // Cambiar estado a Autorizada
        venta.EstadoAutorizacion = EstadoAutorizacionVenta.Autorizada;
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ventaService.AutorizarVentaAsync(venta.Id, "admin", "Motivo"));

        Assert.Contains("PendienteAutorizacion", ex.Message);
    }

    [Fact]
    public async Task AutorizarVentaAsync_VentaRechazada_Falla()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        
        // Cambiar estado a Rechazada
        venta.EstadoAutorizacion = EstadoAutorizacionVenta.Rechazada;
        await db.Context.SaveChangesAsync();

        var ventaService = CreateVentaService(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ventaService.AutorizarVentaAsync(venta.Id, "admin", "Motivo"));

        Assert.Contains("PendienteAutorizacion", ex.Message);
    }

    #endregion

    #region Tests: Motivo obligatorio

    [Fact]
    public async Task AutorizarVentaAsync_SinMotivo_LanzaExcepcion()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => ventaService.AutorizarVentaAsync(venta.Id, "admin", ""));

        Assert.Contains("obligatorio", ex.Message.ToLower());
    }

    [Fact]
    public async Task AutorizarVentaAsync_MotivoNull_LanzaExcepcion()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => ventaService.AutorizarVentaAsync(venta.Id, "admin", null!));

        Assert.Contains("obligatorio", ex.Message.ToLower());
    }

    [Fact]
    public async Task AutorizarVentaAsync_MotivoSoloEspacios_LanzaExcepcion()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => ventaService.AutorizarVentaAsync(venta.Id, "admin", "   "));

        Assert.Contains("obligatorio", ex.Message.ToLower());
    }

    #endregion

    #region Tests: Auditoría

    [Fact]
    public async Task AutorizarVentaAsync_RegistraUsuarioAutorizador()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.AutorizarVentaAsync(venta.Id, "gerente.ventas", "Aprobado");

        // Assert
        var ventaActualizada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.Equal("gerente.ventas", ventaActualizada?.UsuarioAutoriza);
    }

    [Fact]
    public async Task AutorizarVentaAsync_RegistraFechaAutorizacion()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);
        var antesDeAutorizar = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await ventaService.AutorizarVentaAsync(venta.Id, "admin", "Aprobado");

        // Assert
        var ventaActualizada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.NotNull(ventaActualizada);
        Assert.NotNull(ventaActualizada!.FechaAutorizacion);
        Assert.True(ventaActualizada!.FechaAutorizacion!.Value >= antesDeAutorizar);
        Assert.True(ventaActualizada!.FechaAutorizacion.Value <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task AutorizarVentaAsync_RegistraMotivoAutorizacion()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);
        const string motivo = "Aprobado por excepción comercial - cliente histórico";

        // Act
        await ventaService.AutorizarVentaAsync(venta.Id, "admin", motivo);

        // Assert
        var ventaActualizada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.Equal(motivo, ventaActualizada?.MotivoAutorizacion);
    }

    [Fact]
    public async Task AutorizarVentaAsync_MotivoConEspacios_SeLimpia()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.AutorizarVentaAsync(venta.Id, "admin", "  Motivo con espacios  ");

        // Assert
        var ventaActualizada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.Equal("Motivo con espacios", ventaActualizada?.MotivoAutorizacion);
    }

    [Fact]
    public async Task AutorizarVentaAsync_PreservaRazonesOriginales()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var razonesOriginales = venta.RazonesAutorizacionJson;
        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.AutorizarVentaAsync(venta.Id, "admin", "Aprobado");

        // Assert - Las razones originales se preservan (auditoría de qué se autorizó)
        var ventaActualizada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.Equal(razonesOriginales, ventaActualizada?.RazonesAutorizacionJson);
        Assert.Contains("mora activa", ventaActualizada?.RazonesAutorizacionJson ?? "");
    }

    #endregion

    #region Tests: Estado post-autorización

    [Fact]
    public async Task AutorizarVentaAsync_CambiaEstadoAAutorizada()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.AutorizarVentaAsync(venta.Id, "admin", "Aprobado");

        // Assert
        var ventaActualizada = await db.Context.Ventas.FindAsync(venta.Id);
        Assert.Equal(EstadoAutorizacionVenta.Autorizada, ventaActualizada?.EstadoAutorizacion);
    }

    [Fact]
    public async Task AutorizarVentaAsync_NoCreaCredito()
    {
        // Arrange - E3 NO crea el crédito, eso es E4
        using var db = new SqliteInMemoryDb(userName: "admin");
        var (_, _, venta) = await SetupVentaPendienteAutorizacionAsync(db);
        var ventaService = CreateVentaService(db);

        // Act
        await ventaService.AutorizarVentaAsync(venta.Id, "admin", "Aprobado");

        // Assert - No debe haber CreditoId ni cuotas
        var ventaActualizada = await db.Context.Ventas
            .Include(v => v.VentaCreditoCuotas)
            .FirstOrDefaultAsync(v => v.Id == venta.Id);
        
        Assert.Null(ventaActualizada?.CreditoId);
        Assert.Empty(ventaActualizada?.VentaCreditoCuotas ?? new List<VentaCreditoCuota>());
    }

    [Fact]
    public async Task AutorizarVentaAsync_VentaInexistente_RetornaFalse()
    {
        // Arrange
        using var db = new SqliteInMemoryDb(userName: "admin");
        var ventaService = CreateVentaService(db);

        // Act
        var resultado = await ventaService.AutorizarVentaAsync(99999, "admin", "Motivo");

        // Assert
        Assert.False(resultado);
    }

    #endregion
}


