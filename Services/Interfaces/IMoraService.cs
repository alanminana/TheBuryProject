using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface IMoraService
    {
        Task<ConfiguracionMoraViewModel> GetConfiguracionAsync();
        Task SaveConfiguracionAsync(ConfiguracionMoraViewModel model);
        Task<LogMora> ProcesarMoraAsync();
        Task<List<AlertaCobranzaViewModel>> GetAlertasActivasAsync();
        Task<List<LogMora>> GetLogsAsync(int cantidad = 20);
        Task MarcarAlertaLeidaAsync(int alertaId, string usuario);
        Task ResolverAlertaAsync(int alertaId, string usuario, string nota);
        Task GenerarAlertasVencimientoAsync();
        Task ActualizarMoraCuotaAsync(int cuotaId);
        Task<decimal> CalcularRecargoMoraAsync(int cuotaId);
    }
}