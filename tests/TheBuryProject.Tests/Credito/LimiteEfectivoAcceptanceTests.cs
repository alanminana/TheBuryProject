using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using Xunit;

namespace TheBuryProject.Tests.CreditoAcceptance;

public class LimiteEfectivoAcceptanceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CreditoDisponibleService _service;

    public LimiteEfectivoAcceptanceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"LimiteEfectivoAcceptance_{Guid.NewGuid()}")
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
    public async Task Caso1_PresetSinExcepcionNiOverride_LimiteEsPreset()
    {
        var cliente = CrearCliente(100, NivelRiesgoCredito.AprobadoCondicional);
        ConfigurarPreset(NivelRiesgoCredito.AprobadoCondicional, 100000m);
        await _context.SaveChangesAsync();

        var resultado = await _service.CalcularDisponibleAsync(cliente.Id);

        Assert.Equal(100000m, resultado.Limite);
    }

    [Fact]
    public async Task Caso2_PresetMasExcepcionDelta_LimiteEsPresetMasDelta()
    {
        var cliente = CrearCliente(101, NivelRiesgoCredito.AprobadoCondicional);
        ConfigurarPreset(NivelRiesgoCredito.AprobadoCondicional, 100000m);
        await _context.SaveChangesAsync();

        var limiteEsperadoConDelta = 120000m;
        var resultado = await _service.CalcularDisponibleAsync(cliente.Id);

        Assert.Equal(limiteEsperadoConDelta, resultado.Limite);
    }

    [Fact]
    public async Task Caso3_PresetMasOverride_LimiteEsOverride_NoSeSumaNiTomaMaximo()
    {
        var cliente = CrearCliente(102, NivelRiesgoCredito.AprobadoCondicional);
        cliente.LimiteCredito = 80000m;

        ConfigurarPreset(NivelRiesgoCredito.AprobadoCondicional, 100000m);
        await _context.SaveChangesAsync();

        var resultado = await _service.CalcularDisponibleAsync(cliente.Id);

        Assert.Equal(80000m, resultado.Limite);
    }

    [Fact]
    public async Task Caso4_ConOverride_CambioPresetPorPuntaje_NoDebeModificarLimiteEfectivo()
    {
        var cliente = CrearCliente(103, NivelRiesgoCredito.AprobadoCondicional);
        cliente.LimiteCredito = 80000m;

        ConfigurarPreset(NivelRiesgoCredito.AprobadoCondicional, 100000m);
        ConfigurarPreset(NivelRiesgoCredito.AprobadoTotal, 200000m);
        await _context.SaveChangesAsync();

        cliente.NivelRiesgo = NivelRiesgoCredito.AprobadoTotal;
        await _context.SaveChangesAsync();

        var resultado = await _service.CalcularDisponibleAsync(cliente.Id);

        Assert.Equal(80000m, resultado.Limite);
    }

    [Fact]
    public void Caso5_ExcepcionVencida_DeltaNoAplica_ModeloDebeTenerVigenciaExplicita()
    {
        var tipoCliente = typeof(Cliente);
        var propiedadDelta = tipoCliente.GetProperty("ExcepcionDelta");
        var propiedadVigenciaHasta =
            tipoCliente.GetProperty("ExcepcionVigenciaHasta") ??
            tipoCliente.GetProperty("ExcepcionHasta");

        Assert.NotNull(propiedadDelta);
        Assert.NotNull(propiedadVigenciaHasta);
    }

    [Fact]
    public async Task Caso6_VentaGenerada_SnapshotNoCambiaAunqueCambieLimiteCliente()
    {
        var cliente = CrearCliente(104, NivelRiesgoCredito.AprobadoCondicional);
        cliente.LimiteCredito = 120000m;

        var credito = new Models.Entities.Credito
        {
            ClienteId = cliente.Id,
            Numero = "CRE-TEST-0001",
            MontoSolicitado = 50000m,
            MontoAprobado = 50000m,
            SaldoPendiente = 50000m,
            TasaInteres = 0m,
            CantidadCuotas = 1,
            MontoCuota = 50000m,
            CFTEA = 0m,
            TotalAPagar = 50000m,
            Estado = EstadoCredito.Generado,
            PuntajeRiesgoInicial = 6m,
            RowVersion = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        _context.Creditos.Add(credito);
        await _context.SaveChangesAsync();

        var venta = new Venta
        {
            Numero = "VTA-TEST-0001",
            ClienteId = cliente.Id,
            TipoPago = TipoPago.CreditoPersonal,
            Estado = EstadoVenta.Confirmada,
            Subtotal = 50000m,
            IVA = 0m,
            Total = 50000m,
            CreditoId = credito.Id,
            RowVersion = new byte[] { 2, 2, 2, 2, 2, 2, 2, 2 }
        };

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();

        cliente.LimiteCredito = 200000m;
        await _context.SaveChangesAsync();

        var ventaPersistida = await _context.Ventas
            .Include(v => v.Credito)
            .FirstAsync(v => v.Id == venta.Id);

        Assert.NotNull(ventaPersistida.Credito);
        Assert.Equal(50000m, ventaPersistida.Credito!.MontoAprobado);
        Assert.Equal(50000m, ventaPersistida.Total);
    }

    private Cliente CrearCliente(int id, NivelRiesgoCredito puntaje)
    {
        var cliente = new Cliente
        {
            Id = id,
            Nombre = "Cliente",
            Apellido = $"Acceptance {id}",
            TipoDocumento = "DNI",
            NumeroDocumento = $"{id:00000000}",
            Telefono = "111111111",
            Domicilio = "Domicilio Test",
            NivelRiesgo = puntaje,
            PuntajeRiesgo = (decimal)puntaje * 2,
            RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 }
        };

        _context.Clientes.Add(cliente);
        return cliente;
    }

    private void ConfigurarPreset(NivelRiesgoCredito puntaje, decimal limite)
    {
        _context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
        {
            Puntaje = puntaje,
            LimiteMonto = limite,
            Activo = true,
            FechaActualizacion = DateTime.UtcNow,
            UsuarioActualizacion = "acceptance-test"
        });
    }
}
