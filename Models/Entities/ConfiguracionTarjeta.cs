using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Configuración específica para pagos con tarjeta
    /// </summary>
    public class ConfiguracionTarjeta : DashboardDtos
    {
        public int ConfiguracionPagoId { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreTarjeta { get; set; } = string.Empty; // Ej: Visa, Mastercard, Cabal

        [Required]
        public TipoTarjeta TipoTarjeta { get; set; }

        public bool Activa { get; set; } = true;

        // Para tarjeta de crédito
        public bool PermiteCuotas { get; set; } = false;
        public int? CantidadMaximaCuotas { get; set; }
        public TipoCuotaTarjeta? TipoCuota { get; set; }
        public decimal? TasaInteresesMensual { get; set; } // Si tiene interés

        // Para tarjeta de débito
        public bool TieneRecargoDebito { get; set; } = false;
        public decimal? PorcentajeRecargoDebito { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Navigation
        public virtual ConfiguracionPago ConfiguracionPago { get; set; } = null!;
    }
}