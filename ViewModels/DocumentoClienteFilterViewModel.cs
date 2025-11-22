using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    /// <summary>
    /// ViewModel para filtrado, búsqueda y paginación de documentos de clientes
    /// </summary>
    public class DocumentoClienteFilterViewModel
    {
        // Filtros de búsqueda
        public int? ClienteId { get; set; }
        public int? TipoDocumento { get; set; }
        public EstadoDocumento? Estado { get; set; }
        public bool SoloPendientes { get; set; }
        public bool SoloVencidos { get; set; }

        // Paginación
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Resultados
        public List<DocumentoClienteViewModel> Documentos { get; set; } = new();
        public int TotalResultados { get; set; }

        // Propiedades calculadas para paginación
        public int TotalPages => TotalResultados == 0 ? 1 : (int)Math.Ceiling((double)TotalResultados / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}