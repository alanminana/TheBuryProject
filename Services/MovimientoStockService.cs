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

        public MovimientoStockService(AppDbContext context, ILogger<MovimientoStockService> logger)
        {
            _context = context;
            _logger = logger;
        }

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
                .Include(m => m.Producto)
                .Include(m => m.OrdenCompra)
                .Where(m => m.ProductoId == productoId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<MovimientoStock>> GetByOrdenCompraIdAsync(int ordenCompraId)
        {
            return await _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.OrdenCompra)
                .Where(m => m.OrdenCompraId == ordenCompraId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<MovimientoStock>> GetByTipoAsync(TipoMovimiento tipo)
        {
            return await _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.OrdenCompra)
                .Where(m => m.Tipo == tipo)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<MovimientoStock>> GetByFechaRangoAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            return await _context.MovimientosStock
                .Include(m => m.Producto)
                .Include(m => m.OrdenCompra)
                .Where(m => m.CreatedAt >= fechaDesde && m.CreatedAt <= fechaHasta)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<MovimientoStock> CreateAsync(MovimientoStock movimiento)
        {
            _context.MovimientosStock.Add(movimiento);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Movimiento de stock registrado: Producto {ProductoId}, Tipo {Tipo}, Cantidad {Cantidad}",
                movimiento.ProductoId, movimiento.Tipo, movimiento.Cantidad);

            return movimiento;
        }
        public async Task<MovimientoStock> RegistrarAjusteAsync(
            int productoId,
            TipoMovimiento tipo,
            decimal cantidad,
            string? referencia,
            string motivo)
        {
            // Obtener el producto
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null)
            {
                throw new InvalidOperationException("Producto no encontrado");
            }

            // Validar que no quede en stock negativo para salidas
            if (tipo == TipoMovimiento.Salida && producto.StockActual < cantidad)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente. Stock actual: {producto.StockActual}, cantidad solicitada: {cantidad}");
            }

            var stockAnterior = producto.StockActual;

            // Calcular nuevo stock según el tipo de movimiento
            switch (tipo)
            {
                case TipoMovimiento.Entrada:
                    producto.StockActual += cantidad;
                    break;
                case TipoMovimiento.Salida:
                    producto.StockActual -= cantidad;
                    break;
                case TipoMovimiento.Ajuste:
                    // Para ajustes, la cantidad representa el nuevo stock total
                    producto.StockActual = cantidad;
                    break;
            }

            // Crear el movimiento
            var movimiento = new MovimientoStock
            {
                ProductoId = productoId,
                Tipo = tipo,
                Cantidad = tipo == TipoMovimiento.Ajuste
                    ? cantidad - stockAnterior  // Para ajustes, guardamos la diferencia
                    : cantidad,
                StockAnterior = stockAnterior,
                StockNuevo = producto.StockActual,
                Referencia = referencia,
                Motivo = motivo,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System" // TODO: Obtener usuario actual
            };

            _context.MovimientosStock.Add(movimiento);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Ajuste de stock registrado: Producto {ProductoId}, Tipo {Tipo}, Cantidad {Cantidad}",
                productoId, tipo, cantidad);

            return movimiento;
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
                .AsQueryable();

            // Filtros
            if (productoId.HasValue)
            {
                query = query.Where(m => m.ProductoId == productoId.Value);
            }

            if (tipo.HasValue)
            {
                query = query.Where(m => m.Tipo == tipo.Value);
            }

            if (fechaDesde.HasValue)
            {
                query = query.Where(m => m.CreatedAt >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= fechaHasta.Value);
            }

            // Ordenamiento
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
    }
}