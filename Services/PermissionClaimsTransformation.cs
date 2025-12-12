using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services;

/// <summary>
/// Agrega dinámicamente los claims de permisos efectivos del usuario después de la autenticación.
/// Esto asegura que los permisos asignados vía roles estén presentes en el principal incluso cuando
/// los claims del rol no se copien automáticamente al cookie de autenticación.
/// </summary>
public class PermissionClaimsTransformation : IClaimsTransformation
{
    private readonly IRolService _rolService;
    private readonly UserManager<IdentityUser> _userManager;

    public PermissionClaimsTransformation(IRolService rolService, UserManager<IdentityUser> userManager)
    {
        _rolService = rolService;
        _userManager = userManager;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identity == null || string.IsNullOrEmpty(userId))
        {
            return principal;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return principal;
        }

        var effectivePermissions = await _rolService.GetUserEffectivePermissionsAsync(user.Id);
        var normalizedEffectivePermissions = effectivePermissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Quitar permisos que ya no correspondan según la evaluación actual
        var existingPermissionClaims = identity
            .FindAll(c => c.Type == "Permission")
            .ToList();

        foreach (var claim in existingPermissionClaims)
        {
            if (!normalizedEffectivePermissions.Contains(claim.Value))
            {
                identity.RemoveClaim(claim);
            }
        }

        // Agregar los permisos faltantes que sí correspondan
        var currentPermissions = identity
            .FindAll(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permiso in normalizedEffectivePermissions)
        {
            if (currentPermissions.Add(permiso))
            {
                identity.AddClaim(new Claim("Permission", permiso));
            }
        }

        return principal;
    }
}
