using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    /// <summary>
    /// Servicio centralizado para gesti�n de mora, alertas y cobranzas
    /// Consolida c�lculo de mora, generaci�n de alertas y auditor�a
    /// </summary>
    public interface IMoraService
    {
        // Configuraci�n
        Task<ConfiguracionMora> GetConfiguracionAsync();
        Task<ConfiguracionMora> UpdateConfiguracionAsync(ConfiguracionMoraViewModel viewModel);

        // Procesamiento de mora
        Task ProcesarMoraAsync();
        
        // Gesti�n de alertas
        Task<List<AlertaCobranzaViewModel>> GetAlertasActivasAsync();
            Task<List<AlertaCobranzaViewModel>> GetTodasAlertasAsync();
        Task<AlertaCobranzaViewModel?> GetAlertaByIdAsync(int id);
            Task<bool> ResolverAlertaAsync(int id, string? observaciones = null, byte[]? rowVersion = null);
            Task<bool> MarcarAlertaComoLeidaAsync(int id, byte[]? rowVersion = null);
        Task<List<AlertaCobranzaViewModel>> GetAlertasPorClienteAsync(int clienteId);

        // Logs y auditor�a
        Task<List<LogMora>> GetLogsAsync(int cantidad = 50);
    }
}