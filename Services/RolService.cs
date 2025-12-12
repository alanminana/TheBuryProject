using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services;

/// <summary>
/// Implementación del servicio de roles y permisos
/// </summary>
public class RolService : IRolService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppDbContext _context;

    public RolService(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        AppDbContext context)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
    }

    #region Gestión de Roles

    public async Task<List<IdentityRole>> GetAllRolesAsync()
    {
        return await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<IdentityRole?> GetRoleByIdAsync(string roleId)
    {
        return await _roleManager.FindByIdAsync(roleId);
    }

    public async Task<IdentityRole?> GetRoleByNameAsync(string roleName)
    {
        return await _roleManager.FindByNameAsync(roleName);
    }

    public async Task<IdentityResult> CreateRoleAsync(string roleName)
    {
        var role = new IdentityRole(roleName);
        return await _roleManager.CreateAsync(role);
    }

    public async Task<IdentityResult> UpdateRoleAsync(string roleId, string newRoleName)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Rol no encontrado" });
        }

        role.Name = newRoleName;
        role.NormalizedName = newRoleName.ToUpper();
        return await _roleManager.UpdateAsync(role);
    }

    public async Task<IdentityResult> DeleteRoleAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Rol no encontrado" });
        }

        // Verificar que no haya usuarios con este rol
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Any())
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = $"No se puede eliminar el rol porque tiene {usersInRole.Count} usuario(s) asignado(s)"
            });
        }

        // Eliminar permisos asociados
        await ClearPermissionsForRoleAsync(roleId);

        return await _roleManager.DeleteAsync(role);
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await _roleManager.RoleExistsAsync(roleName);
    }

    #endregion

    #region Gestión de Permisos

    public async Task<List<RolPermiso>> GetPermissionsForRoleAsync(string roleId)
    {
        return await _context.RolPermisos
            .Include(rp => rp.Modulo)
            .Include(rp => rp.Accion)
            .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
            .OrderBy(rp => rp.Modulo.Orden)
            .ThenBy(rp => rp.Accion.Orden)
            .ToListAsync();
    }

    public async Task<RolPermiso> AssignPermissionToRoleAsync(string roleId, int moduloId, int accionId)
    {
        // Verificar si ya existe
        var existente = await _context.RolPermisos
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId &&
                                      rp.ModuloId == moduloId &&
                                      rp.AccionId == accionId);

        if (existente != null)
        {
            if (existente.IsDeleted)
            {
                existente.IsDeleted = false;
                await _context.SaveChangesAsync();
            }
            return existente;
        }

        // Obtener módulo y acción
        var modulo = await _context.ModulosSistema.FindAsync(moduloId);
        var accion = await _context.AccionesModulo.FindAsync(accionId);

        if (modulo == null || accion == null)
        {
            throw new InvalidOperationException("Módulo o acción no encontrados");
        }

        var rolPermiso = new RolPermiso
        {
            RoleId = roleId,
            ModuloId = moduloId,
            AccionId = accionId,
            ClaimValue = $"{modulo.Clave}.{accion.Clave}"
        };

        _context.RolPermisos.Add(rolPermiso);
        await _context.SaveChangesAsync();

        // Sincronizar claims
        await SyncRoleClaimsAsync(roleId);

        return rolPermiso;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(string roleId, int moduloId, int accionId)
    {
        var permiso = await _context.RolPermisos
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId &&
                                      rp.ModuloId == moduloId &&
                                      rp.AccionId == accionId &&
                                      !rp.IsDeleted);

        if (permiso == null) return false;

        permiso.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Sincronizar claims
        await SyncRoleClaimsAsync(roleId);

        return true;
    }

    public async Task ClearPermissionsForRoleAsync(string roleId)
    {
        var permisos = await _context.RolPermisos
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        foreach (var permiso in permisos)
        {
            permiso.IsDeleted = true;
        }

        await _context.SaveChangesAsync();

        // Sincronizar claims
        await SyncRoleClaimsAsync(roleId);
    }

    public async Task<bool> RoleHasPermissionAsync(string roleId, string moduloClave, string accionClave)
    {
        var claimValue = $"{moduloClave}.{accionClave}";
        return await _context.RolPermisos
            .AnyAsync(rp => rp.RoleId == roleId &&
                           rp.ClaimValue == claimValue &&
                           !rp.IsDeleted);
    }

    public async Task<List<RolPermiso>> AssignMultiplePermissionsAsync(string roleId, List<(int moduloId, int accionId)> permisos)
    {
        var result = new List<RolPermiso>();

        foreach (var (moduloId, accionId) in permisos)
        {
            var permiso = await AssignPermissionToRoleAsync(roleId, moduloId, accionId);
            result.Add(permiso);
        }

        return result;
    }

    public async Task SyncRoleClaimsAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return;

        // Obtener claims actuales del rol
        var currentClaims = await _roleManager.GetClaimsAsync(role);

        // Obtener permisos actuales de la BD
        var permisos = await GetPermissionsForRoleAsync(roleId);
        var permisoClaims = permisos.Select(p => p.ClaimValue).ToHashSet();

        // Eliminar claims que ya no existen en permisos
        foreach (var claim in currentClaims.Where(c => c.Type == "Permission"))
        {
            if (!permisoClaims.Contains(claim.Value))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }
        }

        // Agregar claims que faltan
        var existingClaimValues = currentClaims.Where(c => c.Type == "Permission").Select(c => c.Value).ToHashSet();
        foreach (var claimValue in permisoClaims)
        {
            if (!existingClaimValues.Contains(claimValue))
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", claimValue));
            }
        }
    }

    #endregion

    #region Gestión de Usuarios en Roles

    public async Task<List<IdentityUser>> GetUsersInRoleAsync(string roleName)
    {
        return (await _userManager.GetUsersInRoleAsync(roleName)).ToList();
    }

    public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Usuario no encontrado" });
        }

        return await _userManager.AddToRoleAsync(user, roleName);
    }

    public async Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Usuario no encontrado" });
        }

        return await _userManager.RemoveFromRoleAsync(user, roleName);
    }

    public async Task<List<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<string>();

        return (await _userManager.GetRolesAsync(user)).ToList();
    }

    public async Task<bool> UserIsInRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        return await _userManager.IsInRoleAsync(user, roleName);
    }

    public async Task<List<string>> GetUserEffectivePermissionsAsync(string userId)
    {
        var userRoles = await GetUserRolesAsync(userId);
        var permissions = new HashSet<string>();

        foreach (var roleName in userRoles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var permisos = await GetPermissionsForRoleAsync(role.Id);
                foreach (var permiso in permisos)
                {
                    permissions.Add(permiso.ClaimValue);
                }
            }
        }

        return permissions.ToList();
    }

    #endregion

    #region Módulos y Acciones

    public async Task<List<ModuloSistema>> GetAllModulosAsync()
    {
        return await _context.ModulosSistema
            .Include(m => m.Acciones)
            .Where(m => !m.IsDeleted && m.Activo)
            .OrderBy(m => m.Orden)
            .ToListAsync();
    }

    public async Task<ModuloSistema?> GetModuloByIdAsync(int id)
    {
        return await _context.ModulosSistema
            .Include(m => m.Acciones)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
    }

    public async Task<ModuloSistema?> GetModuloByClaveAsync(string clave)
    {
        return await _context.ModulosSistema
            .Include(m => m.Acciones)
            .FirstOrDefaultAsync(m => m.Clave == clave && !m.IsDeleted);
    }

    public async Task<List<AccionModulo>> GetAccionesForModuloAsync(int moduloId)
    {
        return await _context.AccionesModulo
            .Where(a => a.ModuloId == moduloId && !a.IsDeleted && a.Activa)
            .OrderBy(a => a.Orden)
            .ToListAsync();
    }

    public async Task<AccionModulo?> GetAccionByIdAsync(int id)
    {
        return await _context.AccionesModulo
            .Include(a => a.Modulo)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
    }

    public async Task<ModuloSistema> CreateModuloAsync(ModuloSistema modulo)
    {
        _context.ModulosSistema.Add(modulo);
        await _context.SaveChangesAsync();
        return modulo;
    }

    public async Task<bool> UpdateModuloAsync(ModuloSistema modulo, string? updatedBy = null)
    {
        var existing = await _context.ModulosSistema
            .FirstOrDefaultAsync(m => m.Id == modulo.Id && !m.IsDeleted);

        if (existing == null)
        {
            return false;
        }

        existing.Nombre = modulo.Nombre;
        existing.Clave = modulo.Clave;
        existing.Descripcion = modulo.Descripcion;
        existing.Categoria = modulo.Categoria;
        existing.Icono = modulo.Icono;
        existing.Orden = modulo.Orden;
        existing.Activo = modulo.Activo;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AccionModulo> CreateAccionAsync(AccionModulo accion)
    {
        _context.AccionesModulo.Add(accion);
        await _context.SaveChangesAsync();
        return accion;
    }

    public async Task<bool> UpdateAccionAsync(AccionModulo accion, string? updatedBy = null)
    {
        var existing = await _context.AccionesModulo
            .FirstOrDefaultAsync(a => a.Id == accion.Id && !a.IsDeleted);

        if (existing == null)
        {
            return false;
        }

        existing.Nombre = accion.Nombre;
        existing.Clave = accion.Clave;
        existing.Descripcion = accion.Descripcion;
        existing.ModuloId = accion.ModuloId;
        existing.Activa = accion.Activa;
        existing.Orden = accion.Orden;
        existing.Icono = accion.Icono;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAccionAsync(int id, string? deletedBy = null)
    {
        var accion = await _context.AccionesModulo
            .Include(a => a.Permisos)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (accion == null)
        {
            return false;
        }

        accion.IsDeleted = true;
        accion.UpdatedAt = DateTime.UtcNow;
        accion.UpdatedBy = deletedBy;

        foreach (var permiso in accion.Permisos)
        {
            permiso.IsDeleted = true;
            permiso.UpdatedAt = DateTime.UtcNow;
            permiso.UpdatedBy = deletedBy;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteModuloAsync(int id, string? deletedBy = null)
    {
        var modulo = await _context.ModulosSistema
            .Include(m => m.Acciones)
            .ThenInclude(a => a.Permisos)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (modulo == null)
        {
            return false;
        }

        modulo.IsDeleted = true;
        modulo.UpdatedAt = DateTime.UtcNow;
        modulo.UpdatedBy = deletedBy;

        foreach (var accion in modulo.Acciones)
        {
            accion.IsDeleted = true;
            accion.UpdatedAt = DateTime.UtcNow;
            accion.UpdatedBy = deletedBy;

            foreach (var permiso in accion.Permisos)
            {
                permiso.IsDeleted = true;
                permiso.UpdatedAt = DateTime.UtcNow;
                permiso.UpdatedBy = deletedBy;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Reportes y Estadísticas

    public async Task<Dictionary<string, Dictionary<string, List<string>>>> GetPermissionsMatrixAsync()
    {
        var roles = await GetAllRolesAsync();
        var modulos = await GetAllModulosAsync();
        var matrix = new Dictionary<string, Dictionary<string, List<string>>>();

        foreach (var role in roles)
        {
            var permisos = await GetPermissionsForRoleAsync(role.Id);
            var rolePermisos = new Dictionary<string, List<string>>();

            foreach (var modulo in modulos)
            {
                var acciones = permisos
                    .Where(p => p.ModuloId == modulo.Id)
                    .Select(p => p.Accion.Clave)
                    .ToList();

                rolePermisos[modulo.Clave] = acciones;
            }

            matrix[role.Name!] = rolePermisos;
        }

        return matrix;
    }

    public async Task<Dictionary<string, int>> GetRoleUsageStatsAsync()
    {
        var roles = await GetAllRolesAsync();
        var stats = new Dictionary<string, int>();

        foreach (var role in roles)
        {
            var users = await GetUsersInRoleAsync(role.Name!);
            stats[role.Name!] = users.Count;
        }

        return stats;
    }

    #endregion
}