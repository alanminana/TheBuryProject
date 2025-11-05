using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class MovimientoStockViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Producto")]
        public int ProductoId { get; set; }

        [Display(Name = "Producto")]
        public string? ProductoNombre { get; set; }

        [Display(Name = "Código")]
        public string? ProductoCodigo { get; set; }

        [Display(Name = "Tipo de Movimiento")]
        public TipoMovimiento Tipo { get; set; }

        [Display(Name = "Tipo")]
        public string? TipoNombre { get; set; }

        [Display(Name = "Cantidad")]
        public decimal Cantidad { get; set; }

        [Display(Name = "Stock Anterior")]
        public decimal StockAnterior { get; set; }

        [Display(Name = "Stock Nuevo")]
        public decimal StockNuevo { get; set; }

        [Display(Name = "Referencia")]
        public string? Referencia { get; set; }

        [Display(Name = "Orden de Compra")]
        public int? OrdenCompraId { get; set; }

        [Display(Name = "Orden de Compra")]
        public string? OrdenCompraNumero { get; set; }

        [Display(Name = "Motivo")]
        public string? Motivo { get; set; }

        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; }
    }

    public class MovimientoStockFilterViewModel
    {
        public int? ProductoId { get; set; }
        public TipoMovimiento? Tipo { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? OrderBy { get; set; }
        public string? OrderDirection { get; set; } = "desc";

        public IEnumerable<MovimientoStockViewModel> Movimientos { get; set; } = new List<MovimientoStockViewModel>();
        public int TotalResultados { get; set; }
    }
}