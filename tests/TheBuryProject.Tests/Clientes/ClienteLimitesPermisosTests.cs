using System.Reflection;
using TheBuryProject.Controllers;
using TheBuryProject.Filters;
using Xunit;

namespace TheBuryProject.Tests.Clientes;

public class ClienteLimitesPermisosTests
{
    [Fact]
    public void LimitesPorPuntaje_Post_RequierePermisoAdministrarLimites()
    {
        var method = typeof(ClienteController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m =>
                m.Name == "LimitesPorPuntaje" &&
                m.GetParameters().Length > 0 &&
                m.GetParameters()[0].ParameterType.Name == "ClienteCreditoLimitesViewModel");

        Assert.NotNull(method);

        var permiso = method!
            .GetCustomAttributes(typeof(PermisoRequeridoAttribute), true)
            .Cast<PermisoRequeridoAttribute>()
            .FirstOrDefault();

        Assert.NotNull(permiso);
        Assert.Equal("clientes", permiso!.Modulo);
        Assert.Equal("managecreditlimits", permiso.Accion);
    }
}
