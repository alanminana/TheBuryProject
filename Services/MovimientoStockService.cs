// ✅ REFACTORIZADO: Transacciones, sin duplicación, optimizado

using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    public class MovimientoStockService : IMovimientoStockService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MovimientoStockService> _logger;

        public MovimientoStockService(
            AppDbContext context,
            ILogger<MovimientoStockService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Obtener Movimientos

        public async Task<IEnumerable<MovimientoStock>> GetAllAsync()
        {
            return await _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.OrdenCompra)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<MovimientoStock?> GetByIdAsync(int id)
        {
            return await _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.OrdenCompra)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<MovimientoStock>> GetByProductoIdAsync(int productoId)
        {
            return await _context.MovimientosStock
                .Include(m => m.OrdenCompra)
                .Where(m => m.ProductoId == productoId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<MovimientoStock>> GetByOrdenCompraIdAsync(int ordenCompraId)
        {
            return await _context.MovimientosStock
                .Include(m => m.Producto)
                .Where(m => m.OrdenCompraId == ordenCompraId && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<MovimientoStock>> GetByTipoAsync(TipoMovimiento tipo)
        {
            return await _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.OrdenCompra)
                .Where(m => m.Tipo == tipo && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<MovimientoStock>> GetByFechaRangoAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            return await _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.OrdenCompra)
                .Where(m => m.CreatedAt >= fechaDesde && m.CreatedAt <= fechaHasta && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<MovimientoStock>> SearchAsync(
            int? productoId = null,
            TipoMovimiento? tipo = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            string? orderBy = null,
            string? orderDirection = "desc")
        {
            var query = _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.OrdenCompra)
                .Where(m => !m.IsDeleted)
                .AsQueryable();

            if (productoId.HasValue)
                query = query.Where(m => m.ProductoId == productoId.Value);

            if (tipo.HasValue)
                query = query.Where(m => m.Tipo == tipo.Value);

            if (fechaDesde.HasValue)
                query = query.Where(m => m.CreatedAt >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(m => m.CreatedAt <= fechaHasta.Value);

            query = orderBy?.ToLower() switch
            {
                "fecha" => orderDirection == "desc"
                    ? query.OrderByDescending(m => m.CreatedAt)
                    : query.OrderBy(m => m.CreatedAt),
                "producto" => orderDirection == "desc"
                    ? query.OrderByDescending(m => m.Producto.Nombre)
                    : query.OrderBy(m => m.Producto.Nombre),
                "tipo" => orderDirection == "desc"
                    ? query.OrderByDescending(m => m.Tipo)
                    : query.OrderBy(m => m.Tipo),
                "cantidad" => orderDirection == "desc"
                    ? query.OrderByDescending(m => m.Cantidad)
                    : query.OrderBy(m => m.Cantidad),
                _ => query.OrderByDescending(m => m.CreatedAt)
            };

            return await query.ToListAsync();
        }

        #endregion

        #region Crear / Actualizar Stock

        public async Task<MovimientoStock> CreateAsync(MovimientoStock movimiento)
        {
            _context.MovimientosStock.Add(movimiento);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Movimiento registrado: Producto {ProductoId}, Tipo {Tipo}, Cantidad {Cantidad}",
                movimiento.ProductoId, movimiento.Tipo, movimiento.Cantidad);

            return movimiento;
        }

        /// <summary>
        /// ✅ MEJORADO: Con TRANSACCIÓN, validación de cantidad y usuario real
        /// </summary>
        public async Task<MovimientoStock> RegistrarAjusteAsync(
            int productoId,
            TipoMovimiento tipo,
            decimal cantidad,
            string? referencia,
            string motivo,
            string? usuarioActual = null)
        {
            // ✅ VALIDACIÓN 1: Cantidad debe ser positiva
            var (valido, mensaje) = await ValidarCantidadAsync(cantidad);
            if (!valido)
            {
                throw new InvalidOperationException(mensaje);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var producto = await _context.Productos
                        .FirstOrDefaultAsync(p => p.Id == productoId && !p.IsDeleted);

                    if (producto == null)
                    {
                        throw new InvalidOperationException($"Producto {productoId} no encontrado");
                    }

                    // ✅ VALIDACIÓN 2: Stock insuficiente para salidas
                    if (tipo == TipoMovimiento.Salida && producto.StockActual < cantidad)
                    {
                        throw new InvalidOperationException(
                            $"Stock insuficiente. Disponible: {producto.StockActual}, Solicitado: {cantidad}");
                    }

                    var stockAnterior = producto.StockActual;

                    // Actualizar stock según tipo
                    switch (tipo)
                    {
                        case TipoMovimiento.Entrada:
                            producto.StockActual += cantidad;
                            break;
                        case TipoMovimiento.Salida:
                            producto.StockActual -= cantidad;
                            break;
                        case TipoMovimiento.Ajuste:
                            producto.StockActual = cantidad;
                            break;
                    }

                    producto.UpdatedAt = DateTime.UtcNow;

                    // Crear movimiento con usuario real
                    var movimiento = new MovimientoStock
                    {
                        ProductoId = productoId,
                        Tipo = tipo,
                        Cantidad = tipo == TipoMovimiento.Ajuste ? cantidad - stockAnterior : cantidad,
                        StockAnterior = stockAnterior,
                        StockNuevo = producto.StockActual,
                        Referencia = referencia,
                        Motivo = motivo,
                        CreatedAt = DateTime.UtcNow,
                        // ✅ MEJORADO: Usar usuario actual, no hardcodeado
                        CreatedBy = string.IsNullOrWhiteSpace(usuarioActual) ? "Sistema" : usuarioActual
                    };

                    _context.MovimientosStock.Add(movimiento);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Ajuste registrado (TRANSACCIÓN): Producto {ProductoId}, Tipo {Tipo}, " +
                        "Stock {Anterior} → {Nuevo}, Usuario: {Usuario}",
                        productoId, tipo, stockAnterior, producto.StockActual, 
                        usuarioActual ?? "Sistema");

                    return movimiento;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error en RegistrarAjusteAsync - Transacción revertida");
                    throw;
                }
            }
        }

        /// <summary>
        /// ✅ NUEVO: Validar que cantidad sea positiva
        /// </summary>
        public async Task<(bool Valido, string Mensaje)> ValidarCantidadAsync(decimal cantidad)
        {
            // Ejecutar en Task para coincidir con interfaz async
            return await Task.FromResult(ValidarCantidadSync(cantidad));
        }

        private (bool Valido, string Mensaje) ValidarCantidadSync(decimal cantidad)
        {
            if (cantidad <= 0)
            {
                return (false, "La cantidad debe ser mayor a 0");
            }

            if (cantidad > 999999.99m)
            {
                return (false, "La cantidad no puede exceder 999999.99");
            }

            return (true, "Cantidad válida");
        }

        /// <summary>
        /// ✅ NUEVO: Validar disponibilidad de stock
        /// </summary>
        public async Task<bool> HayStockDisponibleAsync(int productoId, decimal cantidad)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productoId && !p.IsDeleted);

            return producto != null && producto.StockActual >= cantidad;
        }

        #endregion
    }
}