using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface IVentaService
    {
        Task<List<VentaViewModel>> GetAllAsync(VentaFilterViewModel? filter = null);
        Task<VentaViewModel?> GetByIdAsync(int id);
        Task<VentaViewModel> CreateAsync(VentaViewModel viewModel);
        Task<bool> UpdateAsync(VentaViewModel viewModel);
        Task<bool> DeleteAsync(int id);
        Task<bool> ConfirmarVentaAsync(int ventaId);
        Task<bool> CancelarVentaAsync(int ventaId, string motivo);
        Task<bool> FacturarVentaAsync(int ventaId, FacturaViewModel factura);
        Task<bool> ValidarStockAsync(int ventaId);
        Task<decimal> CalcularTotalVentaAsync(int ventaId);
        Task<bool> NumeroVentaExisteAsync(string numero, int? excludeId = null);
    }
}