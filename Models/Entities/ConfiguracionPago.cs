using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Configuraci�n general de tipo de pago
    /// </summary>
    public class ConfiguracionPago  : AuditableEntity
    {
        [Required]
        public TipoPago TipoPago { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        // Descuento
        public bool PermiteDescuento { get; set; } = false;
        public decimal? PorcentajeDescuentoMaximo { get; set; }

        // Recargo
        public bool TieneRecargo { get; set; } = false;
        public decimal? PorcentajeRecargo { get; set; }

        // Relaciones espec�ficas
        public virtual ICollection<ConfiguracionTarjeta> ConfiguracionesTarjeta { get; set; } = new List<ConfiguracionTarjeta>();
    }
}