using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TheBuryProject.Controllers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Ventas;

public class VentaControllerIndexTests
{
    [Fact]
    public async Task Index_con_caja_abierta_habilita_crear_venta()
    {
        var controller = CreateController(aperturaActiva: new AperturaCaja());

        var result = await controller.Index(new VentaFilterViewModel());

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True((bool?)viewResult.ViewData["PuedeCrearVenta"]);
    }

    [Fact]
    public async Task Index_sin_caja_abierta_deshabilita_crear_venta()
    {
        var controller = CreateController(aperturaActiva: null);

        var result = await controller.Index(new VentaFilterViewModel());

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False((bool?)viewResult.ViewData["PuedeCrearVenta"]);
    }

    private static VentaController CreateController(AperturaCaja? aperturaActiva)
    {
        var ventaService = new Mock<IVentaService>();
        ventaService.Setup(s => s.GetAllAsync(It.IsAny<VentaFilterViewModel>()))
            .ReturnsAsync(new List<VentaViewModel>());

        var clienteLookup = new Mock<IClienteLookupService>();
        clienteLookup.Setup(s => s.GetClientesSelectListAsync(It.IsAny<int?>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SelectListItem>());

        var cajaService = new Mock<ICajaService>();
        cajaService.Setup(s => s.ObtenerAperturaActivaParaUsuarioAsync(It.IsAny<string>()))
            .ReturnsAsync(aperturaActiva);

        var controller = new VentaController(
            ventaService.Object,
            new Mock<IConfiguracionPagoService>().Object,
            NullLogger<VentaController>.Instance,
            new Mock<IFinancialCalculationService>().Object,
            new Mock<IPrequalificationService>().Object,
            new Mock<IDocumentoClienteService>().Object,
            new Mock<ICreditoService>().Object,
            new Mock<IDocumentacionService>().Object,
            new Mock<IClienteService>().Object,
            new Mock<IProductoService>().Object,
            clienteLookup.Object,
            new Mock<IValidacionVentaService>().Object,
            CreateUserManager().Object,
            cajaService.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.Name, "tester"),
                            new Claim(ClaimTypes.NameIdentifier, "tester-id")
                        },
                        authenticationType: "TestAuth"))
            }
        };

        return controller;
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}
