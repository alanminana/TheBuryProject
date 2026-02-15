using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Ventas;

public class VentaViewModelExcepcionDocumentalTests
{
    [Fact]
    public void TieneExcepcionDocumentalRegistrada_ConLineaValida_ExponeDatosCorrectos()
    {
        var vm = new VentaViewModel
        {
            MotivoAutorizacion = "AUTORIZACION|texto previo\nEXCEPCION_DOC|2026-02-14T12:30:00.0000000Z|supervisor1|Documento validado por contingencia"
        };

        Assert.True(vm.TieneExcepcionDocumentalRegistrada);
        Assert.Equal("supervisor1", vm.UsuarioExcepcionDocumental);
        Assert.Equal("Documento validado por contingencia", vm.MotivoExcepcionDocumental);
        Assert.True(vm.FechaExcepcionDocumental.HasValue);
    }

    [Fact]
    public void TieneExcepcionDocumentalRegistrada_SinLineaValida_NoExponeDatos()
    {
        var vm = new VentaViewModel
        {
            MotivoAutorizacion = "Autorización general sin excepción documental"
        };

        Assert.False(vm.TieneExcepcionDocumentalRegistrada);
        Assert.Null(vm.UsuarioExcepcionDocumental);
        Assert.Null(vm.MotivoExcepcionDocumental);
        Assert.Null(vm.FechaExcepcionDocumental);
    }
}
