using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    public class OrdenCompraService : IOrdenCompraService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrdenCompraService> _logger;

        public OrdenCompraService(AppDbContext context, ILogger<OrdenCompraService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<OrdenCompra>> GetAllAsync()
        {
            return await _context.OrdenesCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(o => o.FechaEmision)
                .ToListAsync();
        }

        public async Task<OrdenCompra?> GetByIdAsync(int id)
        {
            _logger.LogInformation("=== GetByIdAsync - Orden {Id} ===", id);

            var orden = await _context.OrdenesCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Marca)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
            {
                _logger.LogWarning("Orden {Id} NO encontrada", id);
                _logger.LogInformation("=== FIN GetByIdAsync ===");
                return null;
            }

            _logger.LogInformation("Orden encontrada: {Numero}", orden.Numero);
            _logger.LogInformation("Proveedor: {Proveedor}", orden.Proveedor?.RazonSocial ?? "NULL");
            _logger.LogInformation("Detalles Count: {Count}", orden.Detalles?.Count ?? 0);

            var detalles = orden.Detalles ?? new List<OrdenCompraDetalle>();
            _logger.LogInformation("Detalles Count: {Count}", detalles.Count);

            foreach (var d in detalles)
            {
                if (d == null) continue;

                _logger.LogInformation(
                    "Detalle {Id} - ProdId {ProdId} - Cant {Cant} - Rec {Rec} - Prod {Prod}",
                    d.Id,
                    d.ProductoId,
                    d.Cantidad,
                    d.CantidadRecibida,
                    d.Producto?.Nombre ?? "NULL"
                );
            }

            _logger.LogInformation("=== FIN GetByIdAsync ===");
            return orden;
        }

        public async Task<OrdenCompra> CreateAsync(OrdenCompra ordenCompra)
        {
            if (await NumeroOrdenExisteAsync(ordenCompra.Numero))
            {
                throw new InvalidOperationException($"Ya existe una orden con el número {ordenCompra.Numero}");
            }

            var proveedor = await _context.Proveedores
                .Include(p => p.ProveedorProductos)
                .FirstOrDefaultAsync(p => p.Id == ordenCompra.ProveedorId);

            if (proveedor == null)
            {
                throw new InvalidOperationException("El proveedor especificado no existe");
            }

            if (proveedor.ProveedorProductos.Any())
            {
                var productosAsociadosIds = proveedor.ProveedorProductos
                    .Select(pp => pp.ProductoId)
                    .ToList();

                var productosNoAsociados = new List<string>();

                foreach (var detalle in ordenCompra.Detalles)
                {
                    if (!productosAsociadosIds.Contains(detalle.ProductoId))
                    {
                        var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                        productosNoAsociados.Add(producto?.Nombre ?? $"ID {detalle.ProductoId}");
                    }
                }

                if (productosNoAsociados.Any())
                {
                    throw new InvalidOperationException(
                        $"No se puede crear la orden. Productos no asociados al proveedor '{proveedor.RazonSocial}': {string.Join(", ", productosNoAsociados)}.");
                }
            }

            CalcularTotales(ordenCompra);
            _context.OrdenesCompra.Add(ordenCompra);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Orden de compra {Numero} creada exitosamente", ordenCompra.Numero);

            return ordenCompra;
        }

        public async Task<OrdenCompra> UpdateAsync(OrdenCompra ordenCompra)
        {
            var ordenExistente = await GetByIdAsync(ordenCompra.Id);
            if (ordenExistente == null)
            {
                throw new InvalidOperationException("La orden de compra no existe");
            }

            if (await NumeroOrdenExisteAsync(ordenCompra.Numero, ordenCompra.Id))
            {
                throw new InvalidOperationException($"Ya existe otra orden con el número {ordenCompra.Numero}");
            }

            CalcularTotales(ordenCompra);

            ordenExistente.Numero = ordenCompra.Numero;
            ordenExistente.ProveedorId = ordenCompra.ProveedorId;
            ordenExistente.FechaEmision = ordenCompra.FechaEmision;
            ordenExistente.FechaEntregaEstimada = ordenCompra.FechaEntregaEstimada;
            ordenExistente.FechaRecepcion = ordenCompra.FechaRecepcion;
            ordenExistente.Estado = ordenCompra.Estado;
            ordenExistente.Subtotal = ordenCompra.Subtotal;
            ordenExistente.Descuento = ordenCompra.Descuento;
            ordenExistente.Iva = ordenCompra.Iva;
            ordenExistente.Total = ordenCompra.Total;
            ordenExistente.Observaciones = ordenCompra.Observaciones;

            var existingDetalles = ordenExistente.Detalles ?? new List<OrdenCompraDetalle>();

            var detallesAEliminar = existingDetalles
                .Where(d => !ordenCompra.Detalles.Any(nd => nd.Id == d.Id))
                .ToList();

            foreach (var detalle in detallesAEliminar)
            {
                _context.OrdenCompraDetalles.Remove(detalle);
            }

            foreach (var detalleNuevo in ordenCompra.Detalles)
            {
                var detalleExistente = existingDetalles.FirstOrDefault(d => d.Id == detalleNuevo.Id);
                if (detalleExistente != null)
                {
                    detalleExistente.ProductoId = detalleNuevo.ProductoId;
                    detalleExistente.Cantidad = detalleNuevo.Cantidad;
                    detalleExistente.PrecioUnitario = detalleNuevo.PrecioUnitario;
                    detalleExistente.Subtotal = detalleNuevo.Subtotal;
                    detalleExistente.CantidadRecibida = detalleNuevo.CantidadRecibida;
                }
                else
                {
                    detalleNuevo.OrdenCompraId = ordenExistente.Id;
                    (ordenExistente.Detalles ??= new List<OrdenCompraDetalle>())
                        .Add(detalleNuevo);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Orden de compra {Numero} actualizada exitosamente", ordenCompra.Numero);
            return ordenExistente;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var orden = await GetByIdAsync(id);
            if (orden == null) return false;

            if (orden.Estado == EstadoOrdenCompra.Recibida || orden.Estado == EstadoOrdenCompra.EnTransito)
            {
                throw new InvalidOperationException("No se puede eliminar una orden en tránsito o recibida");
            }

            if (await _context.Cheques.AnyAsync(c => c.OrdenCompraId == id))
            {
                throw new InvalidOperationException("No se puede eliminar una orden con cheques asociados");
            }

            _context.OrdenesCompra.Remove(orden);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Orden de compra {Id} eliminada exitosamente", id);
            return true;
        }

        public async Task<IEnumerable<OrdenCompra>> SearchAsync(
            string? searchTerm = null,
            int? proveedorId = null,
            EstadoOrdenCompra? estado = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            string? orderBy = null,
            string? orderDirection = "asc")
        {
            var query = _context.OrdenesCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Detalles)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(o =>
                    o.Numero.Contains(searchTerm) ||
                    (o.Proveedor != null && o.Proveedor.RazonSocial.Contains(searchTerm)) ||
                    (o.Proveedor != null && o.Proveedor.NombreFantasia != null && o.Proveedor.NombreFantasia.Contains(searchTerm)) ||
                    (o.Observaciones != null && o.Observaciones.Contains(searchTerm)));
            }

            if (proveedorId.HasValue)
                query = query.Where(o => o.ProveedorId == proveedorId.Value);

            if (estado.HasValue)
                query = query.Where(o => o.Estado == estado.Value);

            if (fechaDesde.HasValue)
                query = query.Where(o => o.FechaEmision >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(o => o.FechaEmision <= fechaHasta.Value);

            query = orderBy?.ToLower() switch
            {
                "numero" => orderDirection == "desc" ? query.OrderByDescending(o => o.Numero) : query.OrderBy(o => o.Numero),
                "proveedor" => orderDirection == "desc" ? query.OrderByDescending(o => o.Proveedor.RazonSocial) : query.OrderBy(o => o.Proveedor.RazonSocial),
                "fechaemision" => orderDirection == "desc" ? query.OrderByDescending(o => o.FechaEmision) : query.OrderBy(o => o.FechaEmision),
                "estado" => orderDirection == "desc" ? query.OrderByDescending(o => o.Estado) : query.OrderBy(o => o.Estado),
                "total" => orderDirection == "desc" ? query.OrderByDescending(o => o.Total) : query.OrderBy(o => o.Total),
                _ => query.OrderByDescending(o => o.FechaEmision)
            };

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<OrdenCompra>> GetByProveedorIdAsync(int proveedorId)
        {
            return await _context.OrdenesCompra
                .Include(o => o.Detalles)
                .Where(o => o.ProveedorId == proveedorId)
                .OrderByDescending(o => o.FechaEmision)
                .ToListAsync();
        }

        public async Task<bool> CambiarEstadoAsync(int id, EstadoOrdenCompra nuevoEstado)
        {
            var orden = await _context.OrdenesCompra.FindAsync(id);
            if (orden == null) return false;

            if (nuevoEstado == EstadoOrdenCompra.Recibida && orden.Estado != EstadoOrdenCompra.Recibida)
                orden.FechaRecepcion = DateTime.Now;

            orden.Estado = nuevoEstado;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Estado de orden {Id} cambiado a {Estado}", id, nuevoEstado);
            return true;
        }

        public async Task<bool> NumeroOrdenExisteAsync(string numero, int? excludeId = null)
        {
            return await _context.OrdenesCompra
                .AnyAsync(o => o.Numero == numero && (excludeId == null || o.Id != excludeId.Value));
        }

        public async Task<OrdenCompra> RecepcionarAsync(int ordenId, List<RecepcionDetalleViewModel> detallesRecepcion)
        {
            var orden = await GetByIdAsync(ordenId);
            if (orden == null)
                throw new InvalidOperationException("Orden no encontrada");

            if (orden.Estado != EstadoOrdenCompra.Confirmada &&
                orden.Estado != EstadoOrdenCompra.EnTransito)
            {
                throw new InvalidOperationException("Solo se pueden recepcionar órdenes confirmadas o en tránsito");
            }

            bool todosRecibidos = true;

            foreach (var recepcion in detallesRecepcion)
            {
                if (recepcion.CantidadARecepcionar <= 0) continue;

                var detalle = orden.Detalles?.FirstOrDefault(d => d.Id == recepcion.DetalleId);
                if (detalle == null) continue;

                int cantidadSolicitada = detalle.Cantidad;
                int cantidadRecibidaActual = detalle.CantidadRecibida;

                int totalRecibido = cantidadRecibidaActual + recepcion.CantidadARecepcionar;

                if (totalRecibido > cantidadSolicitada)
                {
                    throw new InvalidOperationException(
                        $"No se puede recepcionar más de lo solicitado para {detalle.Producto?.Nombre ?? "producto"}"
                    );
                }

                detalle.CantidadRecibida = totalRecibido;

                var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                if (producto != null)
                {
                    var stockAnterior = producto.StockActual;
                    producto.StockActual += recepcion.CantidadARecepcionar;

                    var movimiento = new MovimientoStock
                    {
                        ProductoId = producto.Id,
                        Tipo = TipoMovimiento.Entrada,
                        Cantidad = recepcion.CantidadARecepcionar,
                        StockAnterior = stockAnterior,
                        StockNuevo = producto.StockActual,
                        Referencia = $"Orden de Compra {orden.Numero}",
                        OrdenCompraId = orden.Id,
                        Motivo = "Recepción de mercadería",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.MovimientosStock.Add(movimiento);
                }

                if (detalle.CantidadRecibida < cantidadSolicitada)
                    todosRecibidos = false;
            }

            if (todosRecibidos)
            {
                orden.Estado = EstadoOrdenCompra.Recibida;
                orden.FechaRecepcion = DateTime.Now;
            }
            else if (orden.Estado == EstadoOrdenCompra.Confirmada)
            {
                orden.Estado = EstadoOrdenCompra.EnTransito;
            }

            await _context.SaveChangesAsync();
            return orden;
        }

        public async Task<decimal> CalcularTotalOrdenAsync(int ordenId)
        {
            var orden = await GetByIdAsync(ordenId);
            return orden?.Total ?? 0;
        }

        private void CalcularTotales(OrdenCompra ordenCompra)
        {
            foreach (var detalle in ordenCompra.Detalles)
            {
                detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
            }

            ordenCompra.Subtotal = ordenCompra.Detalles.Sum(d => d.Subtotal);
            var subtotalConDescuento = ordenCompra.Subtotal - ordenCompra.Descuento;
            ordenCompra.Iva = subtotalConDescuento * 0.21m;
            ordenCompra.Total = subtotalConDescuento + ordenCompra.Iva;
        }
    }
}