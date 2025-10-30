using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Representa una categoría de productos en el sistema.
    /// Soporta jerarquía (categorías padre e hijas).
    /// </summary>
    public class Categoria : BaseEntity
    {
        /// <summary>
        /// Código único de la categoría (ej: "ELEC", "FRIO")
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre descriptivo de la categoría
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción opcional de la categoría
        /// </summary>
        [StringLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Id de la categoría padre (null si es raíz)
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Indica si los productos de esta categoría requieren control de serie
        /// </summary>
        public bool ControlSerieDefault { get; set; } = false;

        /// <summary>
        /// Categoría padre (navegación)
        /// </summary>
        public virtual Categoria? Parent { get; set; }

        /// <summary>
        /// Categorías hijas (navegación)
        /// </summary>
        public virtual ICollection<Categoria> Children { get; set; } = new List<Categoria>();
    }
}