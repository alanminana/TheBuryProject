using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    /// <summary>
    /// ✅ REFACTORIZADO: Servicio centralizado para productos
    /// - Validaciones centralizadas
    /// - Eliminada duplicación
    /// - Logging mejorado
    /// </summary>
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

        #region CRUD Básico

        public async Task<IEnumerable<Producto>> GetAllAsync()
        {
            try
            {
                return await _context.Productos
                    .Where(p => !p.IsDeleted)
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

        public async Task<Producto?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Marca)
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto {Id}", id);
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
                    .Where(p => p.CategoriaId == categoriaId && !p.IsDeleted)
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
                    .Where(p => p.MarcaId == marcaId && !p.IsDeleted)
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
                    .Where(p => p.StockActual <= p.StockMinimo && !p.IsDeleted)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos con stock bajo");
                throw;
            }
        }

        #endregion

        #region Búsqueda

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
                    .Where(p => !p.IsDeleted)
                    .AsQueryable();

                // ✅ BÚSQUEDA
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(p =>
                        p.Codigo.ToLower().Contains(searchTerm) ||
                        p.Nombre.ToLower().Contains(searchTerm) ||
                        (p.Descripcion != null && p.Descripcion.ToLower().Contains(searchTerm)));
                }

                // ✅ FILTROS
                if (categoriaId.HasValue)
                    query = query.Where(p => p.CategoriaId == categoriaId.Value);

                if (marcaId.HasValue)
                    query = query.Where(p => p.MarcaId == marcaId.Value);

                if (stockBajo)
                    query = query.Where(p => p.StockActual <= p.StockMinimo);

                if (soloActivos)
                    query = query.Where(p => p.Activo);

                // ✅ ORDENAMIENTO
                var ascending = orderDirection?.ToLower() != "desc";
                query = orderBy?.ToLower() switch
                {
                    "codigo" => ascending ? query.OrderBy(p => p.Codigo) : query.OrderByDescending(p => p.Codigo),
                    "nombre" => ascending ? query.OrderBy(p => p.Nombre) : query.OrderByDescending(p => p.Nombre),
                    "preciocompra" => ascending ? query.OrderBy(p => p.PrecioCompra) : query.OrderByDescending(p => p.PrecioCompra),
                    "precioventa" => ascending ? query.OrderBy(p => p.PrecioVenta) : query.OrderByDescending(p => p.PrecioVenta),
                    "stock" => ascending ? query.OrderBy(p => p.StockActual) : query.OrderByDescending(p => p.StockActual),
                    "categoria" => ascending ? query.OrderBy(p => p.Categoria.Nombre) : query.OrderByDescending(p => p.Categoria.Nombre),
                    "marca" => ascending ? query.OrderBy(p => p.Marca.Nombre) : query.OrderByDescending(p => p.Marca.Nombre),
                    _ => query.OrderBy(p => p.Nombre)
                };

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar productos");
                throw;
            }
        }

        #endregion

        #region Crear / Actualizar

        public async Task<Producto> CreateAsync(Producto producto)
        {
            try
            {
                // ✅ VALIDAR CENTRALIZADO
                ValidarProducto(producto);

                if (await ExistsCodigoAsync(producto.Codigo))
                {
                    throw new InvalidOperationException($"Ya existe un producto con el código '{producto.Codigo}'");
                }

                producto.CreatedAt = DateTime.UtcNow;
                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Producto creado: {Codigo} - {Nombre}", producto.Codigo, producto.Nombre);
                return producto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto {Codigo}", producto.Codigo);
                throw;
            }
        }

        public async Task<Producto> UpdateAsync(Producto producto)
        {
            try
            {
                var existing = await _context.Productos.FindAsync(producto.Id);
                if (existing == null)
                {
                    throw new InvalidOperationException($"No se encontró el producto con ID {producto.Id}");
                }

                // ✅ VALIDAR CENTRALIZADO
                ValidarProducto(producto);

                if (await ExistsCodigoAsync(producto.Codigo, producto.Id))
                {
                    throw new InvalidOperationException($"Ya existe otro producto con el código '{producto.Codigo}'");
                }

                // Registrar cambio de precios
                bool preciosCambiaron =
                    existing.PrecioCompra != producto.PrecioCompra ||
                    existing.PrecioVenta != producto.PrecioVenta;

                if (preciosCambiaron)
                {
                    var usuario = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
                    await _precioHistoricoService.RegistrarCambioAsync(
                        productoId: producto.Id,
                        precioCompraAnterior: existing.PrecioCompra,
                        precioCompraNuevo: producto.PrecioCompra,
                        precioVentaAnterior: existing.PrecioVenta,
                        precioVentaNuevo: producto.PrecioVenta,
                        motivoCambio: null,
                        usuarioModificacion: usuario
                    );

                    _logger.LogInformation(
                        "Precio actualizado para {Codigo}: Compra ${PrecioAnterior} → ${PrecioNuevo}",
                        producto.Codigo, existing.PrecioCompra, producto.PrecioCompra);
                }

                // Actualizar propiedades
                existing.Codigo = producto.Codigo;
                existing.Nombre = producto.Nombre;
                existing.Descripcion = producto.Descripcion;
                existing.CategoriaId = producto.CategoriaId;
                existing.MarcaId = producto.MarcaId;
                existing.PrecioCompra = producto.PrecioCompra;
                existing.PrecioVenta = producto.PrecioVenta;
                existing.RequiereNumeroSerie = producto.RequiereNumeroSerie;
                existing.StockMinimo = producto.StockMinimo;
                existing.StockActual = producto.StockActual;
                existing.UnidadMedida = producto.UnidadMedida;
                existing.Activo = producto.Activo;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Producto actualizado: {Codigo} - {Nombre}", producto.Codigo, producto.Nombre);
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar producto {Id}", producto.Id);
                throw;
            }
        }

        #endregion

        #region Eliminar / Stock

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                {
                    _logger.LogWarning("Intento de eliminar producto inexistente {Id}", id);
                    return false;
                }

                producto.IsDeleted = true;
                producto.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Producto eliminado (soft delete): {Codigo}", producto.Codigo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto {Id}", id);
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
                        $"No se puede reducir el stock. Actual: {producto.StockActual}, " +
                        $"Solicitado: {Math.Abs(cantidad)}. Sería negativo: {nuevoStock}");
                }

                producto.StockActual = nuevoStock;
                producto.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Stock actualizado para {Codigo}: {StockAnterior} → {StockNuevo} (Δ {Cantidad})",
                    producto.Codigo,
                    producto.StockActual - cantidad,
                    producto.StockActual,
                    cantidad);

                return producto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar stock del producto {Id}", id);
                throw;
            }
        }

        #endregion

        #region Validaciones

        public async Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null)
        {
            try
            {
                var query = _context.Productos.Where(p => p.Codigo == codigo && !p.IsDeleted);

                if (excludeId.HasValue)
                    query = query.Where(p => p.Id != excludeId.Value);

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar código {Codigo}", codigo);
                throw;
            }
        }

        // ✅ CENTRALIZADO: Método privado para validar
        private void ValidarProducto(Producto producto)
        {
            if (string.IsNullOrWhiteSpace(producto.Codigo))
            {
                throw new InvalidOperationException("El código del producto no puede estar vacío");
            }

            if (string.IsNullOrWhiteSpace(producto.Nombre))
            {
                throw new InvalidOperationException("El nombre del producto no puede estar vacío");
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
                    "Producto {Codigo}: Precio venta (${PV}) < Precio compra (${PC})",
                    producto.Codigo, producto.PrecioVenta, producto.PrecioCompra);
            }

            if (producto.StockActual < 0)
            {
                throw new InvalidOperationException("El stock actual no puede ser negativo");
            }

            if (producto.StockMinimo < 0)
            {
                throw new InvalidOperationException("El stock mínimo no puede ser negativo");
            }
        }

        #endregion
    }
}