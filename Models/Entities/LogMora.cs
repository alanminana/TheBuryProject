using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    public class LogMora : BaseEntity
    {
        [Required]
        public DateTime FechaEjecucion { get; set; }

        public int CuotasProcesadas { get; set; }

        public int AlertasGeneradas { get; set; }

        public decimal TotalMora { get; set; }

        public bool Exitoso { get; set; } = true;

        [StringLength(1000)]
        public string? Errores { get; set; }

        public int DuracionSegundos { get; set; }
    }
}