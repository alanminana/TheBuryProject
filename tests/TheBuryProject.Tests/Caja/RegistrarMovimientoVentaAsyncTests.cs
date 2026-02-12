using System;
using AutoMapper;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Data;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestDoubles;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.CajaTests;

public class RegistrarMovimientoVentaAsyncTests
{
    [Fact]
    public async Task RegistrarMovimientoVentaAsync_AperturaExiste_CerradaTrue_PermiteMovimiento()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var apertura = await db.CrearAperturaCajaActivaAsync();
        apertura.Cerrada = true;
        await db.Context.SaveChangesAsync();

        var cliente = CrearCliente(db.Context);
        var venta = CrearVenta(db.Context, cliente, apertura.Id, vendedorUserId: null);

        var cajaService = CreateCajaService(db.Context);

        var movimiento = await cajaService.RegistrarMovimientoVentaAsync(
            venta.Id,
            venta.Numero,
            venta.Total,
            venta.TipoPago,
            usuario: "tester");

        Assert.NotNull(movimiento);
        Assert.Equal(apertura.Id, movimiento!.AperturaCajaId);
    }

    [Fact]
    public async Task RegistrarMovimientoVentaAsync_AperturaNoExiste_LanzaError()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var cliente = CrearCliente(db.Context);
        var venta = CrearVenta(db.Context, cliente, aperturaCajaId: 9999, vendedorUserId: null);

        var cajaService = CreateCajaService(db.Context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cajaService.RegistrarMovimientoVentaAsync(
                venta.Id,
                venta.Numero,
                venta.Total,
                venta.TipoPago,
                usuario: "tester"));

        Assert.Equal("Apertura asociada a la venta no encontrada o eliminada.", ex.Message);
    }

    [Fact]
    public async Task RegistrarMovimientoVentaAsync_AperturaIsDeleted_LanzaError()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var apertura = await db.CrearAperturaCajaActivaAsync();
        apertura.IsDeleted = true;
        await db.Context.SaveChangesAsync();

        var cliente = CrearCliente(db.Context);
        var venta = CrearVenta(db.Context, cliente, apertura.Id, vendedorUserId: null);

        var cajaService = CreateCajaService(db.Context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cajaService.RegistrarMovimientoVentaAsync(
                venta.Id,
                venta.Numero,
                venta.Total,
                venta.TipoPago,
                usuario: "tester"));

        Assert.Equal("Apertura asociada a la venta no encontrada o eliminada.", ex.Message);
    }

    [Fact]
    public async Task RegistrarMovimientoVentaAsync_SinApertura_ConVendedorUserId_LanzaError()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var cliente = CrearCliente(db.Context);
        var vendedorUserId = db.Context.Users.First().Id;
        var venta = CrearVenta(db.Context, cliente, aperturaCajaId: null, vendedorUserId);

        var cajaService = CreateCajaService(db.Context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cajaService.RegistrarMovimientoVentaAsync(
                venta.Id,
                venta.Numero,
                venta.Total,
                venta.TipoPago,
                usuario: "tester"));

        Assert.Equal("No hay apertura activa para el vendedor de la venta.", ex.Message);
    }

    [Fact]
    public async Task RegistrarMovimientoVentaAsync_SinApertura_SinVendedorUserId_LanzaNoTrazable()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        _ = await db.CrearAperturaCajaActivaAsync();

        var cliente = CrearCliente(db.Context);
        var venta = CrearVenta(db.Context, cliente, aperturaCajaId: null, vendedorUserId: null);

        var cajaService = CreateCajaService(db.Context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cajaService.RegistrarMovimientoVentaAsync(
                venta.Id,
                venta.Numero,
                venta.Total,
                venta.TipoPago,
                usuario: "tester"));

        Assert.Equal("Venta no trazable: falta apertura y vendedor.", ex.Message);
    }

    private static CajaService CreateCajaService(AppDbContext context)
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        return new CajaService(
            context,
            mapper,
            NullLogger<CajaService>.Instance,
            new NoopNotificacionService());
    }

    private static Cliente CrearCliente(AppDbContext context)
    {
        var cliente = new Cliente
        {
            TipoDocumento = "DNI",
            NumeroDocumento = Guid.NewGuid().ToString("N")[..8],
            Apellido = "Test",
            Nombre = "Cliente",
            Telefono = "123456",
            Domicilio = "Calle Falsa 123"
        };

        context.Clientes.Add(cliente);
        context.SaveChanges();

        return cliente;
    }

    private static Venta CrearVenta(
        AppDbContext context,
        Cliente cliente,
        int? aperturaCajaId,
        string? vendedorUserId)
    {
        var venta = new Venta
        {
            Numero = $"V{Guid.NewGuid():N}"[..20],
            ClienteId = cliente.Id,
            FechaVenta = DateTime.UtcNow,
            Estado = EstadoVenta.Cotizacion,
            TipoPago = TipoPago.Efectivo,
            Subtotal = 100m,
            IVA = 21m,
            Total = 121m,
            AperturaCajaId = aperturaCajaId,
            VendedorUserId = vendedorUserId,
            VendedorNombre = "Tester"
        };

        context.Ventas.Add(venta);
        context.SaveChanges();

        return venta;
    }
}
