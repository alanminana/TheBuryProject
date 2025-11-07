using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class ConfiguracionPagoViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Tipo de Pago")]
        [Required(ErrorMessage = "El tipo de pago es requerido")]
        public TipoPago TipoPago { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Permite Descuento")]
        public bool PermiteDescuento { get; set; } = false;

        [Display(Name = "% Descuento Máximo")]
        [Range(0, 100)]
        public decimal? PorcentajeDescuentoMaximo { get; set; }

        [Display(Name = "Tiene Recargo")]
        public bool TieneRecargo { get; set; } = false;

        [Display(Name = "% Recargo")]
        [Range(0, 100)]
        public decimal? PorcentajeRecargo { get; set; }

        public List<ConfiguracionTarjetaViewModel> ConfiguracionesTarjeta { get; set; } = new List<ConfiguracionTarjetaViewModel>();
    }
}