using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities
{
    public class Venta : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public string Numero { get; set; } = string.Empty;

        public int ClienteId { get; set; }

        [Required]
        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Required]
        public EstadoVenta Estado { get; set; } = EstadoVenta.Cotizacion;

        [Required]
        public TipoPago TipoPago { get; set; } = TipoPago.Efectivo;

        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal IVA { get; set; }
        public decimal Total { get; set; }

        // Crédito personal
        public int? CreditoId { get; set; }

        // Autorización
        public EstadoAutorizacionVenta EstadoAutorizacion { get; set; } = EstadoAutorizacionVenta.NoRequiere;
        public bool RequiereAutorizacion { get; set; } = false;

        [StringLength(200)]
        public string? UsuarioSolicita { get; set; }

        public DateTime? FechaSolicitudAutorizacion { get; set; }

        [StringLength(200)]
        public string? UsuarioAutoriza { get; set; }

        public DateTime? FechaAutorizacion { get; set; }

        [StringLength(1000)]
        public string? MotivoAutorizacion { get; set; }

        [StringLength(1000)]
        public string? MotivoRechazo { get; set; }

        // Información adicional
        [StringLength(200)]
        public string? VendedorNombre { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime? FechaConfirmacion { get; set; }
        public DateTime? FechaFacturacion { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaCancelacion { get; set; }

        [StringLength(500)]
        public string? MotivoCancelacion { get; set; }

        // Navigation properties
        public virtual Cliente Cliente { get; set; } = null!;
        public virtual Credito? Credito { get; set; }
        public virtual ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
        public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
        public virtual DatosTarjeta? DatosTarjeta { get; set; }
        public virtual DatosCheque? DatosCheque { get; set; }
        public virtual ICollection<VentaCreditoCuota> VentaCreditoCuotas { get; set; } = new List<VentaCreditoCuota>();  // NUEVO

    }
}