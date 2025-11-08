using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface IEvaluacionCreditoService
    {
        /// <summary>
        /// Evalúa una solicitud de crédito y devuelve el resultado con semáforo
        /// </summary>
        Task<EvaluacionCreditoViewModel> EvaluarSolicitudAsync(int clienteId, decimal montoSolicitado, int? garanteId = null);

        /// <summary>
        /// Obtiene la última evaluación de un crédito
        /// </summary>
        Task<EvaluacionCreditoViewModel?> GetEvaluacionByCreditoIdAsync(int creditoId);

        /// <summary>
        /// Obtiene todas las evaluaciones de un cliente
        /// </summary>
        Task<List<EvaluacionCreditoViewModel>> GetEvaluacionesByClienteIdAsync(int clienteId);
    }
}