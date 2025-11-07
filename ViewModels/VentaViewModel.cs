using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class VentaViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Número")]
        public string Numero { get; set; } = string.Empty;

        [Display(Name = "Cliente")]
        [Required(ErrorMessage = "El cliente es requerido")]
        public int ClienteId { get; set; }

        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteDocumento { get; set; } = string.Empty;

        [Display(Name = "Fecha de Venta")]
        [Required]
        [DataType(DataType.Date)]
        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Display(Name = "Estado")]
        public EstadoVenta Estado { get; set; } = EstadoVenta.Presupuesto;

        [Display(Name = "Tipo de Pago")]
        [Required(ErrorMessage = "El tipo de pago es requerido")]
        public TipoPago TipoPago { get; set; } = TipoPago.Efectivo;

        [Display(Name = "Subtotal")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Subtotal { get; set; }

        [Display(Name = "Descuento")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Descuento { get; set; }

        [Display(Name = "IVA")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal IVA { get; set; }

        [Display(Name = "Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Total { get; set; }

        [Display(Name = "Crédito ID")]
        public int? CreditoId { get; set; }

        public string? CreditoNumero { get; set; }

        [Display(Name = "Vendedor")]
        [StringLength(200)]
        public string? VendedorNombre { get; set; }

        [Display(Name = "Observaciones")]
        [DataType(DataType.MultilineText)]
        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime? FechaFacturacion { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public string? MotivoCancelacion { get; set; }

        [Display(Name = "Detalles de la Venta")]
        public List<VentaDetalleViewModel> Detalles { get; set; } = new List<VentaDetalleViewModel>();

        public List<FacturaViewModel> Facturas { get; set; } = new List<FacturaViewModel>();

        public DateTime CreatedAt { get; set; }
    }
}