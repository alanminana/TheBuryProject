using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Data.Interfaces;
using TheBuryProject.Data.Repositories;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Servicio para la gestión de productos
    /// </summary>
    public class ProductoService : IProductoService
    {
        private readonly IRepository<Producto> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductoService> _logger;

        public ProductoService(
            IRepository<Producto> repository,
            IUnitOfWork unitOfWork,
            ILogger<ProductoService> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<Producto>> GetAllAsync()
        {
            try
            {
                return await _repository.GetAllAsync(
                    include: q => q.Include(p => p.Categoria)
                                   .Include(p => p.Marca)
                );
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
                return await _repository.GetByIdAsync(
                    id,
                    include: q => q.Include(p => p.Categoria)
                                   .Include(p => p.Marca)
                );
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
                return await _repository.FindAsync(
                    p => p.CategoriaId == categoriaId,
                    include: q => q.Include(p => p.Categoria)
                                   .Include(p => p.Marca)
                );
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
                return await _repository.FindAsync(
                    p => p.MarcaId == marcaId,
                    include: q => q.Include(p => p.Categoria)
                                   .Include(p => p.Marca)
                );
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
                return await _repository.FindAsync(
                    p => p.StockActual <= p.StockMinimo,
                    include: q => q.Include(p => p.Categoria)
                                   .Include(p => p.Marca)
                );
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
                // Validar que el código no esté vacío
                if (string.IsNullOrWhiteSpace(producto.Codigo))
                {
                    throw new InvalidOperationException("El código del producto no puede estar vacío");
                }

                // Validar que el nombre no esté vacío
                if (string.IsNullOrWhiteSpace(producto.Nombre))
                {
                    throw new InvalidOperationException("El nombre del producto no puede estar vacío");
                }

                // Validar que el código no exista
                if (await ExistsCodigoAsync(producto.Codigo))
                {
                    throw new InvalidOperationException($"Ya existe un producto con el código '{producto.Codigo}'");
                }

                // Validar que el precio de venta sea mayor o igual al precio de compra
                if (producto.PrecioVenta < producto.PrecioCompra)
                {
                    _logger.LogWarning(
                        "Creando producto {Codigo} con precio de venta ({PrecioVenta}) menor al precio de compra ({PrecioCompra})",
                        producto.Codigo,
                        producto.PrecioVenta,
                        producto.PrecioCompra
                    );
                }

                // Validar precios positivos
                if (producto.PrecioCompra < 0)
                {
                    throw new InvalidOperationException("El precio de compra no puede ser negativo");
                }

                if (producto.PrecioVenta < 0)
                {
                    throw new InvalidOperationException("El precio de venta no puede ser negativo");
                }

                // Validar stock no negativo
                if (producto.StockActual < 0)
                {
                    throw new InvalidOperationException("El stock actual no puede ser negativo");
                }

                if (producto.StockMinimo < 0)
                {
                    throw new InvalidOperationException("El stock mínimo no puede ser negativo");
                }

                producto.CreatedAt = DateTime.UtcNow;
                await _repository.AddAsync(producto);
                await _unitOfWork.CommitAsync();

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
                // Verificar que el producto existe
                var productoExistente = await _repository.GetByIdAsync(producto.Id);
                if (productoExistente == null)
                {
                    throw new InvalidOperationException($"No se encontró el producto con ID {producto.Id}");
                }

                // Validar que el código no esté vacío
                if (string.IsNullOrWhiteSpace(producto.Codigo))
                {
                    throw new InvalidOperationException("El código del producto no puede estar vacío");
                }

                // Validar que el nombre no esté vacío
                if (string.IsNullOrWhiteSpace(producto.Nombre))
                {
                    throw new InvalidOperationException("El nombre del producto no puede estar vacío");
                }

                // Validar que el código no exista en otro producto
                if (await ExistsCodigoAsync(producto.Codigo, producto.Id))
                {
                    throw new InvalidOperationException($"Ya existe otro producto con el código '{producto.Codigo}'");
                }

                // Validar precios
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

                // Validar stock no negativo
                if (producto.StockActual < 0)
                {
                    throw new InvalidOperationException("El stock actual no puede ser negativo");
                }

                if (producto.StockMinimo < 0)
                {
                    throw new InvalidOperationException("El stock mínimo no puede ser negativo");
                }

                producto.UpdatedAt = DateTime.UtcNow;
                _repository.Update(producto);
                await _unitOfWork.CommitAsync();

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
                var producto = await _repository.GetByIdAsync(id);
                if (producto == null)
                {
                    _logger.LogWarning("Intento de eliminar producto inexistente con ID {Id}", id);
                    return false;
                }

                // Soft delete
                producto.IsDeleted = true;
                producto.UpdatedAt = DateTime.UtcNow;
                _repository.Update(producto);
                await _unitOfWork.CommitAsync();

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
                var productos = await _repository.FindAsync(p => p.Codigo == codigo);

                if (excludeId.HasValue)
                {
                    productos = productos.Where(p => p.Id != excludeId.Value);
                }

                return productos.Any();
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
                var producto = await _repository.GetByIdAsync(id);
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
                _repository.Update(producto);
                await _unitOfWork.CommitAsync();

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