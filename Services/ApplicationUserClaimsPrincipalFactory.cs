using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services;

/// <summary>
/// Fábrica personalizada de ClaimsPrincipal que agrega los permisos efectivos del usuario
/// como claims de tipo "Permission".
/// </summary>
public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<IdentityUser, IdentityRole>
{
    private readonly IRolService _rolService;

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IRolService rolService)
        : base(userManager, roleManager, optionsAccessor)
    {
        _rolService = rolService;
    }

    /// <summary>
    /// Agrega los permisos efectivos del usuario como claims.
    /// </summary>
    public override async Task<ClaimsPrincipal> CreateAsync(IdentityUser user)
    {
        var principal = await base.CreateAsync(user);
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return principal;
        }

        var existingPermissions = identity
            .FindAll("Permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var permisos = await _rolService.GetUserEffectivePermissionsAsync(user.Id);
        foreach (var permiso in permisos.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (existingPermissions.Add(permiso))
            {
                identity.AddClaim(new Claim("Permission", permiso));
            }
        }

        return principal;
    }
}
