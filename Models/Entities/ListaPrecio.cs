using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Models.Entities;

/// <summary>
/// Representa una lista de precios del sistema
/// Ejemplos: "Contado", "Tarjeta 3 cuotas", "Mayorista"
/// </summary>
public class ListaPrecio : BaseEntity
{
    /// <summary>
    /// Nombre de la lista de precios
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Código único de la lista
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de lista de precios
    /// </summary>
    [Required]
    public TipoListaPrecio Tipo { get; set; }

    /// <summary>
    /// Descripción de la lista y sus reglas
    /// </summary>
    [StringLength(500)]
    public string? Descripcion { get; set; }

    /// <summary>
    /// Porcentaje de margen sobre costo (para cálculo automático)
    /// </summary>
    public decimal? MargenPorcentaje { get; set; }

    /// <summary>
    /// Recargo adicional sobre precio base (ej: 10% para tarjeta)
    /// </summary>
    public decimal? RecargoPorcentaje { get; set; }

    /// <summary>
    /// Cantidad de cuotas (para listas de tarjeta)
    /// </summary>
    public int? CantidadCuotas { get; set; }

    /// <summary>
    /// Indica si esta lista está activa
    /// </summary>
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Indica si es la lista predeterminada del sistema
    /// </summary>
    public bool EsPredeterminada { get; set; } = false;

    /// <summary>
    /// Orden de visualización en el sistema
    /// </summary>
    public int Orden { get; set; }

    /// <summary>
    /// Reglas adicionales en formato JSON (flexible para futuras extensiones)
    /// Ejemplo: {"redondeo": "centena", "margenMinimo": 25}
    /// </summary>
    public string? ReglasJson { get; set; }

    // Navegación
    public virtual ICollection<ProductoPrecioLista> Precios { get; set; } = new List<ProductoPrecioLista>();
}