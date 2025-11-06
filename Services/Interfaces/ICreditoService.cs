using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Services.Interfaces
{
    public interface ICreditoService
    {
        // CRUD Básico
        Task<Credito?> GetByIdAsync(int id);
        Task<Credito?> GetByNumeroAsync(string numero);
        Task<IEnumerable<Credito>> GetAllAsync();
        Task<Credito> CreateAsync(Credito credito);
        Task<Credito> UpdateAsync(Credito credito);
        Task<bool> DeleteAsync(int id);

        // Búsqueda y Filtrado
        Task<IEnumerable<Credito>> SearchAsync(
            string? searchTerm = null,
            int? clienteId = null,
            EstadoCredito? estado = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            decimal? montoMinimo = null,
            decimal? montoMaximo = null,
            bool soloEnMora = false,
            string orderBy = "FechaSolicitud",
            string orderDirection = "DESC");

        Task<IEnumerable<Credito>> GetByClienteIdAsync(int clienteId);
        Task<IEnumerable<Credito>> GetCreditosActivosAsync();
        Task<IEnumerable<Credito>> GetCreditosEnMoraAsync();

        // Evaluación de Crédito
        Task<(bool aprobado, string motivo, decimal tasaSugerida)> EvaluarCreditoAsync(
            int clienteId,
            decimal montoSolicitado,
            int cantidadCuotas,
            int? garanteId = null);

        // Cálculos
        decimal CalcularMontoCuota(decimal montoTotal, int cantidadCuotas, decimal tasaInteres);
        decimal CalcularMontoTotal(decimal montoSolicitado, int cantidadCuotas, decimal tasaInteres);
        decimal CalcularTasaSugerida(decimal puntajeRiesgo, bool tieneGarante);
        decimal CalcularPorcentajeSueldo(decimal montoCuota, decimal sueldo);

        // Gestión de Estado
        Task<Credito> AprobarAsync(int creditoId, decimal montoAprobado, decimal tasaInteres, int cantidadCuotas, string aprobadoPor);
        Task<Credito> RechazarAsync(int creditoId, string motivo, string rechazadoPor);
        Task<Credito> DesembolsarAsync(int creditoId);
        Task<Credito> FinalizarAsync(int creditoId);
        Task<Credito> MarcarEnMoraAsync(int creditoId);

        // Plan de Cuotas
        Task<IEnumerable<Cuota>> GenerarPlanCuotasAsync(int creditoId, DateTime fechaInicio);
        Task<IEnumerable<Cuota>> GetCuotasByCreditoIdAsync(int creditoId);

        // Validaciones
        Task<bool> ClienteTieneCreditosActivosAsync(int clienteId);
        Task<bool> ExisteNumeroAsync(string numero, int? excludeId = null);
        Task<string> GenerarNumeroAsync();

        // Estadísticas
        Task<decimal> GetTotalAdeudadoByClienteAsync(int clienteId);
        Task<int> GetCantidadCreditosActivosByClienteAsync(int clienteId);
    }
}