using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    /// <summary>
    /// Servicio centralizado para gestión de mora, alertas y cobranzas
    /// Consolida cálculo de mora, generación de alertas y auditoría
    /// </summary>
    public interface IMoraService
    {
        // Configuración
        Task<ConfiguracionMora> GetConfiguracionAsync();
        Task<ConfiguracionMora> UpdateConfiguracionAsync(ConfiguracionMoraViewModel viewModel);

        // Procesamiento de mora
        Task ProcesarMoraAsync();
        
        // Gestión de alertas
        Task<List<AlertaCobranzaViewModel>> GetAlertasActivasAsync();
        Task<AlertaCobranzaViewModel?> GetAlertaByIdAsync(int id);
        Task<bool> ResolverAlertaAsync(int id, string? observaciones = null);
        Task<List<AlertaCobranzaViewModel>> GetAlertasPorClienteAsync(int clienteId);

        // Logs y auditoría
        Task<List<LogMora>> GetLogsAsync(int cantidad = 50);
    }
}