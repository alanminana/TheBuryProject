using TheBuryProject.Models.Entities;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Interfaz de servicio para operaciones de negocio de Categorías
    /// </summary>
    public interface HierarchicalService
    {
        /// <summary>
        /// Obtiene todas las categorías activas
        /// </summary>
        Task<IEnumerable<Categoria>> GetAllAsync();

        /// <summary>
        /// Obtiene una categoría por su Id
        /// </summary>
        Task<Categoria?> GetByIdAsync(int id);

        /// <summary>
        /// Obtiene una categoría por su código
        /// </summary>
        Task<Categoria?> GetByCodigoAsync(string codigo);

        /// <summary>
        /// Crea una nueva categoría
        /// </summary>
        Task<Categoria> CreateAsync(Categoria categoria);

        /// <summary>
        /// Actualiza una categoría existente
        /// </summary>
        Task<Categoria> UpdateAsync(Categoria categoria);

        /// <summary>
        /// Elimina una categoría (soft delete)
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Verifica si existe una categoría con el código especificado
        /// </summary>
        Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null);
    }
}