using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels;

/// <summary>
/// ViewModel para mostrar un usuario en listas
/// </summary>
public class UsuarioViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool Activo { get; set; } = true;
}

/// <summary>
/// ViewModel para crear un nuevo usuario
/// </summary>
public class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [StringLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
    [Display(Name = "Nombre de Usuario")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Email Confirmado")]
    public bool EmailConfirmed { get; set; } = true; // Por defecto confirmado para evitar problemas de login

    [Display(Name = "Roles")]
    public List<string> RolesSeleccionados { get; set; } = new();
}

/// <summary>
/// ViewModel para editar un usuario
/// </summary>
public class EditarUsuarioViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [StringLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
    [Display(Name = "Nombre de Usuario")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Email Confirmado")]
    public bool EmailConfirmed { get; set; }

    [Display(Name = "Cuenta Bloqueada")]
    public bool LockoutEnabled { get; set; }
}

/// <summary>
/// ViewModel para asignar roles a un usuario
/// </summary>
public class AsignarRolesUsuarioViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<RolCheckboxViewModel> Roles { get; set; } = new();
}

/// <summary>
/// ViewModel para checkbox de rol
/// </summary>
public class RolCheckboxViewModel
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool Seleccionado { get; set; }
}

/// <summary>
/// ViewModel para mostrar detalles de un usuario
/// </summary>
public class UsuarioDetalleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool Activo { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permisos { get; set; } = new();
}

/// <summary>
/// ViewModel para eliminar un usuario
/// </summary>
public class EliminarUsuarioViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// ViewModel para cambiar contraseña de usuario
/// </summary>
public class CambiarPasswordUsuarioViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Nueva Contraseña")]
    [Compare("NewPassword", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}