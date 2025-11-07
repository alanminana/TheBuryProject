using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    public class VentaService : IVentaService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<VentaService> _logger;

        public VentaService(AppDbContext context, IMapper mapper, ILogger<VentaService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<VentaViewModel>> GetAllAsync(VentaFilterViewModel? filter = null)
        {
            try
            {
                var query = _context.Ventas
                    .Include(v => v.Cliente)
                    .Include(v => v.Credito)
                    .Include(v => v.Detalles)
                        .ThenInclude(d => d.Producto)
                    .Include(v => v.Facturas)
                    .AsQueryable();

                // Aplicar filtros
                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.Numero))
                        query = query.Where(v => v.Numero.Contains(filter.Numero));

                    if (!string.IsNullOrWhiteSpace(filter.Cliente))
                        query = query.Where(v =>
                            v.Cliente.NumeroDocumento.Contains(filter.Cliente) ||
                            v.Cliente.Nombre.Contains(filter.Cliente) ||
                            v.Cliente.Apellido.Contains(filter.Cliente));

                    if (filter.Estado.HasValue)
                        query = query.Where(v => v.Estado == filter.Estado.Value);

                    if (filter.TipoPago.HasValue)
                        query = query.Where(v => v.TipoPago == filter.TipoPago.Value);

                    if (filter.FechaDesde.HasValue)
                        query = query.Where(v => v.FechaVenta >= filter.FechaDesde.Value);

                    if (filter.FechaHasta.HasValue)
                        query = query.Where(v => v.FechaVenta <= filter.FechaHasta.Value);

                    if (filter.MontoMinimo.HasValue)
                        query = query.Where(v => v.Total >= filter.MontoMinimo.Value);

                    if (filter.MontoMaximo.HasValue)
                        query = query.Where(v => v.Total <= filter.MontoMaximo.Value);
                }

                var ventas = await query
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

                var viewModels = _mapper.Map<List<VentaViewModel>>(ventas);

                // Mapear nombres manualmente
                foreach (var vm in viewModels)
                {
                    var venta = ventas.First(v => v.Id == vm.Id);
                    vm.ClienteNombre = $"{venta.Cliente.Apellido}, {venta.Cliente.Nombre}";
                    vm.ClienteDocumento = venta.Cliente.NumeroDocumento;
                    if (venta.Credito != null)
                        vm.CreditoNumero = venta.Credito.Numero;
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas");
                throw;
            }
        }

        public async Task<VentaViewModel?> GetByIdAsync(int id)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Cliente)
                    .Include(v => v.Credito)
                    .Include(v => v.Detalles)
                        .ThenInclude(d => d.Producto)
                    .Include(v => v.Facturas)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (venta == null)
                    return null;

                var viewModel = _mapper.Map<VentaViewModel>(venta);
                viewModel.ClienteNombre = $"{venta.Cliente.Apellido}, {venta.Cliente.Nombre}";
                viewModel.ClienteDocumento = venta.Cliente.NumeroDocumento;
                if (venta.Credito != null)
                    viewModel.CreditoNumero = venta.Credito.Numero;

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener venta por ID: {Id}", id);
                throw;
            }
        }

        public async Task<VentaViewModel> CreateAsync(VentaViewModel viewModel)
        {
            try
            {
                // Generar número de venta
                viewModel.Numero = await GenerarNumeroVentaAsync();
                viewModel.Estado = EstadoVenta.Presupuesto;

                // Calcular totales
                CalcularTotales(viewModel);

                var venta = _mapper.Map<Venta>(viewModel);
                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                viewModel.Id = venta.Id;
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear venta");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(VentaViewModel viewModel)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Detalles)
                    .FirstOrDefaultAsync(v => v.Id == viewModel.Id);

                if (venta == null)
                    return false;

                // Solo se puede editar si está en Presupuesto
                if (venta.Estado != EstadoVenta.Presupuesto)
                    throw new Exception("Solo se pueden editar ventas en estado Presupuesto");

                // Calcular totales
                CalcularTotales(viewModel);

                // Actualizar venta
                venta.ClienteId = viewModel.ClienteId;
                venta.FechaVenta = viewModel.FechaVenta;
                venta.TipoPago = viewModel.TipoPago;
                venta.Subtotal = viewModel.Subtotal;
                venta.Descuento = viewModel.Descuento;
                venta.IVA = viewModel.IVA;
                venta.Total = viewModel.Total;
                venta.VendedorNombre = viewModel.VendedorNombre;
                venta.Observaciones = viewModel.Observaciones;
                venta.CreditoId = viewModel.CreditoId;

                // Eliminar detalles que ya no están
                var detallesAEliminar = venta.Detalles
                    .Where(d => !viewModel.Detalles.Any(nd => nd.Id == d.Id))
                    .ToList();

                foreach (var detalle in detallesAEliminar)
                {
                    _context.VentaDetalles.Remove(detalle);
                }

                // Actualizar o agregar detalles
                foreach (var detalleVM in viewModel.Detalles)
                {
                    var detalleExistente = venta.Detalles.FirstOrDefault(d => d.Id == detalleVM.Id);
                    if (detalleExistente != null)
                    {
                        detalleExistente.ProductoId = detalleVM.ProductoId;
                        detalleExistente.Cantidad = detalleVM.Cantidad;
                        detalleExistente.PrecioUnitario = detalleVM.PrecioUnitario;
                        detalleExistente.Descuento = detalleVM.Descuento;
                        detalleExistente.Subtotal = detalleVM.Subtotal;
                        detalleExistente.Observaciones = detalleVM.Observaciones;
                    }
                    else
                    {
                        var nuevoDetalle = new VentaDetalle
                        {
                            VentaId = venta.Id,
                            ProductoId = detalleVM.ProductoId,
                            Cantidad = detalleVM.Cantidad,
                            PrecioUnitario = detalleVM.PrecioUnitario,
                            Descuento = detalleVM.Descuento,
                            Subtotal = detalleVM.Subtotal,
                            Observaciones = detalleVM.Observaciones
                        };
                        venta.Detalles.Add(nuevoDetalle);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar venta: {Id}", viewModel.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Detalles)
                    .Include(v => v.Facturas)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (venta == null)
                    return false;

                // Solo se puede eliminar si está en Presupuesto
                if (venta.Estado != EstadoVenta.Presupuesto)
                    throw new Exception("Solo se pueden eliminar ventas en estado Presupuesto");

                if (venta.Facturas.Any())
                    throw new Exception("No se puede eliminar una venta con facturas asociadas");

                _context.Ventas.Remove(venta);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar venta: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ConfirmarVentaAsync(int ventaId)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Detalles)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(v => v.Id == ventaId);

                if (venta == null)
                    return false;

                if (venta.Estado != EstadoVenta.Presupuesto)
                    throw new Exception("Solo se pueden confirmar ventas en estado Presupuesto");

                // Validar stock
                foreach (var detalle in venta.Detalles)
                {
                    if (detalle.Producto.StockActual < detalle.Cantidad)
                        throw new Exception($"Stock insuficiente para {detalle.Producto.Nombre}. Disponible: {detalle.Producto.StockActual}");
                }

                // Descontar stock
                foreach (var detalle in venta.Detalles)
                {
                    var stockAnterior = detalle.Producto.StockActual;
                    detalle.Producto.StockActual -= detalle.Cantidad;

                    // Registrar movimiento de stock
                    var movimiento = new MovimientoStock
                    {
                        ProductoId = detalle.ProductoId,
                        Tipo = TipoMovimiento.Salida,
                        Cantidad = detalle.Cantidad,
                        StockAnterior = stockAnterior,
                        StockNuevo = detalle.Producto.StockActual,
                        Referencia = $"Venta {venta.Numero}",
                        Motivo = "Venta confirmada",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.MovimientosStock.Add(movimiento);
                }

                venta.Estado = EstadoVenta.Confirmada;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Venta {Numero} confirmada exitosamente", venta.Numero);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar venta: {Id}", ventaId);
                throw;
            }
        }

        public async Task<bool> CancelarVentaAsync(int ventaId, string motivo)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Detalles)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(v => v.Id == ventaId);

                if (venta == null)
                    return false;

                if (venta.Estado == EstadoVenta.Cancelada)
                    throw new Exception("La venta ya está cancelada");

                // Si la venta estaba confirmada, devolver stock
                if (venta.Estado == EstadoVenta.Confirmada || venta.Estado == EstadoVenta.Facturada)
                {
                    foreach (var detalle in venta.Detalles)
                    {
                        var stockAnterior = detalle.Producto.StockActual;
                        detalle.Producto.StockActual += detalle.Cantidad;

                        // Registrar movimiento de stock
                        var movimiento = new MovimientoStock
                        {
                            ProductoId = detalle.ProductoId,
                            Tipo = TipoMovimiento.Entrada,
                            Cantidad = detalle.Cantidad,
                            StockAnterior = stockAnterior,
                            StockNuevo = detalle.Producto.StockActual,
                            Referencia = $"Cancelación Venta {venta.Numero}",
                            Motivo = $"Devolución por cancelación: {motivo}",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.MovimientosStock.Add(movimiento);
                    }
                }

                venta.Estado = EstadoVenta.Cancelada;
                venta.FechaCancelacion = DateTime.Now;
                venta.MotivoCancelacion = motivo;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar venta: {Id}", ventaId);
                throw;
            }
        }

        public async Task<bool> FacturarVentaAsync(int ventaId, FacturaViewModel facturaViewModel)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Facturas)
                    .FirstOrDefaultAsync(v => v.Id == ventaId);

                if (venta == null)
                    return false;

                if (venta.Estado != EstadoVenta.Confirmada)
                    throw new Exception("Solo se pueden facturar ventas confirmadas");

                var factura = _mapper.Map<Factura>(facturaViewModel);
                factura.VentaId = ventaId;
                factura.Subtotal = venta.Subtotal;
                factura.IVA = venta.IVA;
                factura.Total = venta.Total;

                _context.Facturas.Add(factura);

                venta.Estado = EstadoVenta.Facturada;
                venta.FechaFacturacion = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al facturar venta: {Id}", ventaId);
                throw;
            }
        }

        public async Task<bool> ValidarStockAsync(int ventaId)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Detalles)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(v => v.Id == ventaId);

                if (venta == null)
                    return false;

                foreach (var detalle in venta.Detalles)
                {
                    if (detalle.Producto.StockActual < detalle.Cantidad)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar stock de venta: {Id}", ventaId);
                throw;
            }
        }

        public async Task<decimal> CalcularTotalVentaAsync(int ventaId)
        {
            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Detalles)
                    .FirstOrDefaultAsync(v => v.Id == ventaId);

                return venta?.Total ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular total de venta: {Id}", ventaId);
                throw;
            }
        }

        public async Task<bool> NumeroVentaExisteAsync(string numero, int? excludeId = null)
        {
            return await _context.Ventas
                .AnyAsync(v => v.Numero == numero && (excludeId == null || v.Id != excludeId.Value));
        }

        #region Métodos Privados

        private async Task<string> GenerarNumeroVentaAsync()
        {
            var ultimaVenta = await _context.Ventas
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();

            var numero = ultimaVenta != null ? ultimaVenta.Id + 1 : 1;
            return $"VTA-{DateTime.Now:yyyyMM}-{numero:D6}";
        }

        private void CalcularTotales(VentaViewModel viewModel)
        {
            foreach (var detalle in viewModel.Detalles)
            {
                detalle.Subtotal = (detalle.Cantidad * detalle.PrecioUnitario) - detalle.Descuento;
            }

            viewModel.Subtotal = viewModel.Detalles.Sum(d => d.Subtotal);
            var subtotalConDescuento = viewModel.Subtotal - viewModel.Descuento;
            viewModel.IVA = subtotalConDescuento * 0.21m;
            viewModel.Total = subtotalConDescuento + viewModel.IVA;
        }

        #endregion
    }
}