using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Filters;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers;

/// <summary>
/// Controller para gestión de roles y permisos del sistema
/// </summary>
[Authorize]
[PermisoRequerido(Modulo = "roles", Accion = "view")]
public class RolesController : Controller
{
    private readonly IRolService _rolService;
    private readonly ILogger<RolesController> _logger;

    private string? GetSafeReturnUrl(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    private IActionResult RedirectToReturnUrlOrIndex(string? returnUrl)
    {
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);
        return safeReturnUrl != null ? LocalRedirect(safeReturnUrl) : RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToReturnUrlOrDetails(string id, string? returnUrl)
    {
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);
        return safeReturnUrl != null ? LocalRedirect(safeReturnUrl) : RedirectToAction(nameof(Details), new { id });
    }

    public RolesController(
        IRolService rolService,
        ILogger<RolesController> logger)
    {
        _rolService = rolService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos los roles del sistema
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl)
    {
        try
        {
            var roles = await _rolService.GetAllRolesAsync();
            var roleUsageStats = await _rolService.GetRoleUsageStatsAsync();

            var viewModel = roles.Select(r => new RolViewModel
            {
                Id = r.Id,
                Nombre = r.Name!,
                CantidadUsuarios = roleUsageStats.GetValueOrDefault(r.Name!, 0)
            }).ToList();

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de roles");
            TempData["Error"] = "Error al cargar los roles";
            return View(new List<RolViewModel>());
        }
    }

    /// <summary>
    /// Muestra detalles de un rol con sus permisos
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(string id, string? returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        try
        {
            var role = await _rolService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var permisos = await _rolService.GetPermissionsForRoleAsync(id);
            var usuarios = await _rolService.GetUsersInRoleAsync(role.Name!);

            var viewModel = new RolDetalleViewModel
            {
                Id = role.Id,
                Nombre = role.Name!,
                Permisos = permisos.Select(p => new PermisoViewModel
                {
                    Id = p.Id,
                    ModuloNombre = p.Modulo.Nombre,
                    AccionNombre = p.Accion.Nombre,
                    ClaimValue = p.ClaimValue
                }).ToList(),
                Usuarios = usuarios.Select(u => new UsuarioBasicoViewModel
                {
                    Id = u.Id,
                    Email = u.Email!,
                    UserName = u.UserName!
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener detalles del rol {RoleId}", id);
            TempData["Error"] = "Error al cargar los detalles del rol";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Muestra formulario para crear un nuevo rol
    /// </summary>
    [HttpGet]
    [PermisoRequerido(Modulo = "roles", Accion = "create")]
    public IActionResult Create(string? returnUrl)
    {
        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
        return View(new CrearRolViewModel());
    }

    /// <summary>
    /// Crea un nuevo rol
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermisoRequerido(Modulo = "roles", Accion = "create")]
    public async Task<IActionResult> Create(CrearRolViewModel model, string? returnUrl)
    {
        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _rolService.CreateRoleAsync(model.Nombre);

            if (result.Succeeded)
            {
                _logger.LogInformation("Rol creado: {RoleName} por usuario {User}",
                    model.Nombre, User.Identity?.Name);
                TempData["Success"] = $"Rol '{model.Nombre}' creado exitosamente";
                return RedirectToReturnUrlOrIndex(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear rol {RoleName}", model.Nombre);
            ModelState.AddModelError(string.Empty, "Error al crear el rol");
        }

        return View(model);
    }

    /// <summary>
    /// Muestra formulario para editar un rol
    /// </summary>
    [HttpGet]
    [PermisoRequerido(Modulo = "roles", Accion = "update")]
    public async Task<IActionResult> Edit(string id, string? returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        try
        {
            var role = await _rolService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var viewModel = new EditarRolViewModel
            {
                Id = role.Id,
                Nombre = role.Name!
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar formulario de edición para rol {RoleId}", id);
            TempData["Error"] = "Error al cargar el formulario de edición";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Actualiza un rol
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermisoRequerido(Modulo = "roles", Accion = "update")]
    public async Task<IActionResult> Edit(EditarRolViewModel model, string? returnUrl)
    {
        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _rolService.UpdateRoleAsync(model.Id, model.Nombre);

            if (result.Succeeded)
            {
                _logger.LogInformation("Rol actualizado: {RoleId} -> {NuevoNombre} por usuario {User}",
                    model.Id, model.Nombre, User.Identity?.Name);
                TempData["Success"] = $"Rol '{model.Nombre}' actualizado exitosamente";
                return RedirectToReturnUrlOrDetails(model.Id, returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar rol {RoleId}", model.Id);
            ModelState.AddModelError(string.Empty, "Error al actualizar el rol");
        }

        return View(model);
    }

    /// <summary>
    /// Muestra formulario para confirmar eliminación de rol
    /// </summary>
    [HttpGet]
    [PermisoRequerido(Modulo = "roles", Accion = "delete")]
    public async Task<IActionResult> Delete(string id, string? returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        try
        {
            var role = await _rolService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var usuarios = await _rolService.GetUsersInRoleAsync(role.Name!);

            var viewModel = new EliminarRolViewModel
            {
                Id = role.Id,
                Nombre = role.Name!,
                CantidadUsuarios = usuarios.Count
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar formulario de eliminación para rol {RoleId}", id);
            TempData["Error"] = "Error al cargar el formulario de eliminación";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Elimina un rol
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [PermisoRequerido(Modulo = "roles", Accion = "delete")]
    public async Task<IActionResult> DeleteConfirmed(string id, string? returnUrl)
    {
        try
        {
            var result = await _rolService.DeleteRoleAsync(id);

            if (result.Succeeded)
            {
                _logger.LogInformation("Rol eliminado: {RoleId} por usuario {User}",
                    id, User.Identity?.Name);
                TempData["Success"] = "Rol eliminado exitosamente";
            }
            else
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar rol {RoleId}", id);
            TempData["Error"] = "Error al eliminar el rol";
        }

        return RedirectToReturnUrlOrIndex(returnUrl);
    }

    /// <summary>
    /// Muestra interfaz para asignar permisos a un rol
    /// </summary>
    [HttpGet]
    [PermisoRequerido(Modulo = "roles", Accion = "assignpermissions")]
    public async Task<IActionResult> AsignarPermisos(string id, string? returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        try
        {
            var role = await _rolService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var modulos = await _rolService.GetAllModulosAsync();
            var permisosActuales = await _rolService.GetPermissionsForRoleAsync(id);

            var viewModel = new AsignarPermisosViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name!,
                Modulos = modulos.Select(m => new ModuloConAccionesViewModel
                {
                    ModuloId = m.Id,
                    ModuloNombre = m.Nombre,
                    ModuloClave = m.Clave,
                    Categoria = m.Categoria ?? "General",
                    Icono = m.Icono ?? "bi-square",
                    Acciones = m.Acciones.Select(a => new AccionConEstadoViewModel
                    {
                        AccionId = a.Id,
                        AccionNombre = a.Nombre,
                        AccionClave = a.Clave,
                        Seleccionada = permisosActuales.Any(p => p.ModuloId == m.Id && p.AccionId == a.Id)
                    }).ToList()
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar interfaz de permisos para rol {RoleId}", id);
            TempData["Error"] = "Error al cargar la interfaz de permisos";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Asigna/remueve un permiso de un rol (AJAX)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermisoRequerido(Modulo = "roles", Accion = "assignpermissions")]
    public async Task<IActionResult> TogglePermiso(string roleId, int moduloId, int accionId, bool asignar)
    {
        try
        {
            if (asignar)
            {
                await _rolService.AssignPermissionToRoleAsync(roleId, moduloId, accionId);
            }
            else
            {
                await _rolService.RemovePermissionFromRoleAsync(roleId, moduloId, accionId);
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al toggle permiso para rol {RoleId}", roleId);
            return Json(new { success = false, message = "Error al modificar el permiso" });
        }
    }

    /// <summary>
    /// Muestra usuarios en un rol
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Usuarios(string id, string? returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        try
        {
            var role = await _rolService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var usuarios = await _rolService.GetUsersInRoleAsync(role.Name!);

            var viewModel = new UsuariosEnRolViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name!,
                Usuarios = usuarios.Select(u => new UsuarioBasicoViewModel
                {
                    Id = u.Id,
                    Email = u.Email!,
                    UserName = u.UserName!,
                    EmailConfirmed = u.EmailConfirmed
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios del rol {RoleId}", id);
            TempData["Error"] = "Error al cargar los usuarios del rol";
            return RedirectToAction(nameof(Index));
        }
    }
}