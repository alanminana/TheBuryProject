using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface IVentaService
    {
        Task<List<VentaViewModel>> GetAllAsync(VentaFilterViewModel? filter = null);
        Task<VentaViewModel?> GetByIdAsync(int id);
        Task<VentaViewModel> CreateAsync(VentaViewModel viewModel);
        Task<VentaViewModel?> UpdateAsync(int id, VentaViewModel viewModel);
        Task<bool> DeleteAsync(int id);
        Task<bool> ConfirmarVentaAsync(int id);
        Task<bool> CancelarVentaAsync(int id, string motivo);
        Task AsociarCreditoAVentaAsync(int ventaId, int creditoId);
        Task<bool> FacturarVentaAsync(int id, FacturaViewModel facturaViewModel);
        Task<bool> ValidarStockAsync(int ventaId);

        // Nuevos métodos para autorización
        Task<bool> SolicitarAutorizacionAsync(int id, string usuarioSolicita, string motivo);
        Task<bool> AutorizarVentaAsync(int id, string usuarioAutoriza, string motivo);
        Task<bool> RechazarVentaAsync(int id, string usuarioAutoriza, string motivo);
        Task<bool> RequiereAutorizacionAsync(VentaViewModel viewModel);

        // Métodos para datos adicionales
        Task<bool> GuardarDatosTarjetaAsync(int ventaId, DatosTarjetaViewModel datosTarjeta);
        Task<bool> GuardarDatosChequeAsync(int ventaId, DatosChequeViewModel datosCheque);
        Task<DatosTarjetaViewModel> CalcularCuotasTarjetaAsync(int tarjetaId, decimal monto, int cuotas);
        Task<DatosCreditoPersonalViewModel> CalcularCreditoPersonalAsync(int creditoId, decimal montoAFinanciar, int cuotas, DateTime fechaPrimeraCuota);
        Task<DatosCreditoPersonalViewModel?> ObtenerDatosCreditoVentaAsync(int ventaId);
        Task<bool> ValidarDisponibilidadCreditoAsync(int creditoId, decimal monto);
    }
}