using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Relación N:N entre Proveedor y Producto
    /// Indica qué productos puede proveer cada proveedor
    /// </summary>
    public class ProveedorProducto : DashboardDtos
    {
        public int ProveedorId { get; set; }
        public int ProductoId { get; set; }

        // Navegación
        public virtual Proveedor Proveedor { get; set; } = null!;
        public virtual Producto Producto { get; set; } = null!;
    }
}