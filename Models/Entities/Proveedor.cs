using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Representa un proveedor del sistema
    /// </summary>
    public class Proveedor : BaseEntity
    {
        /// <summary>
        /// CUIT del proveedor (11 dígitos sin guiones)
        /// </summary>
        [Required]
        [StringLength(11)]
        public string Cuit { get; set; } = string.Empty;

        /// <summary>
        /// Razón Social del proveedor
        /// </summary>
        [Required]
        [StringLength(200)]
        public string RazonSocial { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de fantasía (opcional)
        /// </summary>
        [StringLength(200)]
        public string? NombreFantasia { get; set; }

        /// <summary>
        /// Email de contacto
        /// </summary>
        [StringLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// Teléfono de contacto
        /// </summary>
        [StringLength(50)]
        public string? Telefono { get; set; }

        /// <summary>
        /// Dirección completa
        /// </summary>
        [StringLength(300)]
        public string? Direccion { get; set; }

        /// <summary>
        /// Ciudad
        /// </summary>
        [StringLength(100)]
        public string? Ciudad { get; set; }

        /// <summary>
        /// Provincia
        /// </summary>
        [StringLength(100)]
        public string? Provincia { get; set; }

        /// <summary>
        /// Código Postal
        /// </summary>
        [StringLength(10)]
        public string? CodigoPostal { get; set; }

        /// <summary>
        /// Nombre del contacto principal
        /// </summary>
        [StringLength(200)]
        public string? Contacto { get; set; }

        /// <summary>
        /// Aclaraciones generales sobre el proveedor
        /// </summary>
        [StringLength(2000)]
        public string? Aclaraciones { get; set; }

        /// <summary>
        /// Indica si el proveedor está activo
        /// </summary>
        public bool Activo { get; set; } = true;

        // Navegación - Relaciones opcionales para facilitar búsqueda
        public virtual ICollection<ProveedorProducto> ProveedorProductos { get; set; } = new List<ProveedorProducto>();
        public virtual ICollection<ProveedorMarca> ProveedorMarcas { get; set; } = new List<ProveedorMarca>();
        public virtual ICollection<ProveedorCategoria> ProveedorCategorias { get; set; } = new List<ProveedorCategoria>();

        // Navegación - Órdenes de compra
        public virtual ICollection<OrdenCompra> OrdenesCompra { get; set; } = new List<OrdenCompra>();

        // Navegación - Cheques
        public virtual ICollection<Cheque> Cheques { get; set; } = new List<Cheque>();
    }
}
