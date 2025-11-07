
using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class VentaFilterViewModel
    {
        [Display(Name = "Número")]
        public string? Numero { get; set; }

        [Display(Name = "Cliente")]
        public string? Cliente { get; set; }

        [Display(Name = "Estado")]
        public EstadoVenta? Estado { get; set; }

        [Display(Name = "Tipo de Pago")]
        public TipoPago? TipoPago { get; set; }

        [Display(Name = "Desde")]
        [DataType(DataType.Date)]
        public DateTime? FechaDesde { get; set; }

        [Display(Name = "Hasta")]
        [DataType(DataType.Date)]
        public DateTime? FechaHasta { get; set; }

        [Display(Name = "Monto Mínimo")]
        public decimal? MontoMinimo { get; set; }

        [Display(Name = "Monto Máximo")]
        public decimal? MontoMaximo { get; set; }

        public string? OrderBy { get; set; }
        public string? OrderDirection { get; set; } = "desc";
    }
}