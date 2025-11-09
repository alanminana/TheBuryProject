using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface IMoraService
    {
        Task<ConfiguracionMora> GetConfiguracionAsync();
        Task<ConfiguracionMora> UpdateConfiguracionAsync(ConfiguracionMoraViewModel viewModel);
        Task ProcesarMoraAsync();
        Task<List<AlertaCobranza>> GetAlertasActivasAsync();
        Task<AlertaCobranza?> GetAlertaByIdAsync(int id);
        Task<bool> ResolverAlertaAsync(int id, string? observaciones);
        Task<List<LogMora>> GetLogsAsync(int cantidad = 50);
    }
}
