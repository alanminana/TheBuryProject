using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface IDocumentoClienteService
    {
        Task<List<DocumentoClienteViewModel>> GetAllAsync();
        Task<DocumentoClienteViewModel?> GetByIdAsync(int id);
        Task<List<DocumentoClienteViewModel>> GetByClienteIdAsync(int clienteId);
        Task<DocumentoClienteViewModel> UploadAsync(DocumentoClienteViewModel viewModel);
        Task<bool> VerificarAsync(int id, string verificadoPor, string? observaciones = null);
        Task<bool> RechazarAsync(int id, string motivo, string rechazadoPor);
        Task<bool> DeleteAsync(int id);
        Task<byte[]> DescargarArchivoAsync(int id);
        Task<List<DocumentoClienteViewModel>> BuscarAsync(DocumentoClienteFilterViewModel filtro);
        Task MarcarVencidosAsync(); // Job diario
    }
}
