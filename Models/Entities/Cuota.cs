using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Representa una cuota de un crédito
    /// </summary>
    public class Cuota : BaseEntity
    {
        public int CreditoId { get; set; }

        [Required]
        public int NumeroCuota { get; set; }

        [Required]
        public DateTime FechaVencimiento { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal MontoOriginal { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal MontoPendiente { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MontoPagado { get; set; } = 0;

        public DateTime? FechaPago { get; set; }

        [Required]
        public EstadoCuota Estado { get; set; } = EstadoCuota.Pendiente;

        public int? DiasAtraso { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Navegación
        public virtual Credito Credito { get; set; } = null!;
    }
}
