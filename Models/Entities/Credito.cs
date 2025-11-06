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
        public int ClienteId { get; set; }

        [StringLength(50)]
        public string Numero { get; set; } = string.Empty;

        public decimal MontoSolicitado { get; set; }
        public decimal MontoAprobado { get; set; }
        public decimal TasaInteres { get; set; }
        public int CantidadCuotas { get; set; }
        public decimal MontoCuota { get; set; }

        public EstadoCredito Estado { get; set; } = EstadoCredito.Solicitado;

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public DateTime? FechaAprobacion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }

        public decimal PuntajeRiesgoInicial { get; set; }

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        // Navigation Properties
        public virtual Cliente Cliente { get; set; } = null!;
    }
}