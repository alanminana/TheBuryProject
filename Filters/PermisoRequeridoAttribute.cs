using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TheBuryProject.Models.Constants;

namespace TheBuryProject.Filters;

/// <summary>
/// Attribute para requerir un permiso específico (claims-based)
/// Uso: [PermisoRequerido(Modulo = "Ventas", Accion = "create")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermisoRequeridoAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    /// <summary>
    /// Clave del módulo (ventas, productos, clientes, etc.)
    /// </summary>
    public string Modulo { get; set; } = string.Empty;

    /// <summary>
    /// Clave de la acción (view, create, update, delete, authorize, etc.)
    /// </summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>
    /// Si es true, permite acceso a SuperAdmin sin verificar el permiso específico
    /// </summary>
    public bool AllowSuperAdmin { get; set; } = true;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Verificar si el usuario está autenticado
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        // TEMPORAL: En desarrollo, permitir acceso si está autenticado
        var env = context.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
        if (env?.IsDevelopment() == true)
        {
            return; // Permitir acceso en desarrollo
        }

        // Si AllowSuperAdmin está habilitado y el usuario es SuperAdmin, permitir
        if (AllowSuperAdmin && user.IsInRole(TheBuryProject.Models.Constants.Roles.SuperAdmin))
        {
            return;
        }

        // Construir el claim value requerido
        var claimValue = $"{Modulo}.{Accion}";

        // Verificar si el usuario tiene el claim de permiso
        var hasPermission = user.HasClaim(c => c.Type == "Permission" && c.Value == claimValue);

        if (!hasPermission)
        {
            // Registrar intento de acceso no autorizado
            var logger = context.HttpContext.RequestServices
                .GetService<ILogger<PermisoRequeridoAttribute>>();

            logger?.LogWarning(
                "Acceso denegado: Usuario {Username} intentó acceder a {Path} sin permiso {Permission}",
                user.Identity?.Name ?? "Desconocido",
                context.HttpContext.Request.Path,
                claimValue
            );

            // Retornar 403 Forbidden
            context.Result = new ForbidResult();
        }
    }
}