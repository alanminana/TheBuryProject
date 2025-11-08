using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities
{
    public class AlertaCobranza : BaseEntity
    {
        public int CuotaId { get; set; }

        public int CreditoId { get; set; }

        public int ClienteId { get; set; }

        public TipoAlerta Tipo { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        public int Prioridad { get; set; } = 1;

        public bool Leida { get; set; } = false;

        public bool Resuelta { get; set; } = false;

        // Navigation Properties
        public virtual Cuota Cuota { get; set; } = null!;
        public virtual Credito Credito { get; set; } = null!;
        public virtual Cliente Cliente { get; set; } = null!;
    }
}