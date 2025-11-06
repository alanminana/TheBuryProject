using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Representa un crédito otorgado a un cliente
    /// </summary>
    public class Credito : BaseEntity
    {
        // Relación con Cliente
        public int ClienteId { get; set; }

        [Required]
        [StringLength(50)]
        public string Numero { get; set; } = string.Empty;

        // Montos
        [Required]
        [Range(0, double.MaxValue)]
        public decimal MontoSolicitado { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MontoAprobado { get; set; }

        // Tasa e Interés
        [Required]
        [Range(0, 100)]
        public decimal TasaInteres { get; set; }

        public int CantidadCuotas { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MontoCuota { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MontoTotal { get; set; }

        // Estados y Fechas
        [Required]
        public EstadoCredito Estado { get; set; } = EstadoCredito.Solicitado;

        [Required]
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        public DateTime? FechaAprobacion { get; set; }

        public DateTime? FechaDesembolso { get; set; }

        public DateTime? FechaFinalizacion { get; set; }

        // Evaluación
        public decimal PuntajeRiesgoInicial { get; set; }

        public decimal? SueldoCliente { get; set; }

        public decimal? PorcentajeSueldo { get; set; }

        public bool TieneGarante { get; set; }

        public int? GaranteId { get; set; }

        // Aprobación/Rechazo
        [StringLength(100)]
        public string? AprobadoPor { get; set; }

        [StringLength(100)]
        public string? RechazadoPor { get; set; }

        public DateTime? FechaRechazo { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }

        // Observaciones
        [StringLength(1000)]
        public string? Observaciones { get; set; }

        // Navegación
        public virtual Cliente Cliente { get; set; } = null!;
        public virtual Garante? Garante { get; set; }
        public virtual ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();
    }
}