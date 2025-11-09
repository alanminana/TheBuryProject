using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
        Task<List<VentasPorDiaDto>> GetVentasUltimos7DiasAsync();
        Task<List<VentasPorMesDto>> GetVentasUltimos12MesesAsync();
        Task<List<ProductoMasVendidoDto>> GetProductosMasVendidosAsync(int top = 10);
        Task<List<CreditoPorEstadoDto>> GetCreditosPorEstadoAsync();
        Task<List<CobranzaPorDiaDto>> GetCobranzaUltimos30DiasAsync();
        Task<List<AlertaDto>> GetAlertasPendientesAsync();
        Task<decimal> CalcularTasaMorosidadAsync();
        Task<decimal> CalcularEfectividadCobranzaAsync();
    }
}