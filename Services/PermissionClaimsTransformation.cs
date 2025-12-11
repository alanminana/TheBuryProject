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

        // Evitar duplicados si los claims ya fueron agregados (por ejemplo, vía fábrica personalizada)
        var existingPermissions = identity
            .FindAll(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToHashSet();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return principal;
        }

        var effectivePermissions = await _rolService.GetUserEffectivePermissionsAsync(user.Id);
        foreach (var permiso in effectivePermissions)
        {
            if (!existingPermissions.Contains(permiso))
            {
                identity.AddClaim(new Claim("Permission", permiso));
            }
        }

        return principal;
    }
}
