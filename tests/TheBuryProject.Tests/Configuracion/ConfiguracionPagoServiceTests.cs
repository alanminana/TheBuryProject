using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Configuracion;

public class ConfiguracionPagoServiceTests
{
    [Fact]
    public async Task ObtenerTasaInteresMensualCreditoPersonalAsync_CreaConfiguracionSiNoExiste()
    {
        using var db = new SqliteInMemoryDb("tester");
        var loggerFactory = NullLoggerFactory.Instance;
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), loggerFactory).CreateMapper();
        var service = new ConfiguracionPagoService(db.Context, mapper, NullLogger<ConfiguracionPagoService>.Instance);

        var tasa = await service.ObtenerTasaInteresMensualCreditoPersonalAsync();

        Assert.Equal(0m, tasa);

        var configuracion = await db.Context.ConfiguracionesPago
            .FirstOrDefaultAsync(c => c.TipoPago == TipoPago.CreditoPersonal);

        Assert.NotNull(configuracion);
        Assert.Equal(0m, configuracion!.TasaInteresMensualCreditoPersonal);
    }

    [Fact]
    public async Task ObtenerTasaInteresMensualCreditoPersonalAsync_DevuelveValorConfigurado()
    {
        using var db = new SqliteInMemoryDb("tester");
        var loggerFactory = NullLoggerFactory.Instance;
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), loggerFactory).CreateMapper();
        var service = new ConfiguracionPagoService(db.Context, mapper, NullLogger<ConfiguracionPagoService>.Instance);

        db.Context.ConfiguracionesPago.Add(new Models.Entities.ConfiguracionPago
        {
            TipoPago = TipoPago.CreditoPersonal,
            Nombre = TipoPago.CreditoPersonal.ToString(),
            Activo = true,
            TasaInteresMensualCreditoPersonal = 7.5m
        });
        await db.Context.SaveChangesAsync();

        var tasa = await service.ObtenerTasaInteresMensualCreditoPersonalAsync();

        Assert.Equal(7.5m, tasa);
    }
}
