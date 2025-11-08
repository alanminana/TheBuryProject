using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels
{
    public class ConfiguracionMoraViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = "Configuración de Mora";

        [Display(Name = "Tasa de Mora Diaria (%)")]
        [Required(ErrorMessage = "La tasa de mora es requerida")]
        [Range(0, 100, ErrorMessage = "La tasa debe estar entre 0% y 100%")]
        public decimal TasaMoraDiaria { get; set; } = 0.05m;

        [Display(Name = "Días de Gracia")]
        [Required(ErrorMessage = "Los días de gracia son requeridos")]
        [Range(0, 30, ErrorMessage = "Los días de gracia deben estar entre 0 y 30")]
        public int DiasGraciaMora { get; set; } = 3;

        [Display(Name = "Recargo Primer Mes (%)")]
        [Required(ErrorMessage = "El recargo es requerido")]
        [Range(0, 100, ErrorMessage = "El recargo debe estar entre 0% y 100%")]
        public decimal PorcentajeRecargoPrimerMes { get; set; } = 5.0m;

        [Display(Name = "Recargo Segundo Mes (%)")]
        [Required(ErrorMessage = "El recargo es requerido")]
        [Range(0, 100, ErrorMessage = "El recargo debe estar entre 0% y 100%")]
        public decimal PorcentajeRecargoSegundoMes { get; set; } = 10.0m;

        [Display(Name = "Recargo Tercer Mes en Adelante (%)")]
        [Required(ErrorMessage = "El recargo es requerido")]
        [Range(0, 100, ErrorMessage = "El recargo debe estar entre 0% y 100%")]
        public decimal PorcentajeRecargoTercerMes { get; set; } = 15.0m;

        [Display(Name = "Días de Anticipación para Alertas")]
        [Required(ErrorMessage = "Los días de anticipación son requeridos")]
        [Range(1, 30, ErrorMessage = "Los días deben estar entre 1 y 30")]
        public int DiasAntesAlertaVencimiento { get; set; } = 7;

        [Display(Name = "Enviar Email de Alerta")]
        public bool EnviarEmailAlerta { get; set; } = false;

        [Display(Name = "Enviar SMS de Alerta")]
        public bool EnviarSMSAlerta { get; set; } = false;

        [Display(Name = "Job Activo")]
        public bool JobActivo { get; set; } = true;

        [Display(Name = "Hora de Ejecución")]
        [Required(ErrorMessage = "La hora de ejecución es requerida")]
        public TimeSpan HoraEjecucion { get; set; } = new TimeSpan(2, 0, 0);

        [Display(Name = "Última Ejecución")]
        public DateTime? UltimaEjecucion { get; set; }

        [Display(Name = "Último Resultado")]
        public string? UltimoResultado { get; set; }

        [Display(Name = "Observaciones")]
        [DataType(DataType.MultilineText)]
        public string? Observaciones { get; set; }
    }
}