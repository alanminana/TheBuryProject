using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Filters;
using TheBuryProject.Models.Constants;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers;

/// <summary>
/// Controller para gestión de usuarios del sistema
/// </summary>
[Authorize(Roles = Roles.SuperAdmin + "," + Roles.Administrador)]
[PermisoRequerido(Modulo = "usuarios", Accion = "view")]
public class UsuariosController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppDbContext _context;
    private readonly IRolService _rolService;
    private readonly ILogger<UsuariosController> _logger;

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

    public UsuariosController(
        UserManager<IdentityUser> userManager,
        AppDbContext context,
        IRolService rolService,
        ILogger<UsuariosController> logger)
    {
        _userManager = userManager;
        _context = context;
        _rolService = rolService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos los usuarios del sistema
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl)
    {
        try
        {
            var users = await _context.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var rolesLookup = await _context.UserRoles
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, r.Name })
                .GroupBy(x => x.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Name!).ToList());

            var viewModels = users.Select(user => new UsuarioViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                Roles = rolesLookup.GetValueOrDefault(user.Id, new List<string>())
            }).ToList();

            return View(viewModels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de usuarios");
            TempData["Error"] = "Error al cargar los usuarios";
            return View(new List<UsuarioViewModel>());
        }
    }

    /// <summary>
    /// Muestra detalles de un usuario
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
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var permisos = await _rolService.GetUserEffectivePermissionsAsync(id);

            var viewModel = new UsuarioDetalleViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                Roles = roles.ToList(),
                Permisos = permisos
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener detalles del usuario {UserId}", id);
            TempData["Error"] = "Error al cargar los detalles del usuario";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Muestra formulario para crear un nuevo usuario
    /// </summary>
    [HttpGet]
    [PermisoRequerido(Modulo = "usuarios", Accion = "create")]
    public async Task<IActionResult> Create(string? returnUrl)
    {
        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
        await CargarRolesEnViewBag();
        return View(new CrearUsuarioViewModel());
    }

    /// <summary>
    /// Crea un nuevo usuario
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermisoRequerido(Modulo = "usuarios", Accion = "create")]
    public async Task<IActionResult> Create(CrearUsuarioViewModel model, string? returnUrl)
    {
        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        if (!ModelState.IsValid)
        {
            await CargarRolesEnViewBag();
            return View(model);
        }

        try
        {
            var user = new IdentityUser
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = model.EmailConfirmed
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Asignar roles seleccionados
                if (model.RolesSeleccionados != null && model.RolesSeleccionados.Any())
                {
                    foreach (var roleName in model.RolesSeleccionados)
                    {
                        await _userManager.AddToRoleAsync(user, roleName);
                    }
                }

                _logger.LogInformation("Usuario creado: {Email} por usuario {User}",
                    model.Email, User.Identity?.Name);
                TempData["Success"] = $"Usuario '{model.Email}' creado exitosamente";
                return RedirectToReturnUrlOrIndex(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear usuario {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Error al crear el usuario");
        }

        await CargarRolesEnViewBag();
        return View(model);
    }

    /// <summary>
    /// Muestra formulario para editar un usuario
    /// </summary>
    [HttpGet]
    [PermisoRequerido(Modulo = "usuarios", Accion = "update")]
    public async Task<IActionResult> Edit(string id, string? returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new EditarUsuarioViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar formulario de edición para usuario {UserId}", id);
            TempData["Error"] = "Error al cargar el formulario de edición";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Actualiza un usuario
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermisoRequerido(Modulo = "usuarios", Accion = "update")]
    public async Task<IActionResult> Edit(EditarUsuarioViewModel model, string? returnUrl)
    {
        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.UserName = model.UserName;
            user.EmailConfirmed = model.EmailConfirmed;
            user.LockoutEnabled = model.LockoutEnabled;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario actualizado: {UserId} por usuario {User}",
                    model.Id, User.Identity?.Name);
                TempData["Success"] = $"Usuario '{model.Email}' actualizado exitosamente";
                return RedirectToReturnUrlOrDetails(model.Id, returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario {UserId}", model.Id);
            ModelState.AddModelError(string.Empty, "Error al actualizar el usuario");
        }

        return View(model);
    }

    /// <summary>
    /// Muestra formulario para eliminar un usuario
    /// </summary>
    [HttpGet]
    [PermisoRequerido(Modulo = "usuarios", Accion = "delete")]
    public async Task<IActionResult> Delete(string id, string? returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new EliminarUsuarioViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles.ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar formulario de eliminación para usuario {UserId}", id);
            TempData["Error"] = "Error al cargar el formulario de eliminación";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Elimina un usuario
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [PermisoRequerido(Modulo = "usuarios", Accion = "delete")]
    public async Task<IActionResult> DeleteConfirmed(string id, string? returnUrl)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario eliminado: {UserId} por usuario {User}",
                    id, User.Identity?.Name);
                TempData["Success"] = "Usuario eliminado exitosamente";
            }
            else
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar usuario {UserId}", id);
            TempData["Error"] = "Error al eliminar el usuario";
        }

        return RedirectToReturnUrlOrIndex(returnUrl);
    }

    /// <summary>
    /// Muestra formulario para asignar roles a un usuario
    /// </summary>
    [HttpGet]
    [PermisoRequerido(Modulo = "usuarios", Accion = "assignroles")]
    public async Task<IActionResult> AsignarRoles(string id, string? returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _rolService.GetAllRolesAsync();

            var viewModel = new AsignarRolesUsuarioViewModel
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Roles = allRoles.Select(r => new RolCheckboxViewModel
                {
                    RoleId = r.Id,
                    RoleName = r.Name!,
                    Seleccionado = userRoles.Contains(r.Name!)
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar formulario de asignación de roles para usuario {UserId}", id);
            TempData["Error"] = "Error al cargar el formulario";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Asigna roles a un usuario
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermisoRequerido(Modulo = "usuarios", Accion = "assignroles")]
    public async Task<IActionResult> AsignarRoles(AsignarRolesUsuarioViewModel model, string? returnUrl)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles.Where(r => r.Seleccionado).Select(r => r.RoleName).ToList();

            // Remover roles no seleccionados
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            // Agregar roles seleccionados
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            _logger.LogInformation("Roles actualizados para usuario {UserId} por {User}",
                model.UserId, User.Identity?.Name);
            TempData["Success"] = "Roles asignados exitosamente";
            return RedirectToReturnUrlOrDetails(model.UserId, returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar roles a usuario {UserId}", model.UserId);
            TempData["Error"] = "Error al asignar roles";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Muestra formulario para cambiar contraseña de usuario
    /// </summary>
    [HttpGet]
    [PermisoRequerido(Modulo = "usuarios", Accion = "resetpassword")]
    public async Task<IActionResult> CambiarPassword(string id, string? returnUrl)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new CambiarPasswordUsuarioViewModel
            {
                UserId = user.Id,
                UserName = user.UserName!
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar formulario de cambio de contraseña para usuario {UserId}", id);
            TempData["Error"] = "Error al cargar el formulario";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Cambia la contraseña de un usuario
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermisoRequerido(Modulo = "usuarios", Accion = "resetpassword")]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordUsuarioViewModel model, string? returnUrl)
    {
        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            // Remove current password and set new one
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Contraseña cambiada para usuario {UserId} por {User}",
                    model.UserId, User.Identity?.Name);
                TempData["Success"] = "Contraseña cambiada exitosamente";
                return RedirectToReturnUrlOrDetails(model.UserId, returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar contraseña de usuario {UserId}", model.UserId);
            ModelState.AddModelError(string.Empty, "Error al cambiar la contraseña");
        }

        return View(model);
    }

    /// <summary>
    /// Carga la lista de roles en ViewBag
    /// </summary>
    private async Task CargarRolesEnViewBag()
    {
        var roles = await _rolService.GetAllRolesAsync();
        ViewBag.Roles = roles.Select(r => r.Name).ToList();
    }
}