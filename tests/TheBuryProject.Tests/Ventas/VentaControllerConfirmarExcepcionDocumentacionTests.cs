using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TheBuryProject.Controllers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Ventas;

public class VentaControllerConfirmarExcepcionDocumentacionTests
{
    [Fact]
    public async Task Confirmar_ConPermisoAutorizar_YSoloDocumentacionFaltante_YFlagExcepcion_AplicaExcepcionYConfirma()
    {
        var ventaService = new Mock<IVentaService>();
        var creditoService = new Mock<ICreditoService>();
        var validacionService = new Mock<IValidacionVentaService>();

        var venta = new VentaViewModel
        {
            Id = 10,
            TipoPago = TipoPago.CreditoPersonal,
            Estado = EstadoVenta.Presupuesto,
            CreditoId = 50
        };

        var credito = new CreditoViewModel
        {
            Id = 50,
            Estado = EstadoCredito.Configurado
        };

        ventaService.Setup(v => v.GetByIdAsync(10)).ReturnsAsync(venta);
        creditoService.Setup(c => c.GetByIdAsync(50)).ReturnsAsync(credito);
        validacionService.Setup(v => v.ValidarConfirmacionVentaAsync(10)).ReturnsAsync(new ValidacionVentaResult
        {
            NoViable = true,
            PendienteRequisitos = true,
            RequisitosPendientes = new List<RequisitoPendiente>
            {
                new() { Tipo = TipoRequisitoPendiente.DocumentacionFaltante, Descripcion = "Falta DNI" }
            }
        });
        ventaService.Setup(v => v.RegistrarExcepcionDocumentalAsync(10, "tester", "Supervisor valida excepción por contingencia"))
            .ReturnsAsync(true);
        ventaService.Setup(v => v.ConfirmarVentaCreditoAsync(10)).ReturnsAsync(true);

        var controller = CreateController(
            ventaService.Object,
            creditoService.Object,
            validacionService.Object,
            includeAuthorizePermission: true);

        var result = await controller.Confirmar(
            10,
            aplicarExcepcionDocumental: true,
            motivoExcepcionDocumental: "Supervisor valida excepción por contingencia");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(10, redirect.RouteValues?["id"]);
        ventaService.Verify(v => v.RegistrarExcepcionDocumentalAsync(10, "tester", "Supervisor valida excepción por contingencia"), Times.Once);
        ventaService.Verify(v => v.ConfirmarVentaCreditoAsync(10), Times.Once);
        Assert.NotNull(controller.TempData["Warning"]);
    }

    [Fact]
    public async Task Confirmar_SinPermisoAutorizar_YDocumentacionFaltante_BloqueaConfirmacion()
    {
        var ventaService = new Mock<IVentaService>();
        var creditoService = new Mock<ICreditoService>();
        var validacionService = new Mock<IValidacionVentaService>();

        var venta = new VentaViewModel
        {
            Id = 11,
            TipoPago = TipoPago.CreditoPersonal,
            Estado = EstadoVenta.Presupuesto,
            CreditoId = 51
        };

        var credito = new CreditoViewModel
        {
            Id = 51,
            Estado = EstadoCredito.Configurado
        };

        ventaService.Setup(v => v.GetByIdAsync(11)).ReturnsAsync(venta);
        creditoService.Setup(c => c.GetByIdAsync(51)).ReturnsAsync(credito);
        validacionService.Setup(v => v.ValidarConfirmacionVentaAsync(11)).ReturnsAsync(new ValidacionVentaResult
        {
            NoViable = true,
            PendienteRequisitos = true,
            RequisitosPendientes = new List<RequisitoPendiente>
            {
                new() { Tipo = TipoRequisitoPendiente.DocumentacionFaltante, Descripcion = "Falta DNI" }
            }
        });

        var controller = CreateController(
            ventaService.Object,
            creditoService.Object,
            validacionService.Object,
            includeAuthorizePermission: false);

        var result = await controller.Confirmar(11);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(11, redirect.RouteValues?["id"]);
        ventaService.Verify(v => v.RegistrarExcepcionDocumentalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ventaService.Verify(v => v.ConfirmarVentaCreditoAsync(11), Times.Never);
        Assert.NotNull(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Confirmar_ConPermisoAutorizar_YDocumentacionFaltante_SinFlagExcepcion_BloqueaConfirmacion()
    {
        var ventaService = new Mock<IVentaService>();
        var creditoService = new Mock<ICreditoService>();
        var validacionService = new Mock<IValidacionVentaService>();

        var venta = new VentaViewModel
        {
            Id = 12,
            TipoPago = TipoPago.CreditoPersonal,
            Estado = EstadoVenta.Presupuesto,
            CreditoId = 52
        };

        var credito = new CreditoViewModel
        {
            Id = 52,
            Estado = EstadoCredito.Configurado
        };

        ventaService.Setup(v => v.GetByIdAsync(12)).ReturnsAsync(venta);
        creditoService.Setup(c => c.GetByIdAsync(52)).ReturnsAsync(credito);
        validacionService.Setup(v => v.ValidarConfirmacionVentaAsync(12)).ReturnsAsync(new ValidacionVentaResult
        {
            NoViable = true,
            PendienteRequisitos = true,
            RequisitosPendientes = new List<RequisitoPendiente>
            {
                new() { Tipo = TipoRequisitoPendiente.DocumentacionFaltante, Descripcion = "Falta DNI" }
            }
        });

        var controller = CreateController(
            ventaService.Object,
            creditoService.Object,
            validacionService.Object,
            includeAuthorizePermission: true);

        var result = await controller.Confirmar(12);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(12, redirect.RouteValues?["id"]);
        ventaService.Verify(v => v.RegistrarExcepcionDocumentalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ventaService.Verify(v => v.ConfirmarVentaCreditoAsync(12), Times.Never);
        Assert.NotNull(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Confirmar_ConPermisoYFlagExcepcion_SinMotivo_BloqueaYNoConfirma()
    {
        var ventaService = new Mock<IVentaService>();
        var creditoService = new Mock<ICreditoService>();
        var validacionService = new Mock<IValidacionVentaService>();

        var venta = new VentaViewModel
        {
            Id = 13,
            TipoPago = TipoPago.CreditoPersonal,
            Estado = EstadoVenta.Presupuesto,
            CreditoId = 53
        };

        var credito = new CreditoViewModel
        {
            Id = 53,
            Estado = EstadoCredito.Configurado
        };

        ventaService.Setup(v => v.GetByIdAsync(13)).ReturnsAsync(venta);
        creditoService.Setup(c => c.GetByIdAsync(53)).ReturnsAsync(credito);
        validacionService.Setup(v => v.ValidarConfirmacionVentaAsync(13)).ReturnsAsync(new ValidacionVentaResult
        {
            NoViable = true,
            PendienteRequisitos = true,
            RequisitosPendientes = new List<RequisitoPendiente>
            {
                new() { Tipo = TipoRequisitoPendiente.DocumentacionFaltante, Descripcion = "Falta recibo" }
            }
        });

        var controller = CreateController(
            ventaService.Object,
            creditoService.Object,
            validacionService.Object,
            includeAuthorizePermission: true);

        var result = await controller.Confirmar(
            13,
            aplicarExcepcionDocumental: true,
            motivoExcepcionDocumental: "   ");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(13, redirect.RouteValues?["id"]);
        ventaService.Verify(v => v.RegistrarExcepcionDocumentalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ventaService.Verify(v => v.ConfirmarVentaCreditoAsync(13), Times.Never);
        Assert.NotNull(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Confirmar_ConExcepcionDocumentalRegistrada_Previamente_ConfirmaSinFlag()
    {
        var ventaService = new Mock<IVentaService>();
        var creditoService = new Mock<ICreditoService>();
        var validacionService = new Mock<IValidacionVentaService>();

        var venta = new VentaViewModel
        {
            Id = 14,
            TipoPago = TipoPago.CreditoPersonal,
            Estado = EstadoVenta.Presupuesto,
            CreditoId = 54,
            MotivoAutorizacion = "EXCEPCION_DOC|2026-02-14T16:37:00.0000000Z|admin@thebury.com|ghj"
        };

        var credito = new CreditoViewModel
        {
            Id = 54,
            Estado = EstadoCredito.Configurado
        };

        ventaService.Setup(v => v.GetByIdAsync(14)).ReturnsAsync(venta);
        creditoService.Setup(c => c.GetByIdAsync(54)).ReturnsAsync(credito);
        validacionService.Setup(v => v.ValidarConfirmacionVentaAsync(14)).ReturnsAsync(new ValidacionVentaResult
        {
            NoViable = true,
            PendienteRequisitos = true,
            RequisitosPendientes = new List<RequisitoPendiente>
            {
                new() { Tipo = TipoRequisitoPendiente.DocumentacionFaltante, Descripcion = "Falta DNI" }
            }
        });
        ventaService.Setup(v => v.ConfirmarVentaCreditoAsync(14)).ReturnsAsync(true);

        var controller = CreateController(
            ventaService.Object,
            creditoService.Object,
            validacionService.Object,
            includeAuthorizePermission: true);

        var result = await controller.Confirmar(14);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(14, redirect.RouteValues?["id"]);
        ventaService.Verify(v => v.RegistrarExcepcionDocumentalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ventaService.Verify(v => v.ConfirmarVentaCreditoAsync(14), Times.Once);
        Assert.NotNull(controller.TempData["Warning"]);
    }

    private static VentaController CreateController(
        IVentaService ventaService,
        ICreditoService creditoService,
        IValidacionVentaService validacionVentaService,
        bool includeAuthorizePermission)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "tester"),
            new(ClaimTypes.NameIdentifier, "tester-id")
        };

        if (includeAuthorizePermission)
        {
            claims.Add(new Claim("Permission", "ventas.authorize"));
        }

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };

        var controller = new VentaController(
            ventaService,
            new Mock<IConfiguracionPagoService>().Object,
            NullLogger<VentaController>.Instance,
            new Mock<IFinancialCalculationService>().Object,
            new Mock<IPrequalificationService>().Object,
            new Mock<IDocumentoClienteService>().Object,
            creditoService,
            new Mock<IDocumentacionService>().Object,
            new Mock<IClienteService>().Object,
            new Mock<IProductoService>().Object,
            new Mock<IClienteLookupService>().Object,
            validacionVentaService,
            CreateUserManager().Object,
            new Mock<ICajaService>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("/Venta/Details/1");
        controller.Url = urlHelper.Object;

        return controller;
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }
}
