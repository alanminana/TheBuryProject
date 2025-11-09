using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Representa una marca de productos en el sistema.
    /// Soporta jerarquía (marcas padre e hijas/submarcas).
    /// </summary>
    public class Marca : DashboardDtos
    {
        /// <summary>
        /// Código único de la marca (ej: "SAM", "LG", "WHI")
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la marca
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción opcional de la marca
        /// </summary>
        [StringLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Id de la marca padre (null si es raíz, ej: Samsung es padre, Samsung Galaxy es hija)
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// País de origen de la marca
        /// </summary>
        [StringLength(100)]
        public string? PaisOrigen { get; set; }

        /// <summary>
        /// Marca padre (navegación)
        /// </summary>
        public virtual Marca? Parent { get; set; }
        /// <summary>
        /// Indica si la marca está activa
        /// </summary>
        public bool Activo { get; set; } = true;
        /// <summary>
        /// SubMarcas (navegación)
        /// </summary>
        public virtual ICollection<Marca> Children { get; set; } = new List<Marca>();
    }
}
