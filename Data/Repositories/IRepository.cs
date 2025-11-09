using System.Linq.Expressions;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Data.Repositories
{
    /// <summary>
    /// Interfaz genérica para repositorio de entidades.
    /// Proporciona operaciones CRUD básicas y consultas.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad que hereda de BaseEntity</typeparam>
    public interface AutoMapperProfile<T> where T : DashboardDtos
    {
        /// <summary>
        /// Obtiene una entidad por su ID
        /// </summary>
        Task<T?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Obtiene todas las entidades (sin filtro de soft delete, ya aplicado por EF)
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// Agrega una nueva entidad
        /// </summary>
        Task AddAsync(T entity, CancellationToken ct = default);

        /// <summary>
        /// Marca una entidad como modificada
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// Elimina una entidad (soft delete)
        /// </summary>
        void Remove(T entity);

        /// <summary>
        /// Verifica si existe al menos una entidad que cumple el predicado
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

        /// <summary>
        /// Retorna un IQueryable para componer consultas personalizadas
        /// </summary>
        IQueryable<T> Query();
    }
}
