using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TheBuryProject.Controllers;
using TheBuryProject.Data;
using TheBuryProject.Helpers;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Creditos;

public class CreditoControllerConfigTasaTests
{
    [Fact]
    public async Task SimularPlanVenta_UsaTasaConfiguracion()
    {
        var creditoService = new Mock<ICreditoService>();
        var evaluacionService = new Mock<IEvaluacionCreditoService>();
        var financialService = new Mock<IFinancialCalculationService>();
        var configuracionPagoService = new Mock<IConfiguracionPagoService>();
        var contextFactory = new Mock<IDbContextFactory<AppDbContext>>();
        var loggerFactory = NullLoggerFactory.Instance;
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), loggerFactory).CreateMapper();
        var clienteLookup = new Mock<IClienteLookupService>();
        var productoService = new Mock<IProductoService>();

        configuracionPagoService
            .Setup(s => s.ObtenerTasaInteresMensualCreditoPersonalAsync())
            .ReturnsAsync(7.5m);

        financialService.Setup(s => s.ComputeFinancedAmount(It.IsAny<decimal>(), It.IsAny<decimal>()))
            .Returns(1000m);
        financialService.Setup(s => s.ComputePmt(It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .Returns(100m);
        financialService.Setup(s => s.CalcularInteresTotal(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>()))
            .Returns(200m);

        var mockConfiguracionMoraService2 = new Mock<IConfiguracionMoraService>();

        var controller = new CreditoController(
            creditoService.Object,
            evaluacionService.Object,
            financialService.Object,
            configuracionPagoService.Object,
            mockConfiguracionMoraService2.Object,
            contextFactory.Object,
            mapper,
            NullLogger<CreditoController>.Instance,
            clienteLookup.Object,
            productoService.Object);

        var result = await controller.SimularPlanVenta(1000m, 0m, 10, 0m, null, null);
        var json = Assert.IsType<JsonResult>(result);
        var payload = JsonSerializer.Serialize(json.Value);
        using var doc = JsonDocument.Parse(payload);

        Assert.Equal(7.5m, doc.RootElement.GetProperty("tasaAplicada").GetDecimal());
    }

    [Fact]
    public async Task ConfigurarVenta_UsaTasaConfiguracion_EnPost()
    {
        var creditoService = new Mock<ICreditoService>();
        var evaluacionService = new Mock<IEvaluacionCreditoService>();
        var financialService = new Mock<IFinancialCalculationService>();
        var configuracionPagoService = new Mock<IConfiguracionPagoService>();
        var contextFactory = new Mock<IDbContextFactory<AppDbContext>>();
        var loggerFactory = NullLoggerFactory.Instance;
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), loggerFactory).CreateMapper();
        var clienteLookup = new Mock<IClienteLookupService>();
        var productoService = new Mock<IProductoService>();

        configuracionPagoService
            .Setup(s => s.ObtenerTasaInteresMensualCreditoPersonalAsync())
            .ReturnsAsync(12.34m);

        var credito = new CreditoViewModel
        {
            Id = 1,
            ClienteId = 1,
            TasaInteres = 0m
        };

        creditoService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(credito);

        CreditoViewModel? actualizado = null;
        creditoService.Setup(s => s.UpdateAsync(It.IsAny<CreditoViewModel>()))
            .Callback<CreditoViewModel>(vm => actualizado = vm)
            .ReturnsAsync(true);

        var mockConfiguracionMoraService2 = new Mock<IConfiguracionMoraService>();

        var controller = new CreditoController(
            creditoService.Object,
            evaluacionService.Object,
            financialService.Object,
            configuracionPagoService.Object,
            mockConfiguracionMoraService2.Object,
            contextFactory.Object,
            mapper,
            NullLogger<CreditoController>.Instance,
            clienteLookup.Object,
            productoService.Object);

        var viewModel = new ConfiguracionCreditoVentaViewModel
        {
            CreditoId = 1,
            Monto = 1000m,
            CantidadCuotas = 12,
            TasaMensual = 99m,
            FechaPrimeraCuota = DateTime.Today.AddDays(30)
        };

        var result = await controller.ConfigurarVenta(viewModel, null);

        Assert.NotNull(result);
        Assert.NotNull(actualizado);
        Assert.Equal(12.34m, actualizado!.TasaInteres);
    }
}
