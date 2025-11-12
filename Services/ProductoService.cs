using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    public class ProductoService : IProductoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductoService> _logger;
        private readonly IPrecioHistoricoService _precioHistoricoService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductoService(
            AppDbContext context,
            ILogger<ProductoService> logger,
            IPrecioHistoricoService precioHistoricoService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _precioHistoricoService = precioHistoricoService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<Producto>> GetAllAsync()
        {
            try
            {
                return await _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Marca)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los productos");
                throw;
            }
        }
        public async Task<IEnumerable<Producto>> SearchAsync(
    string? searchTerm = null,
    int? categoriaId = null,
    int? marcaId = null,
    bool stockBajo = false,
    bool soloActivos = false,
    string? orderBy = null,
    string? orderDirection = "asc")
        {
            try
            {
                var query = _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Marca)
                    .AsQueryable();

                // Filtro por búsqueda de texto
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(p =>
                        p.Codigo.ToLower().Contains(searchTerm) ||
                        p.Nombre.ToLower().Contains(searchTerm) ||
                        (p.Descripcion != null && p.Descripcion.ToLower().Contains(searchTerm))
                    );
                }

                // Filtro por categoría
                if (categoriaId.HasValue)
                {
                    query = query.Where(p => p.CategoriaId == categoriaId.Value);
                }

                // Filtro por marca
                if (marcaId.HasValue)
                {
                    query = query.Where(p => p.MarcaId == marcaId.Value);
                }

                // Filtro por stock bajo
                if (stockBajo)
                {
                    query = query.Where(p => p.StockActual <= p.StockMinimo);
                }

                // Filtro solo activos
                if (soloActivos)
                {
                    query = query.Where(p => p.Activo);
                }

                // Ordenamiento
                if (!string.IsNullOrWhiteSpace(orderBy))
                {
                    var ascending = orderDirection?.ToLower() != "desc";

                    query = orderBy.ToLower() switch
                    {
                        "codigo" => ascending ? query.OrderBy(p => p.Codigo) : query.OrderByDescending(p => p.Codigo),
                        "nombre" => ascending ? query.OrderBy(p => p.Nombre) : query.OrderByDescending(p => p.Nombre),
                        "preciocompra" => ascending ? query.OrderBy(p => p.PrecioCompra) : query.OrderByDescending(p => p.PrecioCompra),
                        "precioventa" => ascending ? query.OrderBy(p => p.PrecioVenta) : query.OrderByDescending(p => p.PrecioVenta),
                        "stock" => ascending ? query.OrderBy(p => p.StockActual) : query.OrderByDescending(p => p.StockActual),
                        "categoria" => ascending ? query.OrderBy(p => p.Categoria.Nombre) : query.OrderByDescending(p => p.Categoria.Nombre),
                        "marca" => ascending ? query.OrderBy(p => p.Marca.Nombre) : query.OrderByDescending(p => p.Marca.Nombre),
                        _ => query.OrderBy(p => p.Nombre) // Default
                    };
                }
                else
                {
                    query = query.OrderBy(p => p.Nombre);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar productos con filtros");
                throw;
            }
        }
        public async Task<Producto?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Marca)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el producto {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Producto>> GetByCategoriaAsync(int categoriaId)
        {
            try
            {
                return await _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Marca)
                    .Where(p => p.CategoriaId == categoriaId)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos por categoría {CategoriaId}", categoriaId);
                throw;
            }
        }

        public async Task<IEnumerable<Producto>> GetByMarcaAsync(int marcaId)
        {
            try
            {
                return await _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Marca)
                    .Where(p => p.MarcaId == marcaId)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos por marca {MarcaId}", marcaId);
                throw;
            }
        }

        public async Task<IEnumerable<Producto>> GetProductosConStockBajoAsync()
        {
            try
            {
                return await _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Marca)
                    .Where(p => p.StockActual <= p.StockMinimo)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos con stock bajo");
                throw;
            }
        }

        public async Task<Producto> CreateAsync(Producto producto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(producto.Codigo))
                {
                    throw new InvalidOperationException("El código del producto no puede estar vacío");
                }

                if (string.IsNullOrWhiteSpace(producto.Nombre))
                {
                    throw new InvalidOperationException("El nombre del producto no puede estar vacío");
                }

                if (await ExistsCodigoAsync(producto.Codigo))
                {
                    throw new InvalidOperationException($"Ya existe un producto con el código '{producto.Codigo}'");
                }

                if (producto.PrecioVenta < producto.PrecioCompra)
                {
                    _logger.LogWarning(
                        "Creando producto {Codigo} con precio de venta ({PrecioVenta}) menor al precio de compra ({PrecioCompra})",
                        producto.Codigo,
                        producto.PrecioVenta,
                        producto.PrecioCompra
                    );
                }

                if (producto.PrecioCompra < 0)
                {
                    throw new InvalidOperationException("El precio de compra no puede ser negativo");
                }

                if (producto.PrecioVenta < 0)
                {
                    throw new InvalidOperationException("El precio de venta no puede ser negativo");
                }

                if (producto.StockActual < 0)
                {
                    throw new InvalidOperationException("El stock actual no puede ser negativo");
                }

                if (producto.StockMinimo < 0)
                {
                    throw new InvalidOperationException("El stock mínimo no puede ser negativo");
                }

                producto.CreatedAt = DateTime.UtcNow;
                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Producto creado exitosamente: {Codigo} - {Nombre}", producto.Codigo, producto.Nombre);
                return producto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el producto {Codigo}", producto.Codigo);
                throw;
            }
        }

        public async Task<Producto> UpdateAsync(Producto producto)
        {
            try
            {
                var productoExistente = await _context.Productos.FindAsync(producto.Id);
                if (productoExistente == null)
                {
                    throw new InvalidOperationException($"No se encontró el producto con ID {producto.Id}");
                }

                if (string.IsNullOrWhiteSpace(producto.Codigo))
                {
                    throw new InvalidOperationException("El código del producto no puede estar vacío");
                }

                if (string.IsNullOrWhiteSpace(producto.Nombre))
                {
                    throw new InvalidOperationException("El nombre del producto no puede estar vacío");
                }

                if (await ExistsCodigoAsync(producto.Codigo, producto.Id))
                {
                    throw new InvalidOperationException($"Ya existe otro producto con el código '{producto.Codigo}'");
                }

                if (producto.PrecioCompra < 0)
                {
                    throw new InvalidOperationException("El precio de compra no puede ser negativo");
                }

                if (producto.PrecioVenta < 0)
                {
                    throw new InvalidOperationException("El precio de venta no puede ser negativo");
                }

                if (producto.PrecioVenta < producto.PrecioCompra)
                {
                    _logger.LogWarning(
                        "Actualizando producto {Codigo} con precio de venta ({PrecioVenta}) menor al precio de compra ({PrecioCompra})",
                        producto.Codigo,
                        producto.PrecioVenta,
                        producto.PrecioCompra
                    );
                }

                if (producto.StockActual < 0)
                {
                    throw new InvalidOperationException("El stock actual no puede ser negativo");
                }

                if (producto.StockMinimo < 0)
                {
                    throw new InvalidOperationException("El stock mínimo no puede ser negativo");
                }

                // Registrar cambio de precio si corresponde
                bool preciosCambiaron =
                    productoExistente.PrecioCompra != producto.PrecioCompra ||
                    productoExistente.PrecioVenta != producto.PrecioVenta;

                if (preciosCambiaron)
                {
                    var usuario = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";

                    await _precioHistoricoService.RegistrarCambioAsync(
                        productoId: producto.Id,
                        precioCompraAnterior: productoExistente.PrecioCompra,
                        precioCompraNuevo: producto.PrecioCompra,
                        precioVentaAnterior: productoExistente.PrecioVenta,
                        precioVentaNuevo: producto.PrecioVenta,
                        motivoCambio: null, // Se puede agregar en el futuro
                        usuarioModificacion: usuario
                    );

                    _logger.LogInformation(
                        "Registrado cambio de precio para producto {Codigo}. " +
                        "Compra: ${PrecioCompraAnterior} → ${PrecioCompraNuevo}, " +
                        "Venta: ${PrecioVentaAnterior} → ${PrecioVentaNuevo}",
                        producto.Codigo,
                        productoExistente.PrecioCompra, producto.PrecioCompra,
                        productoExistente.PrecioVenta, producto.PrecioVenta
                    );
                }

                producto.UpdatedAt = DateTime.UtcNow;
                _context.Entry(productoExistente).CurrentValues.SetValues(producto);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Producto actualizado exitosamente: {Codigo} - {Nombre}", producto.Codigo, producto.Nombre);
                return producto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el producto {Id}", producto.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                {
                    _logger.LogWarning("Intento de eliminar producto inexistente con ID {Id}", id);
                    return false;
                }

                producto.IsDeleted = true;
                producto.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Producto eliminado (soft delete): {Codigo} - {Nombre}", producto.Codigo, producto.Nombre);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el producto {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null)
        {
            try
            {
                var query = _context.Productos.Where(p => p.Codigo == codigo);

                if (excludeId.HasValue)
                {
                    query = query.Where(p => p.Id != excludeId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del código {Codigo}", codigo);
                throw;
            }
        }

        public async Task<Producto> ActualizarStockAsync(int id, decimal cantidad)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                {
                    throw new InvalidOperationException($"No se encontró el producto con ID {id}");
                }

                var nuevoStock = producto.StockActual + cantidad;

                if (nuevoStock < 0)
                {
                    throw new InvalidOperationException(
                        $"No se puede reducir el stock. Stock actual: {producto.StockActual}, " +
                        $"Cantidad solicitada: {Math.Abs(cantidad)}. " +
                        $"Stock resultante sería negativo: {nuevoStock}"
                    );
                }

                producto.StockActual = nuevoStock;
                producto.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Stock actualizado para producto {Codigo}: {StockAnterior} → {StockNuevo} (Δ {Cantidad})",
                    producto.Codigo,
                    producto.StockActual - cantidad,
                    producto.StockActual,
                    cantidad
                );

                return producto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar stock del producto {Id}", id);
                throw;
            }
        }
    }
}