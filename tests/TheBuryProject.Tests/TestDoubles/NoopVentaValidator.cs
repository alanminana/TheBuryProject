using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Validators;

namespace TheBuryProject.Tests.TestDoubles;

internal sealed class NoopVentaValidator : IVentaValidator
{
    public void ValidarEstadoParaEdicion(Venta venta) { }
    public void ValidarEstadoParaEliminacion(Venta venta) { }
    public void ValidarEstadoParaConfirmacion(Venta venta) { }
    public void ValidarEstadoParaFacturacion(Venta venta) { }
    public void ValidarAutorizacion(Venta venta) { }
    public void ValidarStock(Venta venta) { }
    public void ValidarNoEstaCancelada(Venta venta) { }
    public void ValidarEstadoAutorizacion(Venta venta, EstadoAutorizacionVenta estadoEsperado) { }
}
