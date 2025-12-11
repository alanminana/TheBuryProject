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
        var identity = (ClaimsIdentity)principal.Identity!;

        var permisos = await _rolService.GetUserEffectivePermissionsAsync(user.Id);

        // Opcional: deduplicar Permission claims que ya vengan de los roles
        var existentes = identity.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var permiso in permisos)
        {
            if (!existentes.Contains(permiso))
            {
                identity.AddClaim(new Claim("Permission", permiso));
            }
        }

        return principal;
    }
}
