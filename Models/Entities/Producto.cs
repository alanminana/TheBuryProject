using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Entidad que representa un producto en el sistema
    /// </summary>
    public class Producto : BaseEntity
    {
        /// <summary>
        /// C�digo �nico del producto
        /// </summary>
        [Required(ErrorMessage = "El c�digo es obligatorio")]
        [StringLength(50, ErrorMessage = "El c�digo no puede superar 50 caracteres")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del producto
        /// </summary>
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede superar 200 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripci�n detallada del producto
        /// </summary>
        [StringLength(1000, ErrorMessage = "La descripci�n no puede superar 1000 caracteres")]
        public string? Descripcion { get; set; }

        /// <summary>
        /// ID de la categor�a a la que pertenece
        /// </summary>
        [Required(ErrorMessage = "La categor�a es obligatoria")]
        public int CategoriaId { get; set; }

        /// <summary>
        /// ID de la marca del producto
        /// </summary>
        [Required(ErrorMessage = "La marca es obligatoria")]
        public int MarcaId { get; set; }

        /// <summary>
        /// Precio de compra del producto
        /// </summary>
        [Required(ErrorMessage = "El precio de compra es obligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio de compra debe ser mayor o igual a 0")]
        public decimal PrecioCompra { get; set; }

        /// <summary>
        /// Precio de venta del producto
        /// </summary>
        [Required(ErrorMessage = "El precio de venta es obligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a 0")]
        public decimal PrecioVenta { get; set; }

        /// <summary>
        /// Indica si el producto requiere n�mero de serie para control individual
        /// </summary>
        public bool RequiereNumeroSerie { get; set; } = false;

        /// <summary>
        /// Stock m�nimo que debe mantener el producto (alerta)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "El stock m�nimo debe ser mayor o igual a 0")]
        public decimal StockMinimo { get; set; } = 0;

        /// <summary>
        /// Stock actual del producto
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "El stock actual debe ser mayor o igual a 0")]
        public decimal StockActual { get; set; } = 0;

        /// <summary>
        /// Unidad de medida (ej: "UN", "KG", "MT", "LT")
        /// </summary>
        [StringLength(10, ErrorMessage = "La unidad de medida no puede superar 10 caracteres")]
        public string UnidadMedida { get; set; } = "UN";

        /// <summary>
        /// Indica si el producto est� activo para la venta
        /// </summary>
        public bool Activo { get; set; } = true;

        // Propiedades de navegaci�n
        /// <summary>
        /// Categor�a a la que pertenece el producto
        /// </summary>
        public virtual Categoria Categoria { get; set; } = null!;

        /// <summary>
        /// Marca del producto
        /// </summary>
        public virtual Marca Marca { get; set; } = null!;
    }
}