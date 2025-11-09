using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Relación N:N entre Proveedor y Categoría
    /// Indica en qué categorías se especializa cada proveedor
    /// </summary>
    public class ProveedorCategoria : DashboardDtos
    {
        public int ProveedorId { get; set; }
        public int CategoriaId { get; set; }

        // Navegación
        public virtual Proveedor Proveedor { get; set; } = null!;
        public virtual Categoria Categoria { get; set; } = null!;
    }
}