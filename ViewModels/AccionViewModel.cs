using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels;

/// <summary>
/// ViewModel para mostrar una acción en listas
/// </summary>
public class AccionViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ModuloNombre { get; set; }
    public int? ModuloId { get; set; }
    public bool Activo { get; set; }
}

/// <summary>
/// ViewModel para crear una nueva acción
/// </summary>
public class CrearAccionViewModel
{
    [Required(ErrorMessage = "El nombre de la acción es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    [Display(Name = "Nombre de la Acción")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La clave de la acción es requerida")]
    [StringLength(50, ErrorMessage = "La clave no puede exceder 50 caracteres")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "La clave solo puede contener letras minúsculas, números y guiones")]
    [Display(Name = "Clave")]
    public string Clave { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La descripción no puede exceder 200 caracteres")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un módulo")]
    [Display(Name = "Módulo")]
    public int ModuloId { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}

/// <summary>
/// ViewModel para editar una acción
/// </summary>
public class EditarAccionViewModel
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la acción es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    [Display(Name = "Nombre de la Acción")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La clave de la acción es requerida")]
    [StringLength(50, ErrorMessage = "La clave no puede exceder 50 caracteres")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "La clave solo puede contener letras minúsculas, números y guiones")]
    [Display(Name = "Clave")]
    public string Clave { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La descripción no puede exceder 200 caracteres")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un módulo")]
    [Display(Name = "Módulo")]
    public int ModuloId { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; }
}

/// <summary>
/// ViewModel para mostrar detalles de una acción
/// </summary>
public class AccionDetalleViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int ModuloId { get; set; }
    public string ModuloNombre { get; set; } = string.Empty;
    public string ModuloClave { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// ViewModel para eliminar una acción
/// </summary>
public class EliminarAccionViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string ModuloNombre { get; set; } = string.Empty;
}