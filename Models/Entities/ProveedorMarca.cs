using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Relación N:N entre Proveedor y Marca
    /// Indica qué marcas representa cada proveedor
    /// </summary>
    public class ProveedorMarca : DashboardDtos
    {
        public int ProveedorId { get; set; }
        public int MarcaId { get; set; }

        // Navegación
        public virtual Proveedor Proveedor { get; set; } = null!;
        public virtual Marca Marca { get; set; } = null!;
    }
}