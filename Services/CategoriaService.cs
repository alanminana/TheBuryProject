using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Implementaci�n del servicio de Categor�as.
    /// Contiene toda la l�gica de negocio relacionada con categor�as.
    /// </summary>
    public class CategoriaService : ICategoriaService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoriaService> _logger;

        public CategoriaService(AppDbContext context, ILogger<CategoriaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Categoria>> GetAllAsync()
        {
            try
            {
                return await _context.Categorias
                    .Include(c => c.Parent)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las categor�as");
                throw;
            }
        }

        public async Task<Categoria?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Categorias
                    .Include(c => c.Parent)
                    .Include(c => c.Children)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categor�a con Id {Id}", id);
                throw;
            }
        }

        public async Task<Categoria?> GetByCodigoAsync(string codigo)
        {
            try
            {
                return await _context.Categorias
                    .FirstOrDefaultAsync(c => c.Codigo == codigo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categor�a con c�digo {Codigo}", codigo);
                throw;
            }
        }

        public async Task<Categoria> CreateAsync(Categoria categoria)
        {
            try
            {
                // Validaciones de negocio
                if (await ExistsCodigoAsync(categoria.Codigo))
                {
                    throw new InvalidOperationException($"Ya existe una categor�a con el c�digo {categoria.Codigo}");
                }

                _context.Categorias.Add(categoria);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Categor�a creada: {Codigo} - {Nombre}", categoria.Codigo, categoria.Nombre);

                return categoria;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categor�a {Codigo}", categoria.Codigo);
                throw;
            }
        }

        public async Task<Categoria> UpdateAsync(Categoria categoria)
        {
            try
            {
                // Verificar que existe
                var existing = await _context.Categorias.FindAsync(categoria.Id);
                if (existing == null)
                {
                    throw new InvalidOperationException($"No se encontr� la categor�a con Id {categoria.Id}");
                }

                // Validar c�digo �nico (excluyendo el registro actual)
                if (await ExistsCodigoAsync(categoria.Codigo, categoria.Id))
                {
                    throw new InvalidOperationException($"Ya existe otra categor�a con el c�digo {categoria.Codigo}");
                }

                // Actualizar propiedades
                existing.Codigo = categoria.Codigo;
                existing.Nombre = categoria.Nombre;
                existing.Descripcion = categoria.Descripcion;
                existing.ParentId = categoria.ParentId;
                existing.ControlSerieDefault = categoria.ControlSerieDefault;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Categor�a actualizada: {Codigo} - {Nombre}", categoria.Codigo, categoria.Nombre);

                return existing;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Conflicto de concurrencia al actualizar categor�a {Id}", categoria.Id);
                throw new InvalidOperationException("La categor�a fue modificada por otro usuario. Por favor, recargue los datos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categor�a {Id}", categoria.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var categoria = await _context.Categorias.FindAsync(id);
                if (categoria == null)
                {
                    return false;
                }

                // Verificar si tiene categor�as hijas
                var hasChildren = await _context.Categorias.AnyAsync(c => c.ParentId == id);
                if (hasChildren)
                {
                    throw new InvalidOperationException("No se puede eliminar una categor�a que tiene subcategor�as");
                }

                // Soft delete
                categoria.IsDeleted = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Categor�a eliminada (soft delete): {Codigo} - {Nombre}", categoria.Codigo, categoria.Nombre);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categor�a {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null)
        {
            try
            {
                var query = _context.Categorias.Where(c => c.Codigo == codigo);

                if (excludeId.HasValue)
                {
                    query = query.Where(c => c.Id != excludeId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de c�digo {Codigo}", codigo);
                throw;
            }
        }
    }
}