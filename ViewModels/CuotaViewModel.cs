using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class CuotaViewModel
    {
        public int Id { get; set; }

        public int CreditoId { get; set; }

        [Display(Name = "Número de Crédito")]
        public string? CreditoNumero { get; set; }

        [Display(Name = "N° Cuota")]
        public int NumeroCuota { get; set; }

        [Display(Name = "Fecha de Vencimiento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaVencimiento { get; set; }

        [Display(Name = "Monto Original")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal MontoOriginal { get; set; }

        [Display(Name = "Monto Pendiente")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal MontoPendiente { get; set; }

        [Display(Name = "Monto Pagado")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal MontoPagado { get; set; }

        [Display(Name = "Fecha de Pago")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? FechaPago { get; set; }

        [Display(Name = "Estado")]
        public EstadoCuota Estado { get; set; }

        [Display(Name = "Estado")]
        public string EstadoDescripcion => Estado.ToString();

        [Display(Name = "Días de Atraso")]
        public int? DiasAtraso { get; set; }

        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        // Calculados
        [Display(Name = "¿Vencida?")]
        public bool EstaVencida => FechaVencimiento < DateTime.Now && Estado != EstadoCuota.Pagada;

        [Display(Name = "¿En Mora?")]
        public bool EstaEnMora => Estado == EstadoCuota.EnMora || Estado == EstadoCuota.Vencida;
    }
}