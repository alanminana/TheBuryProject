using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
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
                // Validación de string vacío
                if (string.IsNullOrWhiteSpace(categoria.Codigo))
                {
                    throw new InvalidOperationException("El código no puede estar vacío");
                }

                if (string.IsNullOrWhiteSpace(categoria.Nombre))
                {
                    throw new InvalidOperationException("El nombre no puede estar vacío");
                }

                // Validaciones de negocio
                if (await ExistsCodigoAsync(categoria.Codigo))
                {
                    throw new InvalidOperationException($"Ya existe una categoría con el código {categoria.Codigo}");
                }

                // Validar que el ParentId exista si se especifica
                if (categoria.ParentId.HasValue)
                {
                    var parentExists = await _context.Categorias.AnyAsync(c => c.Id == categoria.ParentId.Value);
                    if (!parentExists)
                    {
                        throw new InvalidOperationException($"La categoría padre con Id {categoria.ParentId.Value} no existe");
                    }

                    // Validar que no se está creando un ciclo
                    if (await WouldCreateCycleAsync(null, categoria.ParentId.Value))
                    {
                        throw new InvalidOperationException("No se puede establecer esta relación porque crearía un ciclo");
                    }
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
                // Validación de string vacío
                if (string.IsNullOrWhiteSpace(categoria.Codigo))
                {
                    throw new InvalidOperationException("El código no puede estar vacío");
                }

                if (string.IsNullOrWhiteSpace(categoria.Nombre))
                {
                    throw new InvalidOperationException("El nombre no puede estar vacío");
                }

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

                // Validar que el ParentId exista si se especifica
                if (categoria.ParentId.HasValue)
                {
                    var parentExists = await _context.Categorias.AnyAsync(c => c.Id == categoria.ParentId.Value);
                    if (!parentExists)
                    {
                        throw new InvalidOperationException($"La categoría padre con Id {categoria.ParentId.Value} no existe");
                    }

                    // Validar que no se crea un ciclo
                    if (await WouldCreateCycleAsync(categoria.Id, categoria.ParentId.Value))
                    {
                        throw new InvalidOperationException("No se puede establecer esta relación porque crearía un ciclo jerárquico");
                    }
                }

                // Actualizar propiedades
                existing.Codigo = categoria.Codigo;
                existing.Nombre = categoria.Nombre;
                existing.Descripcion = categoria.Descripcion;
                existing.ParentId = categoria.ParentId;
                existing.ControlSerieDefault = categoria.ControlSerieDefault;

                // IMPORTANTE: Copiar el RowVersion para que funcione el control de concurrencia
                if (categoria.RowVersion != null)
                {
                    _context.Entry(existing).OriginalValues["RowVersion"] = categoria.RowVersion;
                }

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
        public async Task<IEnumerable<Categoria>> SearchAsync(
    string? searchTerm = null,
    bool soloActivos = false,
    string? orderBy = null,
    string? orderDirection = "asc")
        {
            try
            {
                var query = _context.Categorias
                    .Include(c => c.Parent)
                    .AsQueryable();

                // Búsqueda por texto
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(c =>
                        c.Nombre.ToLower().Contains(searchTerm) ||
                        (c.Descripcion != null && c.Descripcion.ToLower().Contains(searchTerm))
                    );
                }

                // Filtro solo activos
                if (soloActivos)
                {
                    query = query.Where(c => c.Activo);
                }

                // Ordenamiento dinámico
                if (!string.IsNullOrWhiteSpace(orderBy))
                {
                    var ascending = orderDirection?.ToLower() != "desc";
                    query = orderBy.ToLower() switch
                    {
                        "nombre" => ascending ? query.OrderBy(c => c.Nombre) : query.OrderByDescending(c => c.Nombre),
                        "descripcion" => ascending ? query.OrderBy(c => c.Descripcion) : query.OrderByDescending(c => c.Descripcion),
                        "parent" => ascending ? query.OrderBy(c => c.Parent.Nombre) : query.OrderByDescending(c => c.Parent.Nombre),
                        _ => query.OrderBy(c => c.Nombre)
                    };
                }
                else
                {
                    query = query.OrderBy(c => c.Nombre);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar categorías con filtros");
                throw;
            }
        }
        /// <summary>
        /// Verifica si establecer una relación padre-hijo crearía un ciclo en la jerarquía.
        /// </summary>
        /// <param name="categoryId">ID de la categoría (null para nuevas categorías)</param>
        /// <param name="parentId">ID del padre propuesto</param>
        /// <returns>True si se crearía un ciclo, False en caso contrario</returns>
        private async Task<bool> WouldCreateCycleAsync(int? categoryId, int parentId)
        {
            // Si no hay categoryId, es una creación nueva, no puede haber ciclo
            if (!categoryId.HasValue)
            {
                return false;
            }

            // Si intenta ser su propio padre
            if (categoryId.Value == parentId)
            {
                return true;
            }

            // Recorrer la jerarquía hacia arriba desde el parent propuesto
            var currentParentId = (int?)parentId;
            var visitedIds = new HashSet<int> { categoryId.Value };

            while (currentParentId.HasValue)
            {
                // Si encontramos la categoría original, hay un ciclo
                if (visitedIds.Contains(currentParentId.Value))
                {
                    return true;
                }

                visitedIds.Add(currentParentId.Value);

                // Obtener el padre del padre
                var parent = await _context.Categorias
                    .Where(c => c.Id == currentParentId.Value)
                    .Select(c => new { c.ParentId })
                    .FirstOrDefaultAsync();

                if (parent == null)
                {
                    break;
                }

                currentParentId = parent.ParentId;
            }

            return false;
        }
    }
}