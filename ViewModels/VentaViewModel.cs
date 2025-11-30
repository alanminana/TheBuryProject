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
        public DateTime FechaVenta { get; set; } = DateTime.UtcNow;

        [Display(Name = "Estado")]
        public EstadoVenta Estado { get; set; } = EstadoVenta.Cotizacion;

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

        [Display(Name = "Crédito Personal")]
        public int? CreditoId { get; set; }
        public string? CreditoNumero { get; set; }

        // Autorización
        [Display(Name = "Estado Autorización")]
        public EstadoAutorizacionVenta EstadoAutorizacion { get; set; } = EstadoAutorizacionVenta.NoRequiere;

        public bool RequiereAutorizacion { get; set; } = false;
        public string? UsuarioSolicita { get; set; }
        public DateTime? FechaSolicitudAutorizacion { get; set; }
        public string? UsuarioAutoriza { get; set; }
        public DateTime? FechaAutorizacion { get; set; }
        public string? MotivoAutorizacion { get; set; }
        public string? MotivoRechazo { get; set; }

        [Display(Name = "Vendedor")]
        [StringLength(200)]
        public string? VendedorNombre { get; set; }

        [Display(Name = "Observaciones")]
        [DataType(DataType.MultilineText)]
        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime? FechaConfirmacion { get; set; }
        public DateTime? FechaFacturacion { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public string? MotivoCancelacion { get; set; }

        [Display(Name = "Detalles de la Venta")]
        public List<VentaDetalleViewModel> Detalles { get; set; } = new List<VentaDetalleViewModel>();

        public List<FacturaViewModel> Facturas { get; set; } = new List<FacturaViewModel>();

        // Datos adicionales según tipo de pago
        public DatosTarjetaViewModel? DatosTarjeta { get; set; }
        public DatosChequeViewModel? DatosCheque { get; set; }
        public DatosCreditoPersonalViewModel? DatosCreditoPersonal { get; set; }  // NUEVO

        // Datos de financiamiento
        [Display(Name = "Venta financiada")]
        public bool EsFinanciada { get; set; }

        [Display(Name = "Anticipo"), DataType(DataType.Currency)]
        [Range(0, double.MaxValue, ErrorMessage = "El anticipo no puede ser negativo")]
        public decimal? Anticipo { get; set; }

        [Display(Name = "Tasa mensual (%)")]
        [Range(0, 100, ErrorMessage = "La tasa debe estar entre 0% y 100%")]
        public decimal? TasaInteresMensualFinanciacion { get; set; }

        [Display(Name = "Cantidad de cuotas")]
        [Range(1, 120, ErrorMessage = "Las cuotas deben estar entre 1 y 120")]
        public int? CantidadCuotasFinanciacion { get; set; }

        [Display(Name = "Monto financiado estimado"), DataType(DataType.Currency)]
        public decimal? MontoFinanciadoEstimado { get; set; }

        [Display(Name = "Cuota estimada"), DataType(DataType.Currency)]
        public decimal? CuotaEstimada { get; set; }

        [Display(Name = "Ingreso neto declarado"), DataType(DataType.Currency)]
        public decimal? IngresoNetoDeclarado { get; set; }

        [Display(Name = "Otras deudas declaradas"), DataType(DataType.Currency)]
        public decimal? EndeudamientoDeclarado { get; set; }

        [Display(Name = "Antigüedad laboral (meses)")]
        public int? AntiguedadLaboralMeses { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}