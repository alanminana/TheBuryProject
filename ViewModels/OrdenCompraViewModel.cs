using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class OrdenCompraViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El número de orden es obligatorio")]
        [StringLength(50, ErrorMessage = "El número no puede tener más de 50 caracteres")]
        [Display(Name = "Número de Orden")]
        public string Numero { get; set; } = string.Empty;

        [Required(ErrorMessage = "El proveedor es obligatorio")]
        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        [Display(Name = "Proveedor")]
        public string? ProveedorNombre { get; set; }

        [Required(ErrorMessage = "La fecha de emisión es obligatoria")]
        [Display(Name = "Fecha de Emisión")]
        [DataType(DataType.Date)]
        public DateTime FechaEmision { get; set; } = DateTime.Today;

        [Display(Name = "Fecha de Entrega Estimada")]
        [DataType(DataType.Date)]
        public DateTime? FechaEntregaEstimada { get; set; }

        [Display(Name = "Fecha de Recepción")]
        [DataType(DataType.Date)]
        public DateTime? FechaRecepcion { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [Display(Name = "Estado")]
        public EstadoOrdenCompra Estado { get; set; } = EstadoOrdenCompra.Borrador;

        [Display(Name = "Estado")]
        public string? EstadoNombre { get; set; }

        [Display(Name = "Subtotal")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Subtotal { get; set; }

        [Display(Name = "Descuento")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Descuento { get; set; }

        [Display(Name = "IVA")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Iva { get; set; }

        [Display(Name = "Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Total { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden tener más de 500 caracteres")]
        [Display(Name = "Observaciones")]
        [DataType(DataType.MultilineText)]
        public string? Observaciones { get; set; }

        [Display(Name = "Total Items")]
        public decimal TotalItems { get; set; }

        [Display(Name = "Total Recibido")]
        public decimal TotalRecibido { get; set; }

        // Lista de detalles
        public List<OrdenCompraDetalleViewModel> Detalles { get; set; } = new List<OrdenCompraDetalleViewModel>();

        // Información de auditoría
        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime UpdatedAt { get; set; }
    }
}