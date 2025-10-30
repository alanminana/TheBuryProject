namespace TheBuryProject.Models.Base
{
    /// <summary>
    /// Clase base para todas las entidades del sistema.
    /// Proporciona propiedades comunes como Id, auditor�a y soft delete.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Identificador �nico de la entidad
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Fecha y hora de creaci�n del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha y hora de �ltima modificaci�n
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Usuario que cre� el registro
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Usuario que modific� el registro por �ltima vez
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Indica si el registro est� eliminado (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Token de concurrencia para control optimista
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}