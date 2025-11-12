using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities;

/// <summary>
/// Representa un módulo del sistema con sus acciones disponibles
/// </summary>
public class ModuloSistema : BaseEntity
{
    /// <summary>
    /// Nombre del módulo (Productos, Ventas, Clientes, etc.)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Clave única del módulo (productos, ventas, clientes)
    /// Usar minúsculas sin espacios
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Clave { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del módulo
    /// </summary>
    [StringLength(500)]
    public string? Descripcion { get; set; }

    /// <summary>
    /// Icono del módulo (Bootstrap Icons)
    /// Ejemplo: "bi-box-seam", "bi-cart"
    /// </summary>
    [StringLength(50)]
    public string? Icono { get; set; }

    /// <summary>
    /// Orden de visualización en el menú
    /// </summary>
    public int Orden { get; set; }

    /// <summary>
    /// Indica si el módulo está activo
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Categoría del módulo (Catálogo, Ventas, Compras, Configuración, etc.)
    /// </summary>
    [StringLength(50)]
    public string? Categoria { get; set; }

    // Navegación
    public virtual ICollection<AccionModulo> Acciones { get; set; } = new List<AccionModulo>();
    public virtual ICollection<RolPermiso> Permisos { get; set; } = new List<RolPermiso>();
}