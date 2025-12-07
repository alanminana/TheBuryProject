using System.Collections.Generic;
using TheBuryProject.Models.Enums;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface IDocumentoClienteService
    {
        Task<List<DocumentoClienteViewModel>> GetAllAsync();
        Task<DocumentoClienteViewModel?> GetByIdAsync(int id);
        Task<List<DocumentoClienteViewModel>> GetByClienteIdAsync(int clienteId);
        Task<DocumentoClienteViewModel> UploadAsync(DocumentoClienteViewModel viewModel);
        Task<DocumentacionClienteEstadoViewModel> ValidarDocumentacionObligatoriaAsync(
            int clienteId,
            IEnumerable<TipoDocumentoCliente>? requeridos = null);
        Task<bool> VerificarAsync(int id, string verificadoPor, string? observaciones = null);
        Task<bool> RechazarAsync(int id, string motivo, string rechazadoPor);
        Task<bool> DeleteAsync(int id);
        Task<byte[]> DescargarArchivoAsync(int id);
        /// <summary>
        /// Busca documentos con filtros y paginacin
        /// </summary>
        Task<(List<DocumentoClienteViewModel> Documentos, int Total)> BuscarAsync(DocumentoClienteFilterViewModel filtro);
        /// <summary>
        /// Marca documentos vencidos automticamente (ejecutado por BackgroundService)
        /// </summary>
        Task MarcarVencidosAsync();
    }
}
