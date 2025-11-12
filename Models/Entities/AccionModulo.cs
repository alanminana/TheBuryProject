using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities;

/// <summary>
/// Representa una acción disponible en un módulo del sistema
/// </summary>
public class AccionModulo : BaseEntity
{
    /// <summary>
    /// ID del módulo al que pertenece esta acción
    /// </summary>
    [Required]
    public int ModuloId { get; set; }

    /// <summary>
    /// Nombre de la acción (Ver, Crear, Editar, Eliminar, etc.)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Clave única de la acción (view, create, update, delete, authorize, etc.)
    /// Usar minúsculas sin espacios
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Clave { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la acción
    /// </summary>
    [StringLength(500)]
    public string? Descripcion { get; set; }

    /// <summary>
    /// Icono de la acción (Bootstrap Icons)
    /// </summary>
    [StringLength(50)]
    public string? Icono { get; set; }

    /// <summary>
    /// Orden de visualización
    /// </summary>
    public int Orden { get; set; }

    /// <summary>
    /// Indica si la acción está activa
    /// </summary>
    public bool Activa { get; set; } = true;

    // Navegación
    public virtual ModuloSistema Modulo { get; set; } = null!;
    public virtual ICollection<RolPermiso> Permisos { get; set; } = new List<RolPermiso>();
}