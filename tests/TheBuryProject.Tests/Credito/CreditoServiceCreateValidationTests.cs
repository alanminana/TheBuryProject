using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TheBuryProject.Data;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Exceptions;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;
using Xunit;
using CreditoEntity = TheBuryProject.Models.Entities.Credito;

namespace TheBuryProject.Tests.CreditoServiceTests;

public class CreditoServiceCreateValidationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CreditoService _service;

    public CreditoServiceCreateValidationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreditoCreate_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.SavingChanges += (_, _) =>
        {
            foreach (var entry in _context.ChangeTracker.Entries()
                         .Where(e => e.State == EntityState.Added &&
                                     e.Properties.Any(p => p.Metadata.Name == nameof(Cliente.RowVersion))))
            {
                var rowVersionProp = entry.Properties.First(p => p.Metadata.Name == nameof(Cliente.RowVersion));
                if (rowVersionProp.CurrentValue is not byte[] bytes || bytes.Length == 0)
                {
                    rowVersionProp.CurrentValue = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
                }
            }
        };

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance)
            .CreateMapper();

        var financialService = new Mock<IFinancialCalculationService>();
        var cajaService = new Mock<ICajaService>();
        var creditoDisponibleService = new CreditoDisponibleService(_context);

        _service = new CreditoService(
            _context,
            mapper,
            NullLogger<CreditoService>.Instance,
            financialService.Object,
            cajaService.Object,
            creditoDisponibleService);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateAsync_MontoSolicitadoExcedeDisponible_LanzaErrorFuncional()
    {
        var cliente = CrearCliente(1, NivelRiesgoCredito.AprobadoCondicional);

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoCondicional,
            LimiteMonto = 100000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        _context.Creditos.Add(new CreditoEntity
        {
            ClienteId = cliente.Id,
            Estado = EstadoCredito.Activo,
            SaldoPendiente = 90000m,
            Numero = "CR-EXIST-001",
            MontoSolicitado = 90000m,
            MontoAprobado = 90000m,
            TasaInteres = 0m,
            CantidadCuotas = 1,
            MontoCuota = 90000m,
            CFTEA = 0m,
            TotalAPagar = 90000m,
            PuntajeRiesgoInicial = cliente.PuntajeRiesgo,
            RowVersion = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 }
        });

        await _context.SaveChangesAsync();

        var solicitud = new CreditoViewModel
        {
            ClienteId = cliente.Id,
            MontoSolicitado = 20000m,
            CantidadCuotas = 12,
            TasaInteres = 5m
        };

        var ex = await Assert.ThrowsAsync<CreditoDisponibleException>(() => _service.CreateAsync(solicitud));

        Assert.Contains("Excede el crédito disponible por puntaje", ex.Message);
        Assert.Contains("Disponible:", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_MontoSolicitadoDentroDelDisponible_CreaCredito()
    {
        var cliente = CrearCliente(2, NivelRiesgoCredito.AprobadoTotal);

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoTotal,
            LimiteMonto = 150000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        await _context.SaveChangesAsync();

        var solicitud = new CreditoViewModel
        {
            ClienteId = cliente.Id,
            MontoSolicitado = 50000m,
            CantidadCuotas = 10,
            TasaInteres = 4m
        };

        var creado = await _service.CreateAsync(solicitud);

        Assert.True(creado.Id > 0);
        Assert.False(string.IsNullOrWhiteSpace(creado.Numero));
        Assert.Equal(50000m, creado.MontoAprobado);

        var persistido = await _context.Creditos.FirstOrDefaultAsync(c => c.Id == creado.Id);
        Assert.NotNull(persistido);
        Assert.Equal(50000m, persistido!.SaldoPendiente);
    }

    [Fact]
    public async Task RecalcularSaldoCreditoAsync_ConCuotasConInteres_UsaCapitalPendienteParaCupo()
    {
        var cliente = CrearCliente(3, NivelRiesgoCredito.AprobadoTotal);

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoTotal,
            LimiteMonto = 100000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        var credito = new CreditoEntity
        {
            ClienteId = cliente.Id,
            Numero = "CR-CUPO-001",
            Estado = EstadoCredito.Activo,
            MontoSolicitado = 100000m,
            MontoAprobado = 100000m,
            TotalAPagar = 120000m,
            SaldoPendiente = 120000m,
            TasaInteres = 5m,
            CantidadCuotas = 2,
            MontoCuota = 60000m,
            PuntajeRiesgoInicial = cliente.PuntajeRiesgo,
            RowVersion = new byte[] { 1, 3, 5, 7, 9, 2, 4, 6 },
            Cuotas = new List<Cuota>
            {
                new()
                {
                    NumeroCuota = 1,
                    MontoCapital = 50000m,
                    MontoInteres = 10000m,
                    MontoTotal = 60000m,
                    FechaVencimiento = DateTime.UtcNow.AddMonths(1),
                    Estado = EstadoCuota.Pendiente,
                    MontoPagado = 0m
                },
                new()
                {
                    NumeroCuota = 2,
                    MontoCapital = 50000m,
                    MontoInteres = 10000m,
                    MontoTotal = 60000m,
                    FechaVencimiento = DateTime.UtcNow.AddMonths(2),
                    Estado = EstadoCuota.Pendiente,
                    MontoPagado = 0m
                }
            }
        };

        _context.Creditos.Add(credito);
        await _context.SaveChangesAsync();

        var recalculado = await _service.RecalcularSaldoCreditoAsync(credito.Id);
        Assert.True(recalculado);

        var creditoActualizado = await _context.Creditos.FirstAsync(c => c.Id == credito.Id);
        Assert.Equal(100000m, creditoActualizado.SaldoPendiente);

        var disponibleService = new CreditoDisponibleService(_context);
        var disponible = await disponibleService.CalcularDisponibleAsync(cliente.Id);
        Assert.Equal(0m, disponible.Disponible);
    }

    [Fact]
    public async Task RecalcularSaldoCreditoAsync_PagoParcial_DejaDisponibleProgresivo()
    {
        var cliente = CrearCliente(4, NivelRiesgoCredito.AprobadoTotal);

        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = NivelRiesgoCredito.AprobadoTotal,
            LimiteMonto = 100000m,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "test"
        });

        var credito = new CreditoEntity
        {
            ClienteId = cliente.Id,
            Numero = "CR-CUPO-002",
            Estado = EstadoCredito.Activo,
            MontoSolicitado = 100000m,
            MontoAprobado = 100000m,
            TotalAPagar = 120000m,
            SaldoPendiente = 100000m,
            TasaInteres = 5m,
            CantidadCuotas = 2,
            MontoCuota = 60000m,
            PuntajeRiesgoInicial = cliente.PuntajeRiesgo,
            RowVersion = new byte[] { 6, 4, 2, 9, 7, 5, 3, 1 },
            Cuotas = new List<Cuota>
            {
                new()
                {
                    NumeroCuota = 1,
                    MontoCapital = 50000m,
                    MontoInteres = 10000m,
                    MontoTotal = 60000m,
                    FechaVencimiento = DateTime.UtcNow.AddMonths(1),
                    Estado = EstadoCuota.Pendiente,
                    MontoPagado = 30000m
                },
                new()
                {
                    NumeroCuota = 2,
                    MontoCapital = 50000m,
                    MontoInteres = 10000m,
                    MontoTotal = 60000m,
                    FechaVencimiento = DateTime.UtcNow.AddMonths(2),
                    Estado = EstadoCuota.Pendiente,
                    MontoPagado = 0m
                }
            }
        };

        _context.Creditos.Add(credito);
        await _context.SaveChangesAsync();

        var recalculado = await _service.RecalcularSaldoCreditoAsync(credito.Id);
        Assert.True(recalculado);

        var creditoActualizado = await _context.Creditos.FirstAsync(c => c.Id == credito.Id);
        Assert.Equal(75000m, creditoActualizado.SaldoPendiente);

        var disponibleService = new CreditoDisponibleService(_context);
        var disponible = await disponibleService.CalcularDisponibleAsync(cliente.Id);
        Assert.Equal(25000m, disponible.Disponible);
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
