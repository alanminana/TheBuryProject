using Microsoft.AspNetCore.Identity;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Usuario de la aplicación extendiendo IdentityUser para agregar campos personalizados.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Indica si el usuario está activo en el sistema.
        /// Los usuarios inactivos no pueden iniciar sesión y no se muestran en listas.
        /// Se usa soft delete para mantener integridad referencial con Ventas, Auditorías, etc.
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Fecha de creación del usuario
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha de desactivación del usuario (si está inactivo)
        /// </summary>
        public DateTime? FechaDesactivacion { get; set; }

        /// <summary>
        /// Usuario que desactivó este usuario
        /// </summary>
        public string? DesactivadoPor { get; set; }

        /// <summary>
        /// Motivo de la desactivación
        /// </summary>
        public string? MotivoDesactivacion { get; set; }
    }
}
