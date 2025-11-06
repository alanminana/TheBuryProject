using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class CreditoViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Número de Crédito")]
        public string Numero { get; set; } = string.Empty;

        // Cliente
        public int ClienteId { get; set; }

        [Display(Name = "Cliente")]
        public string? ClienteNombre { get; set; }

        [Display(Name = "Documento")]
        public string? ClienteDocumento { get; set; }

        // Montos
        [Display(Name = "Monto Solicitado")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontoSolicitado { get; set; }

        [Display(Name = "Monto Aprobado")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontoAprobado { get; set; }

        [Display(Name = "Monto Total a Pagar")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontoTotal { get; set; }

        // Cuotas
        [Display(Name = "Cantidad de Cuotas")]
        public int CantidadCuotas { get; set; }

        [Display(Name = "Monto por Cuota")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontoCuota { get; set; }

        [Display(Name = "Tasa de Interés (%)")]
        [DisplayFormat(DataFormatString = "{0:N2}%", ApplyFormatInEditMode = false)]
        public decimal TasaInteres { get; set; }

        // Estado
        [Display(Name = "Estado")]
        public EstadoCredito Estado { get; set; }

        [Display(Name = "Estado")]
        public string EstadoDescripcion => Estado.ToString();

        // Fechas
        [Display(Name = "Fecha de Solicitud")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime FechaSolicitud { get; set; }

        [Display(Name = "Fecha de Aprobación")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? FechaAprobacion { get; set; }

        [Display(Name = "Fecha de Desembolso")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? FechaDesembolso { get; set; }

        [Display(Name = "Fecha de Finalización")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? FechaFinalizacion { get; set; }

        // Evaluación
        [Display(Name = "Puntaje de Riesgo")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = false)]
        public decimal PuntajeRiesgoInicial { get; set; }

        [Display(Name = "Sueldo del Cliente")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal? SueldoCliente { get; set; }

        [Display(Name = "% del Sueldo")]
        [DisplayFormat(DataFormatString = "{0:N2}%", ApplyFormatInEditMode = false)]
        public decimal? PorcentajeSueldo { get; set; }

        [Display(Name = "Tiene Garante")]
        public bool TieneGarante { get; set; }

        public int? GaranteId { get; set; }

        [Display(Name = "Garante")]
        public string? GaranteNombre { get; set; }

        // Aprobación/Rechazo
        [Display(Name = "Aprobado Por")]
        public string? AprobadoPor { get; set; }

        [Display(Name = "Rechazado Por")]
        public string? RechazadoPor { get; set; }

        [Display(Name = "Fecha de Rechazo")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? FechaRechazo { get; set; }

        [Display(Name = "Motivo de Rechazo")]
        public string? MotivoRechazo { get; set; }

        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        // Calculados
        [Display(Name = "Cuotas Pagadas")]
        public int CuotasPagadas { get; set; }

        [Display(Name = "Cuotas Pendientes")]
        public int CuotasPendientes { get; set; }

        [Display(Name = "Monto Pagado")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontoPagado { get; set; }

        [Display(Name = "Saldo Pendiente")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal SaldoPendiente { get; set; }

        [Display(Name = "Días en Mora")]
        public int? DiasEnMora { get; set; }

        // Auditoría
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}