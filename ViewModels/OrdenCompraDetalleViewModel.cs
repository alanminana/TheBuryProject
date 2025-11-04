using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels
{
    public class OrdenCompraDetalleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La orden de compra es obligatoria")]
        public int OrdenCompraId { get; set; }

        [Required(ErrorMessage = "El producto es obligatorio")]
        [Display(Name = "Producto")]
        public int ProductoId { get; set; }

        [Display(Name = "Producto")]
        public string? ProductoNombre { get; set; }

        [Display(Name = "Código")]
        public string? ProductoCodigo { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Display(Name = "Cantidad")]
        [Range(0.01, 999999.99, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es obligatorio")]
        [Display(Name = "Precio Unitario")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioUnitario { get; set; }

        [Display(Name = "Subtotal")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Subtotal { get; set; }

        [Display(Name = "Cantidad Recibida")]
        [Range(0, 999999.99, ErrorMessage = "La cantidad recibida no puede ser negativa")]
        public decimal CantidadRecibida { get; set; }

        // Propiedad calculada
        public bool EstaCompleto => CantidadRecibida >= Cantidad;
    }
}