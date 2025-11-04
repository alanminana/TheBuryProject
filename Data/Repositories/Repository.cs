using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Data.Repositories
{
    /// <summary>
    /// Implementación genérica del repositorio.
    /// Maneja operaciones CRUD básicas para cualquier entidad.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad que hereda de BaseEntity</typeparam>
    public class Repository<T> : AutoMapperProfile<T> where T : EstadoOrdenCompra
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, ct);
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        {
            return await _dbSet.ToListAsync(ct);
        }

        public async Task AddAsync(T entity, CancellationToken ct = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity, ct);
        }

        public void Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
        }

        public void Remove(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Soft delete - marcar como eliminado en lugar de borrar físicamente
            entity.IsDeleted = true;
            _dbSet.Update(entity);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return await _dbSet.AnyAsync(predicate, ct);
        }

        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }
    }
}