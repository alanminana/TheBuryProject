using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Representa un cr�dito otorgado a un cliente
    /// </summary>
    public class Credito : BaseEntity
    {
        public int ClienteId { get; set; }

        [StringLength(50)]
        public string Numero { get; set; } = string.Empty;

        public decimal MontoSolicitado { get; set; }
        public decimal MontoAprobado { get; set; }
        public decimal TasaInteres { get; set; } // Tasa mensual
        public int CantidadCuotas { get; set; }
        public decimal MontoCuota { get; set; }

        public decimal CFTEA { get; set; } // Costo Financiero Total Efectivo Anual
        public decimal TotalAPagar { get; set; }
        public decimal SaldoPendiente { get; set; }

        public EstadoCredito Estado { get; set; } = EstadoCredito.Solicitado;

        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
        public DateTime? FechaAprobacion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public DateTime? FechaPrimeraCuota { get; set; }

        public decimal PuntajeRiesgoInicial { get; set; }

        // Garante (opcional)
        public int? GaranteId { get; set; }
        public bool RequiereGarante { get; set; } = false;

        // Datos de aprobaci�n
        [StringLength(100)]
        public string? AprobadoPor { get; set; }

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        // Navigation Properties
        public virtual Cliente Cliente { get; set; } = null!;
        public virtual Garante? Garante { get; set; }
        public virtual ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();
    }
}