using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProveedorService> _logger;

        public ProveedorService(AppDbContext context, ILogger<ProveedorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Proveedor>> GetAllAsync()
        {
            try
            {
                return await _context.Proveedores
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.ProveedorProductos)
                        .ThenInclude(pp => pp.Producto)
                    .Include(p => p.ProveedorMarcas)
                        .ThenInclude(pm => pm.Marca)
                    .Include(p => p.ProveedorCategorias)
                        .ThenInclude(pc => pc.Categoria)
                    .OrderBy(p => p.RazonSocial)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los proveedores");
                throw;
            }
        }

        public async Task<Proveedor?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Proveedores
                    .Include(p => p.ProveedorProductos)
                        .ThenInclude(pp => pp.Producto)
                    .Include(p => p.ProveedorMarcas)
                        .ThenInclude(pm => pm.Marca)
                    .Include(p => p.ProveedorCategorias)
                        .ThenInclude(pc => pc.Categoria)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proveedor {Id}", id);
                throw;
            }
        }

        public async Task CreateAsync(Proveedor proveedor)
        {
            try
            {
                // Validar CUIT único
                if (await ExistsCuitAsync(proveedor.Cuit))
                {
                    throw new InvalidOperationException($"Ya existe un proveedor con el CUIT {proveedor.Cuit}");
                }

                // ⭐ DEBUG - Ver qué viene en las asociaciones
                _logger.LogInformation("=== CREAR PROVEEDOR DEBUG ===");
                _logger.LogInformation("Productos recibidos: {Count}", proveedor.ProveedorProductos.Count);
                _logger.LogInformation("Marcas recibidas: {Count}", proveedor.ProveedorMarcas.Count);
                _logger.LogInformation("Categorías recibidas: {Count}", proveedor.ProveedorCategorias.Count);

                // Asegurar que las referencias al proveedor estén correctas
                foreach (var pp in proveedor.ProveedorProductos)
                {
                    pp.Proveedor = proveedor;
                    _logger.LogInformation("Producto ID: {ProductoId}", pp.ProductoId);
                }

                foreach (var pm in proveedor.ProveedorMarcas)
                {
                    pm.Proveedor = proveedor;
                }

                foreach (var pc in proveedor.ProveedorCategorias)
                {
                    pc.Proveedor = proveedor;
                }

                _context.Proveedores.Add(proveedor);
                await _context.SaveChangesAsync();

                // ⭐ DEBUG - Verificar después de guardar
                var proveedorGuardado = await _context.Proveedores
                    .Include(p => p.ProveedorProductos)
                    .FirstOrDefaultAsync(p => p.Id == proveedor.Id);

                _logger.LogInformation("Productos guardados en DB: {Count}", proveedorGuardado?.ProveedorProductos.Count ?? 0);
                _logger.LogInformation("=== FIN DEBUG ===");

                _logger.LogInformation("Proveedor creado: {Id} - {RazonSocial} con {ProductosCount} productos, {MarcasCount} marcas, {CategoriasCount} categorías",
                    proveedor.Id, proveedor.RazonSocial, proveedor.ProveedorProductos.Count, proveedor.ProveedorMarcas.Count, proveedor.ProveedorCategorias.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear proveedor");
                throw;
            }
        }

        public async Task UpdateAsync(Proveedor proveedor)
        {
            try
            {
                // Validar CUIT único (excluyendo el registro actual)
                if (await ExistsCuitAsync(proveedor.Cuit, proveedor.Id))
                {
                    throw new InvalidOperationException($"Ya existe otro proveedor con el CUIT {proveedor.Cuit}");
                }

                var existingProveedor = await _context.Proveedores
                    .Include(p => p.ProveedorProductos)
                    .Include(p => p.ProveedorMarcas)
                    .Include(p => p.ProveedorCategorias)
                    .FirstOrDefaultAsync(p => p.Id == proveedor.Id);

                if (existingProveedor == null)
                {
                    throw new InvalidOperationException("Proveedor no encontrado");
                }

                // Actualizar propiedades básicas
                _context.Entry(existingProveedor).CurrentValues.SetValues(proveedor);

                // Actualizar asociaciones de productos
                existingProveedor.ProveedorProductos.Clear();
                foreach (var pp in proveedor.ProveedorProductos)
                {
                    pp.ProveedorId = proveedor.Id;
                    existingProveedor.ProveedorProductos.Add(pp);
                }

                // Actualizar asociaciones de marcas
                existingProveedor.ProveedorMarcas.Clear();
                foreach (var pm in proveedor.ProveedorMarcas)
                {
                    pm.ProveedorId = proveedor.Id;
                    existingProveedor.ProveedorMarcas.Add(pm);
                }

                // Actualizar asociaciones de categorías
                existingProveedor.ProveedorCategorias.Clear();
                foreach (var pc in proveedor.ProveedorCategorias)
                {
                    pc.ProveedorId = proveedor.Id;
                    existingProveedor.ProveedorCategorias.Add(pc);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Proveedor actualizado: {Id} - {RazonSocial}", proveedor.Id, proveedor.RazonSocial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar proveedor {Id}", proveedor.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var proveedor = await _context.Proveedores.FindAsync(id);
                if (proveedor == null)
                {
                    return false;
                }

                // Verificar si tiene órdenes de compra asociadas
                var tieneOrdenes = await _context.OrdenesCompra.AnyAsync(o => o.ProveedorId == id);
                if (tieneOrdenes)
                {
                    throw new InvalidOperationException("No se puede eliminar el proveedor porque tiene órdenes de compra asociadas");
                }

                // Soft delete
                proveedor.IsDeleted = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Proveedor eliminado: {Id} - {RazonSocial}", id, proveedor.RazonSocial);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar proveedor {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsCuitAsync(string cuit, int? excludeId = null)
        {
            try
            {
                var query = _context.Proveedores.Where(p => p.Cuit == cuit);

                if (excludeId.HasValue)
                {
                    query = query.Where(p => p.Id != excludeId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de CUIT {Cuit}", cuit);
                throw;
            }
        }

        public async Task<IEnumerable<Proveedor>> SearchAsync(
            string? searchTerm = null,
            bool soloActivos = false,
            string? orderBy = null,
            string? orderDirection = "asc")
        {
            try
            {
                var query = _context.Proveedores
                    .Include(p => p.ProveedorProductos)
                        .ThenInclude(pp => pp.Producto)
                    .Include(p => p.ProveedorMarcas)
                        .ThenInclude(pm => pm.Marca)
                    .Include(p => p.ProveedorCategorias)
                        .ThenInclude(pc => pc.Categoria)
                    .AsQueryable();

                // Búsqueda por texto
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(p =>
                        p.Cuit.Contains(searchTerm) ||
                        p.RazonSocial.ToLower().Contains(searchTerm) ||
                        (p.NombreFantasia != null && p.NombreFantasia.ToLower().Contains(searchTerm)) ||
                        (p.Email != null && p.Email.ToLower().Contains(searchTerm))
                    );
                }

                // Filtro solo activos
                if (soloActivos)
                {
                    query = query.Where(p => p.Activo);
                }

                // Ordenamiento dinámico
                if (!string.IsNullOrWhiteSpace(orderBy))
                {
                    var ascending = orderDirection?.ToLower() != "desc";
                    query = orderBy.ToLower() switch
                    {
                        "cuit" => ascending ? query.OrderBy(p => p.Cuit) : query.OrderByDescending(p => p.Cuit),
                        "razonsocial" => ascending ? query.OrderBy(p => p.RazonSocial) : query.OrderByDescending(p => p.RazonSocial),
                        "email" => ascending ? query.OrderBy(p => p.Email) : query.OrderByDescending(p => p.Email),
                        "telefono" => ascending ? query.OrderBy(p => p.Telefono) : query.OrderByDescending(p => p.Telefono),
                        _ => query.OrderBy(p => p.RazonSocial)
                    };
                }
                else
                {
                    query = query.OrderBy(p => p.RazonSocial);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar proveedores con filtros");
                throw;
            }
        }
    }
}