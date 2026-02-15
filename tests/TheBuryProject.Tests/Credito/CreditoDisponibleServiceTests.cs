using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Exceptions;
using Xunit;
using CreditoEntity = TheBuryProject.Models.Entities.Credito;

namespace TheBuryProject.Tests.CreditoDisponibleTests;

public class CreditoDisponibleServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CreditoDisponibleService _service;

    public CreditoDisponibleServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreditoDisponible_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _service = new CreditoDisponibleService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CalcularDisponibleAsync_PuntajeConLimiteYSaldoCero_DisponibleEsIgualALimite()
    {
        var cliente = CrearCliente(1, NivelRiesgoCredito.AprobadoCondicional);

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoCondicional,
            LimiteMonto = 120000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        await _context.SaveChangesAsync();

        var resultado = await _service.CalcularDisponibleAsync(cliente.Id);

        Assert.Equal(120000m, resultado.Limite);
        Assert.Equal(0m, resultado.SaldoVigente);
        Assert.Equal(120000m, resultado.Disponible);
    }

    [Fact]
    public async Task CalcularDisponibleAsync_PuntajeConLimiteYSaldoMayorACero_DisponibleCorrecto()
    {
        var cliente = CrearCliente(2, NivelRiesgoCredito.AprobadoTotal);

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoTotal,
            LimiteMonto = 200000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        _context.Creditos.AddRange(
            new CreditoEntity
            {
                ClienteId = cliente.Id,
                Estado = EstadoCredito.Activo,
                SaldoPendiente = 35000m,
                Numero = "CR-001",
                MontoSolicitado = 35000m,
                MontoAprobado = 35000m,
                TasaInteres = 0m,
                CantidadCuotas = 1,
                MontoCuota = 35000m,
                CFTEA = 0m,
                TotalAPagar = 35000m,
                PuntajeRiesgoInicial = 10m,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            },
            new CreditoEntity
            {
                ClienteId = cliente.Id,
                Estado = EstadoCredito.Generado,
                SaldoPendiente = 15000m,
                Numero = "CR-002",
                MontoSolicitado = 15000m,
                MontoAprobado = 15000m,
                TasaInteres = 0m,
                CantidadCuotas = 1,
                MontoCuota = 15000m,
                CFTEA = 0m,
                TotalAPagar = 15000m,
                PuntajeRiesgoInicial = 10m,
                RowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }
            },
            new CreditoEntity
            {
                ClienteId = cliente.Id,
                Estado = EstadoCredito.Finalizado,
                SaldoPendiente = 9999m,
                Numero = "CR-003",
                MontoSolicitado = 9999m,
                MontoAprobado = 9999m,
                TasaInteres = 0m,
                CantidadCuotas = 1,
                MontoCuota = 9999m,
                CFTEA = 0m,
                TotalAPagar = 9999m,
                PuntajeRiesgoInicial = 10m,
                RowVersion = new byte[] { 2, 2, 2, 2, 2, 2, 2, 2 }
            });

        await _context.SaveChangesAsync();

        var resultado = await _service.CalcularDisponibleAsync(cliente.Id);

        Assert.Equal(200000m, resultado.Limite);
        Assert.Equal(50000m, resultado.SaldoVigente);
        Assert.Equal(150000m, resultado.Disponible);
    }

    [Fact]
    public async Task CalcularDisponibleAsync_PuntajeSinLimiteConfigurado_LanzaErrorFuncional()
    {
        var cliente = CrearCliente(3, NivelRiesgoCredito.AprobadoLimitado);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<CreditoDisponibleException>(() =>
            _service.CalcularDisponibleAsync(cliente.Id));

        Assert.Contains("No existe límite de crédito configurado", ex.Message);
    }

    [Fact]
    public async Task CalcularDisponibleAsync_Limite8000_Saldo3000_Disponible5000()
    {
        var cliente = CrearCliente(4, NivelRiesgoCredito.Rechazado);

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.Rechazado,
            LimiteMonto = 8000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        _context.Creditos.Add(new CreditoEntity
        {
            ClienteId = cliente.Id,
            Estado = EstadoCredito.Activo,
            SaldoPendiente = 3000m,
            Numero = "CR-004",
            MontoSolicitado = 3000m,
            MontoAprobado = 3000m,
            TasaInteres = 0m,
            CantidadCuotas = 1,
            MontoCuota = 3000m,
            CFTEA = 0m,
            TotalAPagar = 3000m,
            PuntajeRiesgoInicial = 4m,
            RowVersion = new byte[] { 4, 4, 4, 4, 4, 4, 4, 4 }
        });

        await _context.SaveChangesAsync();

        var resultado = await _service.CalcularDisponibleAsync(cliente.Id);

        Assert.Equal(8000m, resultado.Limite);
        Assert.Equal(3000m, resultado.SaldoVigente);
        Assert.Equal(5000m, resultado.Disponible);
    }

    [Fact]
    public async Task CalcularDisponibleAsync_ActualizarLimite_ReflejaNuevoDisponible()
    {
        var cliente = CrearCliente(5, NivelRiesgoCredito.AprobadoLimitado);

        var config = new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoLimitado,
            LimiteMonto = 8000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        };

        _context.PuntajesCreditoLimite.Add(config);
        _context.Creditos.Add(new CreditoEntity
        {
            ClienteId = cliente.Id,
            Estado = EstadoCredito.Activo,
            SaldoPendiente = 3000m,
            Numero = "CR-005",
            MontoSolicitado = 3000m,
            MontoAprobado = 3000m,
            TasaInteres = 0m,
            CantidadCuotas = 1,
            MontoCuota = 3000m,
            CFTEA = 0m,
            TotalAPagar = 3000m,
            PuntajeRiesgoInicial = 6m,
            RowVersion = new byte[] { 5, 5, 5, 5, 5, 5, 5, 5 }
        });

        await _context.SaveChangesAsync();

        var disponibleInicial = await _service.CalcularDisponibleAsync(cliente.Id);
        Assert.Equal(5000m, disponibleInicial.Disponible);

        config.LimiteMonto = 10000m;
        config.FechaActualizacion = DateTime.UtcNow;
        config.UsuarioActualizacion = "admin@test";
        await _context.SaveChangesAsync();

        var disponibleActualizado = await _service.CalcularDisponibleAsync(cliente.Id);
        Assert.Equal(7000m, disponibleActualizado.Disponible);
    }

    [Fact]
    public async Task CalcularDisponibleAsync_LimiteInactivo_LanzaErrorFuncional()
    {
        var cliente = CrearCliente(6, NivelRiesgoCredito.AprobadoTotal);

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoTotal,
            LimiteMonto = 15000m,
            Activo = false,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<CreditoDisponibleException>(() =>
            _service.CalcularDisponibleAsync(cliente.Id));

        Assert.Contains("No existe límite de crédito configurado", ex.Message);
    }

    [Fact]
    public async Task CalcularDisponibleAsync_LimiteIndividualMayorAlPuntaje_UsaLimiteIndividual()
    {
        var cliente = CrearCliente(7, NivelRiesgoCredito.AprobadoCondicional);
        cliente.LimiteCredito = 180000m;

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoCondicional,
            LimiteMonto = 100000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        await _context.SaveChangesAsync();

        var resultado = await _service.CalcularDisponibleAsync(cliente.Id);

        Assert.Equal(180000m, resultado.Limite);
        Assert.Equal(180000m, resultado.Disponible);
    }

    [Fact]
    public async Task CalcularDisponibleAsync_MontoMaximoPersonalizadoMayor_UsaMontoMaximoPersonalizado()
    {
        var cliente = CrearCliente(8, NivelRiesgoCredito.AprobadoLimitado);
        cliente.LimiteCredito = 150000m;
        cliente.MontoMaximoPersonalizado = 220000m;

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoLimitado,
            LimiteMonto = 90000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        await _context.SaveChangesAsync();

        var resultado = await _service.CalcularDisponibleAsync(cliente.Id);

        Assert.Equal(220000m, resultado.Limite);
        Assert.Equal(220000m, resultado.Disponible);
    }

    private Cliente CrearCliente(int id, NivelRiesgoCredito puntaje)
    {
        var cliente = new Cliente
        {
            Id = id,
            Nombre = "Cliente",
            Apellido = $"Test {id}",
            TipoDocumento = "DNI",
            NumeroDocumento = $"DOC{id:00000000}",
            Telefono = "111111111",
            Domicilio = "Domicilio Test",
            NivelRiesgo = puntaje,
            PuntajeRiesgo = (decimal)puntaje * 2,
            RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 }
        };

        _context.Clientes.Add(cliente);
        return cliente;
    }
}
