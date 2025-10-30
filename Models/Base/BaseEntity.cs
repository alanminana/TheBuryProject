namespace TheBuryProject.Models.Base
{
    /// <summary>
    /// Clase base para todas las entidades del sistema.
    /// Proporciona propiedades comunes como Id, auditoría y soft delete.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Identificador único de la entidad
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Fecha y hora de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha y hora de última modificación
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Usuario que creó el registro
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Usuario que modificó el registro por última vez
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Indica si el registro está eliminado (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Token de concurrencia para control optimista
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}