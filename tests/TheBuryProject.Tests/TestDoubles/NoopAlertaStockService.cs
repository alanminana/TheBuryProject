using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Tests.TestDoubles;

internal sealed class NoopAlertaStockService : IAlertaStockService
{
    public Task<int> GenerarAlertasStockBajoAsync() => Task.FromResult(0);
    public Task<List<AlertaStock>> GetAlertasPendientesAsync() => Task.FromResult(new List<AlertaStock>());
    public Task<PaginatedResult<AlertaStockViewModel>> BuscarAsync(AlertaStockFiltroViewModel filtro) =>
        Task.FromResult(new PaginatedResult<AlertaStockViewModel>());

    public Task<AlertaStockViewModel?> GetByIdAsync(int id) => Task.FromResult<AlertaStockViewModel?>(null);

    public Task<bool> ResolverAlertaAsync(int id, string usuarioResolucion, string? observaciones = null, byte[]? rowVersion = null) =>
        Task.FromResult(true);

    public Task<bool> IgnorarAlertaAsync(int id, string usuarioResolucion, string? observaciones = null, byte[]? rowVersion = null) =>
        Task.FromResult(true);

    public Task<AlertaStockEstadisticasViewModel> GetEstadisticasAsync() =>
        Task.FromResult(new AlertaStockEstadisticasViewModel());

    public Task<List<AlertaStock>> GetAlertasByProductoIdAsync(int productoId) =>
        Task.FromResult(new List<AlertaStock>());

    public Task<AlertaStock?> VerificarYGenerarAlertaAsync(int productoId) =>
        Task.FromResult<AlertaStock?>(null);

    public Task<int> VerificarYGenerarAlertasAsync(IEnumerable<int> productoIds) =>
        Task.FromResult(0);

    public Task<int> LimpiarAlertasAntiguasAsync(int diasAntiguedad = 30) => Task.FromResult(0);

    public Task<List<ProductoCriticoViewModel>> GetProductosCriticosAsync() =>
        Task.FromResult(new List<ProductoCriticoViewModel>());
}
