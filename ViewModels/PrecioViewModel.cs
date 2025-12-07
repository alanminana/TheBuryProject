using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels;

/// <summary>
/// ViewModel para lista de precios
/// </summary>
public class ListaPrecioViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public TipoListaPrecio Tipo { get; set; }
    public string TipoDisplay { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? MargenPorcentaje { get; set; }
    public decimal? RecargoPorcentaje { get; set; }
    public decimal? MargenMinimoPorcentaje { get; set; }
    public int? CantidadCuotas { get; set; }
    public string? ReglaRedondeo { get; set; }
    public string? ReglasJson { get; set; }
    public string? Notas { get; set; }
    public bool Activa { get; set; }
    public bool EsPredeterminada { get; set; }
    public int CantidadProductos { get; set; }
}

/// <summary>
/// ViewModel para crear lista de precios
/// </summary>
public class CrearListaPrecioViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    [Display(Name = "Nombre de la Lista")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(20, ErrorMessage = "El código no puede exceder 20 caracteres")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo es requerido")]
    [Display(Name = "Tipo de Lista")]
    public TipoListaPrecio Tipo { get; set; }

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(0, 999.99)]
    [Display(Name = "Margen % sobre Costo")]
    public decimal? MargenPorcentaje { get; set; }

    [Range(0, 999.99)]
    [Display(Name = "Recargo % Adicional")]
    public decimal? RecargoPorcentaje { get; set; }

    [Range(0, 999.99)]
    [Display(Name = "Margen Mínimo %")]
    public decimal? MargenMinimoPorcentaje { get; set; }

    [Range(1, 60)]
    [Display(Name = "Cantidad de Cuotas")]
    public int? CantidadCuotas { get; set; }

    [StringLength(20, ErrorMessage = "La regla de redondeo no puede exceder 20 caracteres")]
    [Display(Name = "Regla de Redondeo")]
    public string? ReglaRedondeo { get; set; }

    [Display(Name = "Reglas JSON")]
    public string? ReglasJson { get; set; }

    [StringLength(1000)]
    [Display(Name = "Notas")]
    public string? Notas { get; set; }

    [Display(Name = "Activa")]
    public bool Activa { get; set; } = true;

    [Display(Name = "Predeterminada")]
    public bool EsPredeterminada { get; set; } = false;
}

/// <summary>
/// ViewModel para editar lista de precios
/// </summary>
public class EditarListaPrecioViewModel : CrearListaPrecioViewModel
{
    [Required]
    public int Id { get; set; }
}

/// <summary>
/// ViewModel para mostrar precio de un producto
/// </summary>
public class ProductoPrecioViewModel
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public string ListaNombre { get; set; } = string.Empty;
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public decimal MargenPorcentaje { get; set; }
    public decimal MargenValor { get; set; }
    public DateTime VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public bool EsVigente { get; set; }
    public bool EsManual { get; set; }
}

/// <summary>
/// ViewModel para el historial de precios de un producto
/// </summary>
public class HistorialPreciosViewModel
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int? ListaId { get; set; }
    public List<PrecioHistorialItemViewModel> Precios { get; set; } = new();
}

/// <summary>
/// ViewModel para item de historial de precio
/// </summary>
public class PrecioHistorialItemViewModel
{
    public int ListaId { get; set; }
    public string ListaNombre { get; set; } = string.Empty;
    public DateTime VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public decimal MargenPorcentaje { get; set; }
    public bool EsManual { get; set; }
    public bool EsVigente { get; set; }
    public string? CreadoPor { get; set; }
    public string? Notas { get; set; }
}

/// <summary>
/// ViewModel para simular cambio masivo de precios
/// </summary>
public class SimularCambioMasivoViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200)]
    [Display(Name = "Nombre del Cambio")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Tipo de Cambio")]
    public TipoCambio TipoCambio { get; set; }

    [Required]
    [Display(Name = "Tipo de Aplicación")]
    public TipoAplicacion TipoAplicacion { get; set; }

    [Required]
    [Display(Name = "Valor del Cambio")]
    public decimal ValorCambio { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Debe seleccionar al menos una lista")]
    [Display(Name = "Listas Afectadas")]
    public List<int> ListasIds { get; set; } = new();

    [Display(Name = "Categorías Específicas")]
    public List<int>? CategoriasIds { get; set; }

    [Display(Name = "Marcas Específicas")]
    public List<int>? MarcasIds { get; set; }

    [Display(Name = "Productos Específicos")]
    public List<int>? ProductosIds { get; set; }

    [Display(Name = "Fecha de Vigencia")]
    public DateTime? FechaVigencia { get; set; }

    [StringLength(1000)]
    [Display(Name = "Notas")]
    public string? Notas { get; set; }
}

/// <summary>
/// ViewModel para mostrar simulación de cambio
/// </summary>
public class SimulacionViewModel
{
    public int BatchId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoCambio TipoCambio { get; set; }
    public TipoAplicacion TipoAplicacion { get; set; }
    public decimal ValorCambio { get; set; }
    public EstadoBatch Estado { get; set; }
    public int CantidadProductos { get; set; }
    public decimal? PorcentajePromedioCambio { get; set; }
    public string SolicitadoPor { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public bool RequiereAutorizacion { get; set; }
    public List<SimulacionItemViewModel> Items { get; set; } = new();
    public Dictionary<string, object>? Estadisticas { get; set; }
    public int PaginaActual { get; set; }
    public int TamanioPagina { get; set; }
}

/// <summary>
/// ViewModel para item de simulación
/// </summary>
public class SimulacionItemViewModel
{
    public int Id { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int ListaId { get; set; }
    public string ListaNombre { get; set; } = string.Empty;
    public decimal? Costo { get; set; }
    public decimal PrecioAnterior { get; set; }
    public decimal PrecioNuevo { get; set; }
    public decimal DiferenciaValor { get; set; }
    public decimal DiferenciaPorcentaje { get; set; }
    public decimal? MargenAnterior { get; set; }
    public decimal? MargenNuevo { get; set; }
    public bool TieneAdvertencia { get; set; }
    public string? MensajeAdvertencia { get; set; }
}

/// <summary>
/// ViewModel para aprobar/rechazar batch
/// </summary>
public class AutorizarBatchViewModel
{
    public int BatchId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoCambio TipoCambio { get; set; }
    public decimal ValorCambio { get; set; }
    public int CantidadProductos { get; set; }
    public decimal? PorcentajePromedioCambio { get; set; }
    public string SolicitadoPor { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
}

/// <summary>
/// ViewModel para aplicar batch
/// </summary>
public class AplicarBatchViewModel
{
    public int BatchId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadProductos { get; set; }
    public string? AprobadoPor { get; set; }
    public DateTime? FechaAprobacion { get; set; }

    [Display(Name = "Fecha de Vigencia")]
    public DateTime? FechaVigencia { get; set; }
}

/// <summary>
/// ViewModel para revertir batch
/// </summary>
public class RevertirBatchViewModel
{
    public int BatchId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadProductos { get; set; }
    public string? AplicadoPor { get; set; }
    public DateTime? FechaAplicacion { get; set; }
    public DateTime? FechaVigencia { get; set; }

    [Required(ErrorMessage = "Debe indicar el motivo de la reversión")]
    [StringLength(500)]
    [Display(Name = "Motivo de Reversión")]
    public string Motivo { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel para un batch individual
/// </summary>
public class BatchViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoCambio TipoCambio { get; set; }
    public string TipoCambioDisplay { get; set; } = string.Empty;
    public TipoAplicacion TipoAplicacion { get; set; }
    public string TipoAplicacionDisplay { get; set; } = string.Empty;
    public decimal ValorCambio { get; set; }
    public EstadoBatch Estado { get; set; }
    public string EstadoDisplay { get; set; } = string.Empty;
    public int CantidadProductos { get; set; }
    public decimal? PorcentajePromedioCambio { get; set; }
    public string SolicitadoPor { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public string? AprobadoPor { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? AplicadoPor { get; set; }
    public DateTime? FechaAplicacion { get; set; }
    public bool RequiereAutorizacion { get; set; }
}

/// <summary>
/// ViewModel para lista de batches
/// </summary>
public class BatchListViewModel
{
    public List<BatchViewModel> Batches { get; set; } = new();
    public EstadoBatch? EstadoFiltro { get; set; }
    public int PaginaActual { get; set; } = 1;
    public int TamanioPagina { get; set; } = 20;
    public int TotalItems { get; set; }
}