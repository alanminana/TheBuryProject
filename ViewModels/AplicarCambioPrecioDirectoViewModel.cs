using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels
{
    public class AplicarCambioPrecioDirectoViewModel
    {
        [Required]
        public string Alcance { get; set; } // "seleccionados" | "filtrados"

        [Required]
        public decimal ValorPorcentaje { get; set; }

        public string ProductoIdsText { get; set; } // CSV de IDs si seleccionados

        public string FiltrosJson { get; set; } // JSON si filtrados

        public string Motivo { get; set; } // opcional
    }
}
