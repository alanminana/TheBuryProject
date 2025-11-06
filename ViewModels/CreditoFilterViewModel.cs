using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class CreditoFilterViewModel
    {
        [Display(Name = "Buscar")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Cliente")]
        public int? ClienteId { get; set; }

        [Display(Name = "Estado")]
        public EstadoCredito? Estado { get; set; }

        [Display(Name = "Fecha Desde")]
        [DataType(DataType.Date)]
        public DateTime? FechaDesde { get; set; }

        [Display(Name = "Fecha Hasta")]
        [DataType(DataType.Date)]
        public DateTime? FechaHasta { get; set; }

        [Display(Name = "Monto Mínimo")]
        public decimal? MontoMinimo { get; set; }

        [Display(Name = "Monto Máximo")]
        public decimal? MontoMaximo { get; set; }

        [Display(Name = "Solo en Mora")]
        public bool SoloEnMora { get; set; }

        [Display(Name = "Ordenar por")]
        public string OrderBy { get; set; } = "FechaSolicitud";

        [Display(Name = "Dirección")]
        public string OrderDirection { get; set; } = "DESC";

        // Resultados
        public IEnumerable<CreditoViewModel> Results { get; set; } = new List<CreditoViewModel>();

        // Dropdowns
        public SelectList? Clientes { get; set; }
        public SelectList? Estados { get; set; }
    }
}