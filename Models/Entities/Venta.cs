using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities
{
    public class Venta : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Numero { get; set; } = string.Empty;

        [Required]
        public int ClienteId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Required]
        public EstadoVenta Estado { get; set; } = EstadoVenta.Presupuesto;

        [Required]
        public TipoPago TipoPago { get; set; } = TipoPago.Efectivo;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Descuento { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal IVA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        // Si es venta a crédito
        public int? CreditoId { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [StringLength(200)]
        public string? VendedorNombre { get; set; }

        public DateTime? FechaFacturacion { get; set; }

        public DateTime? FechaCancelacion { get; set; }

        [StringLength(500)]
        public string? MotivoCancelacion { get; set; }

        // Navegación
        public virtual Cliente Cliente { get; set; } = null!;
        public virtual Credito? Credito { get; set; }
        public virtual ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
        public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
    }
}