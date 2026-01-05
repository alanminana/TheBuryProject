using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Tests.TestDoubles;

internal sealed class NoopNotificacionService : INotificacionService
{
    public Task<Notificacion> CrearNotificacionAsync(CrearNotificacionViewModel model)
        => Task.FromResult(new Notificacion());

    public Task CrearNotificacionParaUsuarioAsync(
        string usuario,
        TipoNotificacion tipo,
        string titulo,
        string mensaje,
        string? url = null,
        PrioridadNotificacion prioridad = PrioridadNotificacion.Media)
        => Task.CompletedTask;

    public Task CrearNotificacionParaRolAsync(
        string rol,
        TipoNotificacion tipo,
        string titulo,
        string mensaje,
        string? url = null,
        PrioridadNotificacion prioridad = PrioridadNotificacion.Media)
        => Task.CompletedTask;

    public Task<List<NotificacionViewModel>> ObtenerNotificacionesUsuarioAsync(string usuario, bool soloNoLeidas = false, int limite = 50)
        => Task.FromResult(new List<NotificacionViewModel>());

    public Task<int> ObtenerCantidadNoLeidasAsync(string usuario)
        => Task.FromResult(0);

    public Task<Notificacion?> ObtenerNotificacionPorIdAsync(int id)
        => Task.FromResult<Notificacion?>(null);

    public Task MarcarComoLeidaAsync(int notificacionId, string usuario, byte[]? rowVersion = null)
        => Task.CompletedTask;

    public Task MarcarTodasComoLeidasAsync(string usuario)
        => Task.CompletedTask;

    public Task EliminarNotificacionAsync(int id, string usuario, byte[]? rowVersion = null)
        => Task.CompletedTask;

    public Task LimpiarNotificacionesAntiguasAsync(int diasAntiguedad = 30)
        => Task.CompletedTask;

    public Task<ListaNotificacionesViewModel> ObtenerResumenNotificacionesAsync(string usuario)
        => Task.FromResult(new ListaNotificacionesViewModel());
}
