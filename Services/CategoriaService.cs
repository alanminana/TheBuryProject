using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Implementación del servicio de Categorías.
    /// Contiene toda la lógica de negocio relacionada con categorías.
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
                _logger.LogError(ex, "Error al obtener todas las categorías");
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
                _logger.LogError(ex, "Error al obtener categoría con Id {Id}", id);
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
                _logger.LogError(ex, "Error al obtener categoría con código {Codigo}", codigo);
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
                    throw new InvalidOperationException($"Ya existe una categoría con el código {categoria.Codigo}");
                }

                _context.Categorias.Add(categoria);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Categoría creada: {Codigo} - {Nombre}", categoria.Codigo, categoria.Nombre);

                return categoria;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría {Codigo}", categoria.Codigo);
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
                    throw new InvalidOperationException($"No se encontró la categoría con Id {categoria.Id}");
                }

                // Validar código único (excluyendo el registro actual)
                if (await ExistsCodigoAsync(categoria.Codigo, categoria.Id))
                {
                    throw new InvalidOperationException($"Ya existe otra categoría con el código {categoria.Codigo}");
                }

                // Actualizar propiedades
                existing.Codigo = categoria.Codigo;
                existing.Nombre = categoria.Nombre;
                existing.Descripcion = categoria.Descripcion;
                existing.ParentId = categoria.ParentId;
                existing.ControlSerieDefault = categoria.ControlSerieDefault;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Categoría actualizada: {Codigo} - {Nombre}", categoria.Codigo, categoria.Nombre);

                return existing;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Conflicto de concurrencia al actualizar categoría {Id}", categoria.Id);
                throw new InvalidOperationException("La categoría fue modificada por otro usuario. Por favor, recargue los datos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categoría {Id}", categoria.Id);
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

                // Verificar si tiene categorías hijas
                var hasChildren = await _context.Categorias.AnyAsync(c => c.ParentId == id);
                if (hasChildren)
                {
                    throw new InvalidOperationException("No se puede eliminar una categoría que tiene subcategorías");
                }

                // Soft delete
                categoria.IsDeleted = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Categoría eliminada (soft delete): {Codigo} - {Nombre}", categoria.Codigo, categoria.Nombre);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categoría {Id}", id);
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
                _logger.LogError(ex, "Error al verificar existencia de código {Codigo}", codigo);
                throw;
            }
        }
    }
}