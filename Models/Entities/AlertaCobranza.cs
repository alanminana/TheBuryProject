using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Alerta de cobranza generada por el sistema
    /// </summary>
    public class AlertaCobranza : BaseEntity
    {
        public int CreditoId { get; set; }
        public virtual Credito? Credito { get; set; }

        public int ClienteId { get; set; }
        public virtual Cliente? Cliente { get; set; }

        public TipoAlertaCobranza Tipo { get; set; }
        public PrioridadAlerta Prioridad { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal MontoVencido { get; set; }
        public int CuotasVencidas { get; set; }
        public DateTime FechaAlerta { get; set; }
        public bool Resuelta { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string? Observaciones { get; set; }
    }
}