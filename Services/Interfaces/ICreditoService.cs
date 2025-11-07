using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface ICreditoService
    {
        // CRUD básico
        Task<List<CreditoViewModel>> GetAllAsync(CreditoFilterViewModel? filter = null);
        Task<CreditoViewModel?> GetByIdAsync(int id);
        Task<CreditoViewModel> CreateAsync(CreditoViewModel viewModel);
        Task<bool> UpdateAsync(CreditoViewModel viewModel);
        Task<bool> DeleteAsync(int id);

        // Operaciones de crédito
        Task<SimularCreditoViewModel> SimularCreditoAsync(SimularCreditoViewModel modelo);
        Task<bool> AprobarCreditoAsync(int creditoId, string aprobadoPor);
        Task<bool> RechazarCreditoAsync(int creditoId, string motivo);
        Task<bool> CancelarCreditoAsync(int creditoId, string motivo);

        // Operaciones de cuotas
        Task<List<CuotaViewModel>> GetCuotasByCreditoAsync(int creditoId);
        Task<CuotaViewModel?> GetCuotaByIdAsync(int cuotaId);
        Task<bool> PagarCuotaAsync(PagarCuotaViewModel pago);
        Task<List<CuotaViewModel>> GetCuotasVencidasAsync();
        Task ActualizarEstadoCuotasAsync();

        // Cálculos financieros
        decimal CalcularMontoCuotaSistemaFrances(decimal monto, decimal tasaMensual, int cantidadCuotas);
        decimal CalcularCFTEA(decimal tasaMensual);
        Task<bool> RecalcularSaldoCreditoAsync(int creditoId);
    }
}