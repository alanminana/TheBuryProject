namespace TheBuryProject.ViewModels
{
    /// <summary>
    /// ViewModel para búsqueda y filtros de proveedores
    /// </summary>
    public class ProveedorFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public bool SoloActivos { get; set; }
        public string? OrderBy { get; set; }
        public string? OrderDirection { get; set; }

        public IEnumerable<ProveedorViewModel> Proveedores { get; set; } = new List<ProveedorViewModel>();
        public int TotalResultados { get; set; }
    }
}