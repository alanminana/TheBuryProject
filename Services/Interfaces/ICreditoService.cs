using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface ICreditoService
    {
        // CRUD b�sico
        Task<List<CreditoViewModel>> GetAllAsync(CreditoFilterViewModel? filter = null);
        Task<CreditoViewModel?> GetByIdAsync(int id);
        Task<List<CreditoViewModel>> GetByClienteIdAsync(int clienteId);
        Task<CreditoViewModel> CreateAsync(CreditoViewModel viewModel);
        Task<CreditoViewModel> CreatePendienteConfiguracionAsync(int clienteId, decimal montoTotal);
        Task<bool> UpdateAsync(CreditoViewModel viewModel);
        Task<bool> DeleteAsync(int id);

        // Operaciones de cr�dito
        Task<SimularCreditoViewModel> SimularCreditoAsync(SimularCreditoViewModel modelo);
        Task<bool> AprobarCreditoAsync(int creditoId, string aprobadoPor);
        Task<bool> RechazarCreditoAsync(int creditoId, string motivo);
        Task<bool> CancelarCreditoAsync(int creditoId, string motivo);
        Task<(bool Success, string? NumeroCredito, string? ErrorMessage)> SolicitarCreditoAsync(
    SolicitudCreditoViewModel solicitud,
    string usuarioSolicitante,
    CancellationToken cancellationToken = default);

        // Operaciones de cuotas
        Task<List<CuotaViewModel>> GetCuotasByCreditoAsync(int creditoId);
        Task<CuotaViewModel?> GetCuotaByIdAsync(int cuotaId);
        Task<bool> PagarCuotaAsync(PagarCuotaViewModel pago);
        Task<List<CuotaViewModel>> GetCuotasVencidasAsync();
        Task ActualizarEstadoCuotasAsync();

        // C�lculos financieros
        decimal CalcularMontoCuotaSistemaFrances(decimal monto, decimal tasaMensual, int cantidadCuotas);
        decimal CalcularCFTEA(decimal tasaMensual);
        Task<bool> RecalcularSaldoCreditoAsync(int creditoId);
    }
}