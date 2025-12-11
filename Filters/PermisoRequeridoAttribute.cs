using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    /// Si es true, permite acceso a <see cref="Roles.SuperAdmin"/> sin verificar el permiso específico
    /// </summary>
    public bool AllowSuperAdmin { get; set; } = true;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        // Primera validación: el usuario debe estar autenticado antes de cualquier lógica de permisos
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var serviceProvider = context.HttpContext.RequestServices;
        var env = serviceProvider.GetService<IWebHostEnvironment>();
        var logger = serviceProvider.GetService<ILogger<PermisoRequeridoAttribute>>();
        var configuration = serviceProvider.GetService<IConfiguration>();
        var requestPath = httpContext.Request.Path;

        // Permitir omitir permisos solo cuando la configuración lo habilite explícitamente en desarrollo
        var skipPermissionsInDevelopment = env?.IsDevelopment() is true
            && configuration?.GetValue<bool>("Seguridad:OmitirPermisosEnDev") is true;

        if (skipPermissionsInDevelopment)
        {
            logger?.LogWarning(
                "Permisos omitidos en Development porque Seguridad:OmitirPermisosEnDev=true para {Username} al acceder a {Path}",
                user.Identity?.Name ?? "Desconocido",
                requestPath);

            return; // Permitir acceso en desarrollo
        }

        // Bypass de Roles.SuperAdmin (después del bypass en Development, antes de validar claims)
        if (AllowSuperAdmin && user.IsInRole(Roles.SuperAdmin))
        {
            return;
        }

        // Construir el claim value requerido
        var claimValue = $"{Modulo}.{Accion}";

        // Verificar si el usuario tiene el claim de permiso requerido
        var hasPermission = user.HasClaim(c => c.Type == "Permission" && c.Value == claimValue);

        if (!hasPermission)
        {
            // Registrar intento de acceso no autorizado
            logger?.LogWarning(
                "Acceso denegado: Usuario {Username} intentó acceder a {Path} sin claim Permission requerido {Permission}",
                user.Identity?.Name ?? "Desconocido",
                requestPath,
                claimValue
            );

            // Retornar 403 Forbidden
            context.Result = new ForbidResult();
            return;
        }
    }
}
