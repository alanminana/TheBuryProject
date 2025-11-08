using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    public class ConfiguracionMora : BaseEntity
    {
        [Required]
        public decimal TasaMoraDiaria { get; set; } = 0.05m;

        [Required]
        public int DiasGraciaMora { get; set; } = 3;

        [Required]
        public decimal PorcentajeRecargoPrimerMes { get; set; } = 5.0m;

        [Required]
        public decimal PorcentajeRecargoSegundoMes { get; set; } = 10.0m;

        [Required]
        public decimal PorcentajeRecargoTercerMes { get; set; } = 15.0m;

        [Required]
        public int DiasAntesAlertaVencimiento { get; set; } = 7;

        public bool JobActivo { get; set; } = true;

        public TimeSpan HoraEjecucion { get; set; } = new TimeSpan(2, 0, 0);

        public DateTime? UltimaEjecucion { get; set; }
    }
}