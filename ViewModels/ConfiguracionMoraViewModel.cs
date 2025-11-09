using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels
{
    public class ConfiguracionMoraViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tasa de Mora Diaria (%)")]
        [Range(0, 100)]
        public decimal TasaMoraDiaria { get; set; } = 0.05m;

        [Required]
        [Display(Name = "Días de Gracia")]
        [Range(0, 30)]
        public int DiasGraciaMora { get; set; } = 3;

        [Required]
        [Display(Name = "Recargo Primer Mes (%)")]
        [Range(0, 100)]
        public decimal PorcentajeRecargoPrimerMes { get; set; } = 5.0m;

        [Required]
        [Display(Name = "Recargo Segundo Mes (%)")]
        [Range(0, 100)]
        public decimal PorcentajeRecargoSegundoMes { get; set; } = 10.0m;

        [Required]
        [Display(Name = "Recargo Tercer Mes (%)")]
        [Range(0, 100)]
        public decimal PorcentajeRecargoTercerMes { get; set; } = 15.0m;

        [Required]
        [Display(Name = "Días Antes de Alerta")]
        [Range(1, 30)]
        public int DiasAntesAlertaVencimiento { get; set; } = 7;

        [Display(Name = "Job Activo")]
        public bool JobActivo { get; set; } = true;

        [Required]
        [Display(Name = "Hora de Ejecución")]
        public TimeSpan HoraEjecucion { get; set; } = new TimeSpan(2, 0, 0);

        public DateTime? UltimaEjecucion { get; set; }
    }
}