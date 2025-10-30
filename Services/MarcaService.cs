using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Implementación del servicio de Marcas.
    /// Contiene toda la lógica de negocio relacionada con marcas.
    /// </summary>
    public class MarcaService : IMarcaService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MarcaService> _logger;

        public MarcaService(AppDbContext context, ILogger<MarcaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Marca>> GetAllAsync()
        {
            try
            {
                return await _context.Marcas
                    .Include(m => m.Parent)
                    .OrderBy(m => m.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las marcas");
                throw;
            }
        }

        public async Task<Marca?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Marcas
                    .Include(m => m.Parent)
                    .Include(m => m.Children)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener marca con Id {Id}", id);
                throw;
            }
        }

        public async Task<Marca?> GetByCodigoAsync(string codigo)
        {
            try
            {
                return await _context.Marcas
                    .FirstOrDefaultAsync(m => m.Codigo == codigo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener marca con código {Codigo}", codigo);
                throw;
            }
        }

        public async Task<Marca> CreateAsync(Marca marca)
        {
            try
            {
                // Validaciones de negocio
                if (await ExistsCodigoAsync(marca.Codigo))
                {
                    throw new InvalidOperationException($"Ya existe una marca con el código {marca.Codigo}");
                }

                _context.Marcas.Add(marca);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marca creada: {Codigo} - {Nombre}", marca.Codigo, marca.Nombre);

                return marca;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear marca {Codigo}", marca.Codigo);
                throw;
            }
        }

        public async Task<Marca> UpdateAsync(Marca marca)
        {
            try
            {
                // Verificar que existe
                var existing = await _context.Marcas.FindAsync(marca.Id);
                if (existing == null)
                {
                    throw new InvalidOperationException($"No se encontró la marca con Id {marca.Id}");
                }

                // Validar código único (excluyendo el registro actual)
                if (await ExistsCodigoAsync(marca.Codigo, marca.Id))
                {
                    throw new InvalidOperationException($"Ya existe otra marca con el código {marca.Codigo}");
                }

                // Actualizar propiedades
                existing.Codigo = marca.Codigo;
                existing.Nombre = marca.Nombre;
                existing.Descripcion = marca.Descripcion;
                existing.ParentId = marca.ParentId;
                existing.PaisOrigen = marca.PaisOrigen;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marca actualizada: {Codigo} - {Nombre}", marca.Codigo, marca.Nombre);

                return existing;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Conflicto de concurrencia al actualizar marca {Id}", marca.Id);
                throw new InvalidOperationException("La marca fue modificada por otro usuario. Por favor, recargue los datos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar marca {Id}", marca.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var marca = await _context.Marcas.FindAsync(id);
                if (marca == null)
                {
                    return false;
                }

                // Verificar si tiene submarcas
                var hasChildren = await _context.Marcas.AnyAsync(m => m.ParentId == id);
                if (hasChildren)
                {
                    throw new InvalidOperationException("No se puede eliminar una marca que tiene submarcas");
                }

                // Soft delete
                marca.IsDeleted = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marca eliminada (soft delete): {Codigo} - {Nombre}", marca.Codigo, marca.Nombre);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar marca {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null)
        {
            try
            {
                var query = _context.Marcas.Where(m => m.Codigo == codigo);

                if (excludeId.HasValue)
                {
                    query = query.Where(m => m.Id != excludeId.Value);
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