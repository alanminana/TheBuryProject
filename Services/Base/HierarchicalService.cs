using TheBuryProject.Models.Entities;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Interfaz de servicio para operaciones de negocio de Categor�as
    /// </summary>
    public interface HierarchicalService
    {
        /// <summary>
        /// Obtiene todas las categor�as activas
        /// </summary>
        Task<IEnumerable<Categoria>> GetAllAsync();

        /// <summary>
        /// Obtiene una categor�a por su Id
        /// </summary>
        Task<Categoria?> GetByIdAsync(int id);

        /// <summary>
        /// Obtiene una categor�a por su c�digo
        /// </summary>
        Task<Categoria?> GetByCodigoAsync(string codigo);

        /// <summary>
        /// Crea una nueva categor�a
        /// </summary>
        Task<Categoria> CreateAsync(Categoria categoria);

        /// <summary>
        /// Actualiza una categor�a existente
        /// </summary>
        Task<Categoria> UpdateAsync(Categoria categoria);

        /// <summary>
        /// Elimina una categor�a (soft delete)
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Verifica si existe una categor�a con el c�digo especificado
        /// </summary>
        Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null);
    }
}