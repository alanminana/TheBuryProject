using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class AlertaCobranzaViewModel
    {
        public int Id { get; set; }

        public int CuotaId { get; set; }

        public int CreditoId { get; set; }

        public int ClienteId { get; set; }

        [Display(Name = "Tipo de Alerta")]
        public TipoAlerta Tipo { get; set; }

        [Required]
        [Display(Name = "Título")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Mensaje")]
        [StringLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [Display(Name = "Prioridad")]
        [Range(1, 3)]
        public int Prioridad { get; set; } = 1;

        [Display(Name = "Leída")]
        public bool Leida { get; set; } = false;

        [Display(Name = "Resuelta")]
        public bool Resuelta { get; set; } = false;

        // Propiedades adicionales para la vista
        public string ClienteNombre { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}