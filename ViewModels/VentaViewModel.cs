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
        public DatosCreditoPersonallViewModel? DatosCreditoPersonall { get; set; }  // NUEVO

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

        #region Presentación

        public string EstadoDisplay => Estado switch
        {
            EstadoVenta.Cotizacion => "Cotización",
            EstadoVenta.Presupuesto => "Presupuesto",
            EstadoVenta.Confirmada => "Confirmada",
            EstadoVenta.Facturada => "Facturada",
            EstadoVenta.Entregada => "Entregada",
            EstadoVenta.Cancelada => "Cancelada",
            _ => Estado.ToString()
        };

        public string EstadoBadgeClass => Estado switch
        {
            EstadoVenta.Cotizacion => "badge bg-secondary",
            EstadoVenta.Presupuesto => "badge bg-info text-dark",
            EstadoVenta.Confirmada => "badge bg-primary",
            EstadoVenta.Facturada => "badge bg-success",
            EstadoVenta.Entregada => "badge bg-dark text-light",
            EstadoVenta.Cancelada => "badge bg-danger",
            _ => "badge bg-secondary"
        };

        public string EstadoAutorizacionDisplay => EstadoAutorizacion switch
        {
            EstadoAutorizacionVenta.NoRequiere => "No Requiere",
            EstadoAutorizacionVenta.PendienteAutorizacion => "Pendiente",
            EstadoAutorizacionVenta.Autorizada => "Autorizada",
            EstadoAutorizacionVenta.Rechazada => "Rechazada",
            _ => EstadoAutorizacion.ToString()
        };

        public string EstadoAutorizacionBadgeClass => EstadoAutorizacion switch
        {
            EstadoAutorizacionVenta.NoRequiere => "badge bg-dark text-light",
            EstadoAutorizacionVenta.PendienteAutorizacion => "badge bg-warning text-dark",
            EstadoAutorizacionVenta.Autorizada => "badge bg-success",
            EstadoAutorizacionVenta.Rechazada => "badge bg-danger",
            _ => "badge bg-secondary"
        };

        public string EstadoAutorizacionIconClass => EstadoAutorizacion switch
        {
            EstadoAutorizacionVenta.PendienteAutorizacion => "bi bi-hourglass-split",
            EstadoAutorizacionVenta.Autorizada => "bi bi-check-circle",
            EstadoAutorizacionVenta.Rechazada => "bi bi-x-circle",
            _ => string.Empty
        };

        #endregion

        #region Permisos de acción

        public bool PuedeEditar => Estado == EstadoVenta.Cotizacion || Estado == EstadoVenta.Presupuesto;

        public bool PuedeConfirmar =>
            Estado == EstadoVenta.Presupuesto && (!RequiereAutorizacion || EstadoAutorizacion == EstadoAutorizacionVenta.Autorizada);

        public bool PuedeFacturar =>
            Estado == EstadoVenta.Confirmada && (!RequiereAutorizacion || EstadoAutorizacion == EstadoAutorizacionVenta.Autorizada);

        public bool PuedeCancelar => Estado != EstadoVenta.Cancelada;

        public bool PuedeAutorizar => EstadoAutorizacion == EstadoAutorizacionVenta.PendienteAutorizacion;

        public bool PuedeCrearDevolucion =>
            Estado == EstadoVenta.Confirmada || Estado == EstadoVenta.Facturada || Estado == EstadoVenta.Entregada;

        public bool DebeAlertarAutorizacionPendiente =>
            RequiereAutorizacion && EstadoAutorizacion == EstadoAutorizacionVenta.PendienteAutorizacion;

        public bool FueRechazada => EstadoAutorizacion == EstadoAutorizacionVenta.Rechazada;

        #endregion
    }
}
