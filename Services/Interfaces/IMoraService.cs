using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface IMoraService
    {
        // Configuración
        Task<ConfiguracionMoraViewModel> GetConfiguracionAsync();
        Task<ConfiguracionMoraViewModel> SaveConfiguracionAsync(ConfiguracionMoraViewModel viewModel);

        // Procesamiento de Mora
        Task<LogMora> ProcesarMoraAsync();
        Task ActualizarMoraCuotaAsync(int cuotaId);
        Task<decimal> CalcularRecargoMoraAsync(int cuotaId);

        // Alertas
        Task GenerarAlertasVencimientoAsync();
        Task<List<AlertaCobranzaViewModel>> GetAlertasActivasAsync();
        Task<List<AlertaCobranzaViewModel>> BuscarAlertasAsync(AlertaCobranzaFilterViewModel filtro);
        Task<bool> MarcarAlertaLeidaAsync(int alertaId, string usuario);
        Task<bool> ResolverAlertaAsync(int alertaId, string nota, string usuario);

        // Logs
        Task<List<LogMora>> GetLogsAsync(int cantidad = 20);
    }
}