using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;

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
            return await _context.OrdenesCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Marca)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<OrdenCompra> CreateAsync(OrdenCompra ordenCompra)
        {
            // Validar que el número no exista
            if (await NumeroOrdenExisteAsync(ordenCompra.Numero))
            {
                throw new InvalidOperationException($"Ya existe una orden con el número {ordenCompra.Numero}");
            }

            // Validar que el proveedor exista
            var proveedor = await _context.Proveedores.FindAsync(ordenCompra.ProveedorId);
            if (proveedor == null)
            {
                throw new InvalidOperationException("El proveedor especificado no existe");
            }

            // Calcular totales
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

            // Validar que el número no exista en otra orden
            if (await NumeroOrdenExisteAsync(ordenCompra.Numero, ordenCompra.Id))
            {
                throw new InvalidOperationException($"Ya existe otra orden con el número {ordenCompra.Numero}");
            }

            // Calcular totales
            CalcularTotales(ordenCompra);

            // Actualizar propiedades
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

            // Actualizar detalles
            // Eliminar detalles que ya no están
            var detallesAEliminar = ordenExistente.Detalles
                .Where(d => !ordenCompra.Detalles.Any(nd => nd.Id == d.Id))
                .ToList();

            foreach (var detalle in detallesAEliminar)
            {
                _context.OrdenCompraDetalles.Remove(detalle);
            }

            // Agregar o actualizar detalles
            foreach (var detalleNuevo in ordenCompra.Detalles)
            {
                var detalleExistente = ordenExistente.Detalles.FirstOrDefault(d => d.Id == detalleNuevo.Id);
                if (detalleExistente != null)
                {
                    // Actualizar
                    detalleExistente.ProductoId = detalleNuevo.ProductoId;
                    detalleExistente.Cantidad = detalleNuevo.Cantidad;
                    detalleExistente.PrecioUnitario = detalleNuevo.PrecioUnitario;
                    detalleExistente.Subtotal = detalleNuevo.Subtotal;
                    detalleExistente.CantidadRecibida = detalleNuevo.CantidadRecibida;
                }
                else
                {
                    // Agregar
                    detalleNuevo.OrdenCompraId = ordenExistente.Id;
                    ordenExistente.Detalles.Add(detalleNuevo);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Orden de compra {Numero} actualizada exitosamente", ordenCompra.Numero);
            return ordenExistente;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var orden = await GetByIdAsync(id);
            if (orden == null)
            {
                return false;
            }

            // Validar que se pueda eliminar (no debe estar en ciertos estados)
            if (orden.Estado == EstadoOrdenCompra.Recibida || orden.Estado == EstadoOrdenCompra.EnTransito)
            {
                throw new InvalidOperationException("No se puede eliminar una orden que está en tránsito o ya fue recibida");
            }

            // Verificar si tiene cheques asociados
            var tieneCheques = await _context.Cheques.AnyAsync(c => c.OrdenCompraId == id);
            if (tieneCheques)
            {
                throw new InvalidOperationException("No se puede eliminar una orden que tiene cheques asociados");
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

            // Filtro por término de búsqueda
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(o =>
                    o.Numero.Contains(searchTerm) ||
                    o.Proveedor.RazonSocial.Contains(searchTerm) ||
                    o.Proveedor.NombreFantasia.Contains(searchTerm) ||
                    o.Observaciones.Contains(searchTerm));
            }

            // Filtro por proveedor
            if (proveedorId.HasValue)
            {
                query = query.Where(o => o.ProveedorId == proveedorId.Value);
            }

            // Filtro por estado
            if (estado.HasValue)
            {
                query = query.Where(o => o.Estado == estado.Value);
            }

            // Filtro por rango de fechas
            if (fechaDesde.HasValue)
            {
                query = query.Where(o => o.FechaEmision >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                query = query.Where(o => o.FechaEmision <= fechaHasta.Value);
            }

            // Ordenamiento
            query = orderBy?.ToLower() switch
            {
                "numero" => orderDirection == "desc"
                    ? query.OrderByDescending(o => o.Numero)
                    : query.OrderBy(o => o.Numero),
                "proveedor" => orderDirection == "desc"
                    ? query.OrderByDescending(o => o.Proveedor.RazonSocial)
                    : query.OrderBy(o => o.Proveedor.RazonSocial),
                "fechaemision" => orderDirection == "desc"
                    ? query.OrderByDescending(o => o.FechaEmision)
                    : query.OrderBy(o => o.FechaEmision),
                "estado" => orderDirection == "desc"
                    ? query.OrderByDescending(o => o.Estado)
                    : query.OrderBy(o => o.Estado),
                "total" => orderDirection == "desc"
                    ? query.OrderByDescending(o => o.Total)
                    : query.OrderBy(o => o.Total),
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
            if (orden == null)
            {
                return false;
            }

            // Si se marca como recibida, actualizar la fecha de recepción
            if (nuevoEstado == EstadoOrdenCompra.Recibida && orden.Estado != EstadoOrdenCompra.Recibida)
            {
                orden.FechaRecepcion = DateTime.Now;
            }

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

        public async Task<decimal> CalcularTotalOrdenAsync(int ordenId)
        {
            var orden = await GetByIdAsync(ordenId);
            if (orden == null)
            {
                return 0;
            }

            return orden.Total;
        }

        private void CalcularTotales(OrdenCompra ordenCompra)
        {
            // Calcular subtotales de cada detalle
            foreach (var detalle in ordenCompra.Detalles)
            {
                detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
            }

            // Calcular subtotal general
            ordenCompra.Subtotal = ordenCompra.Detalles.Sum(d => d.Subtotal);

            // Aplicar descuento
            var subtotalConDescuento = ordenCompra.Subtotal - ordenCompra.Descuento;

            // Calcular IVA
            ordenCompra.Iva = subtotalConDescuento * 0.21m; // 21% IVA por defecto

            // Calcular total
            ordenCompra.Total = subtotalConDescuento + ordenCompra.Iva;
        }
    }
}