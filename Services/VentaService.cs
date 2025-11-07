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
        private readonly IConfiguracionPagoService _configuracionPagoService;

        public VentaService(
            AppDbContext context,
            IMapper mapper,
            ILogger<VentaService> logger,
            IConfiguracionPagoService configuracionPagoService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _configuracionPagoService = configuracionPagoService;
        }

        public async Task<List<VentaViewModel>> GetAllAsync(VentaFilterViewModel? filter = null)
        {
            var query = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Credito)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.Facturas)
                .Include(v => v.DatosTarjeta)
                .Include(v => v.DatosCheque)
                .Where(v => !v.IsDeleted)
                .AsQueryable();

            if (filter != null)
            {
                if (filter.ClienteId.HasValue)
                    query = query.Where(v => v.ClienteId == filter.ClienteId.Value);

                if (!string.IsNullOrEmpty(filter.Numero))
                    query = query.Where(v => v.Numero.Contains(filter.Numero));

                if (filter.FechaDesde.HasValue)
                    query = query.Where(v => v.FechaVenta >= filter.FechaDesde.Value);

                if (filter.FechaHasta.HasValue)
                    query = query.Where(v => v.FechaVenta <= filter.FechaHasta.Value);

                if (filter.Estado.HasValue)
                    query = query.Where(v => v.Estado == filter.Estado.Value);

                if (filter.TipoPago.HasValue)
                    query = query.Where(v => v.TipoPago == filter.TipoPago.Value);

                if (filter.EstadoAutorizacion.HasValue)
                    query = query.Where(v => v.EstadoAutorizacion == filter.EstadoAutorizacion.Value);
            }

            var ventas = await query
                .OrderByDescending(v => v.FechaVenta)
                .ThenByDescending(v => v.Id)
                .ToListAsync();

            return _mapper.Map<List<VentaViewModel>>(ventas);
        }

        public async Task<VentaViewModel?> GetByIdAsync(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Credito)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.Facturas)
                .Include(v => v.DatosTarjeta)
                    .ThenInclude(dt => dt!.ConfiguracionTarjeta)
                .Include(v => v.DatosCheque)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            return venta == null ? null : _mapper.Map<VentaViewModel>(venta);
        }

        public async Task<VentaViewModel> CreateAsync(VentaViewModel viewModel)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var venta = _mapper.Map<Venta>(viewModel);

                // Generar número de venta
                venta.Numero = await GenerarNumeroVentaAsync(viewModel.Estado);

                // Calcular totales
                CalcularTotales(venta);

                // Verificar si requiere autorización
                var cliente = await _context.Clientes
                    .Include(c => c.Creditos)
                    .FirstOrDefaultAsync(c => c.Id == viewModel.ClienteId);

                if (cliente != null && viewModel.TipoPago == TipoPago.CreditoPersonal)
                {
                    venta.RequiereAutorizacion = await ValidarLimiteCreditoClienteAsync(cliente, venta.Total);

                    if (venta.RequiereAutorizacion)
                    {
                        venta.EstadoAutorizacion = EstadoAutorizacionVenta.PendienteAutorizacion;
                        venta.FechaSolicitudAutorizacion = DateTime.Now;
                    }
                }

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                // Guardar datos adicionales según tipo de pago
                if (viewModel.DatosTarjeta != null &&
                    (viewModel.TipoPago == TipoPago.TarjetaCredito || viewModel.TipoPago == TipoPago.TarjetaDebito))
                {
                    await GuardarDatosTarjetaAsync(venta.Id, viewModel.DatosTarjeta);
                }

                if (viewModel.DatosCheque != null && viewModel.TipoPago == TipoPago.Cheque)
                {
                    await GuardarDatosChequeAsync(venta.Id, viewModel.DatosCheque);
                }

                await transaction.CommitAsync();

                return _mapper.Map<VentaViewModel>(venta);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al crear venta");
                throw;
            }
        }


        public async Task<VentaViewModel?> UpdateAsync(int id, VentaViewModel viewModel)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            if (venta == null)
                return null;

            // Solo se pueden editar ventas en estado Cotización o Presupuesto
            if (venta.Estado != EstadoVenta.Cotizacion && venta.Estado != EstadoVenta.Presupuesto)
            {
                throw new InvalidOperationException("Solo se pueden editar ventas en estado Cotización o Presupuesto");
            }

            venta.ClienteId = viewModel.ClienteId;
            venta.FechaVenta = viewModel.FechaVenta;
            venta.TipoPago = viewModel.TipoPago;
            venta.Descuento = viewModel.Descuento;
            venta.VendedorNombre = viewModel.VendedorNombre;
            venta.Observaciones = viewModel.Observaciones;
            venta.CreditoId = viewModel.CreditoId;
            venta.UpdatedAt = DateTime.Now;

            // Actualizar detalles
            _context.VentaDetalles.RemoveRange(venta.Detalles);

            foreach (var detalleVM in viewModel.Detalles)
            {
                var detalle = _mapper.Map<VentaDetalle>(detalleVM);
                detalle.VentaId = venta.Id;
                venta.Detalles.Add(detalle);
            }

            CalcularTotales(venta);

            // Verificar si ahora requiere autorización
            if (viewModel.TipoPago == TipoPago.CreditoPersonal)
            {
                var cliente = await _context.Clientes
                    .Include(c => c.Creditos)
                    .FirstOrDefaultAsync(c => c.Id == viewModel.ClienteId);

                if (cliente != null)
                {
                    venta.RequiereAutorizacion = await ValidarLimiteCreditoClienteAsync(cliente, venta.Total);

                    if (venta.RequiereAutorizacion && venta.EstadoAutorizacion == EstadoAutorizacionVenta.NoRequiere)
                    {
                        venta.EstadoAutorizacion = EstadoAutorizacionVenta.PendienteAutorizacion;
                        venta.FechaSolicitudAutorizacion = DateTime.Now;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return _mapper.Map<VentaViewModel>(venta);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var venta = await _context.Ventas.FindAsync(id);

            if (venta == null)
                return false;

            // Solo se pueden eliminar ventas en estado Cotización o Presupuesto
            if (venta.Estado != EstadoVenta.Cotizacion && venta.Estado != EstadoVenta.Presupuesto)
            {
                throw new InvalidOperationException("Solo se pueden eliminar ventas en estado Cotización o Presupuesto");
            }

            venta.IsDeleted = true;
            venta.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ConfirmarVentaAsync(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            if (venta == null)
                return false;

            if (venta.Estado != EstadoVenta.Presupuesto)
            {
                throw new InvalidOperationException("Solo se pueden confirmar ventas en estado Presupuesto");
            }

            // Si requiere autorización, validar que esté autorizada
            if (venta.RequiereAutorizacion && venta.EstadoAutorizacion != EstadoAutorizacionVenta.Autorizada)
            {
                throw new InvalidOperationException("La venta requiere autorización antes de ser confirmada");
            }

            // Validar stock disponible
            foreach (var detalle in venta.Detalles)
            {
                if (detalle.Producto.StockActual < detalle.Cantidad)
                {
                    throw new InvalidOperationException($"Stock insuficiente para el producto {detalle.Producto.Nombre}. Disponible: {detalle.Producto.StockActual}");
                }
            }

            // Descontar stock y registrar movimientos
            foreach (var detalle in venta.Detalles)
            {
                detalle.Producto.StockActual -= detalle.Cantidad;

                var movimiento = new MovimientoStock
                {
                    ProductoId = detalle.ProductoId,
                    Tipo = TipoMovimientoStock.Venta,
                    Cantidad = -detalle.Cantidad,
                    StockAnterior = detalle.Producto.StockActual + detalle.Cantidad,
                    StockNuevo = detalle.Producto.StockActual,
                    Referencia = $"Venta {venta.Numero}",
                    Observaciones = $"Venta confirmada - Cliente: {venta.Cliente?.Apellido}, {venta.Cliente?.Nombre}"
                };

                _context.MovimientosStock.Add(movimiento);
            }

            venta.Estado = EstadoVenta.Confirmada;
            venta.FechaConfirmacion = DateTime.Now;
            venta.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CancelarVentaAsync(int id, string motivo)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            if (venta == null)
                return false;

            if (venta.Estado == EstadoVenta.Cancelada)
            {
                throw new InvalidOperationException("La venta ya está cancelada");
            }

            // Si la venta estaba confirmada, devolver stock
            if (venta.Estado == EstadoVenta.Confirmada || venta.Estado == EstadoVenta.Facturada)
            {
                foreach (var detalle in venta.Detalles)
                {
                    detalle.Producto.StockActual += detalle.Cantidad;

                    var movimiento = new MovimientoStock
                    {
                        ProductoId = detalle.ProductoId,
                        Tipo = TipoMovimientoStock.Devolucion,
                        Cantidad = detalle.Cantidad,
                        StockAnterior = detalle.Producto.StockActual - detalle.Cantidad,
                        StockNuevo = detalle.Producto.StockActual,
                        Referencia = $"Cancelación Venta {venta.Numero}",
                        Observaciones = motivo
                    };

                    _context.MovimientosStock.Add(movimiento);
                }
            }

            venta.Estado = EstadoVenta.Cancelada;
            venta.FechaCancelacion = DateTime.Now;
            venta.MotivoCancelacion = motivo;
            venta.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> FacturarVentaAsync(int id, FacturaViewModel facturaViewModel)
        {
            var venta = await _context.Ventas
                .Include(v => v.Facturas)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            if (venta == null)
                return false;

            if (venta.Estado != EstadoVenta.Confirmada)
            {
                throw new InvalidOperationException("Solo se pueden facturar ventas confirmadas");
            }

            // Si requiere autorización, validar que esté autorizada
            if (venta.RequiereAutorizacion && venta.EstadoAutorizacion != EstadoAutorizacionVenta.Autorizada)
            {
                throw new InvalidOperationException("La venta requiere autorización antes de ser facturada");
            }

            var factura = _mapper.Map<Factura>(facturaViewModel);
            factura.VentaId = venta.Id;
            factura.Numero = await GenerarNumeroFacturaAsync(factura.Tipo);

            _context.Facturas.Add(factura);

            venta.Estado = EstadoVenta.Facturada;
            venta.FechaFacturacion = DateTime.Now;
            venta.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ValidarStockAsync(int ventaId)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == ventaId && !v.IsDeleted);

            if (venta == null)
                return false;

            foreach (var detalle in venta.Detalles)
            {
                if (detalle.Producto.StockActual < detalle.Cantidad)
                    return false;
            }

            return true;
        }
        public async Task<bool> SolicitarAutorizacionAsync(int id, string usuarioSolicita, string motivo)
        {
            var venta = await _context.Ventas.FindAsync(id);

            if (venta == null)
                return false;

            venta.RequiereAutorizacion = true;
            venta.EstadoAutorizacion = EstadoAutorizacionVenta.PendienteAutorizacion;
            venta.UsuarioSolicita = usuarioSolicita;
            venta.FechaSolicitudAutorizacion = DateTime.Now;
            venta.MotivoAutorizacion = motivo;
            venta.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AutorizarVentaAsync(int id, string usuarioAutoriza, string motivo)
        {
            var venta = await _context.Ventas.FindAsync(id);

            if (venta == null)
                return false;

            if (venta.EstadoAutorizacion != EstadoAutorizacionVenta.PendienteAutorizacion)
            {
                throw new InvalidOperationException("La venta no está pendiente de autorización");
            }

            venta.EstadoAutorizacion = EstadoAutorizacionVenta.Autorizada;
            venta.UsuarioAutoriza = usuarioAutoriza;
            venta.FechaAutorizacion = DateTime.Now;
            venta.MotivoAutorizacion = motivo;
            venta.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RechazarVentaAsync(int id, string usuarioAutoriza, string motivo)
        {
            var venta = await _context.Ventas.FindAsync(id);

            if (venta == null)
                return false;

            if (venta.EstadoAutorizacion != EstadoAutorizacionVenta.PendienteAutorizacion)
            {
                throw new InvalidOperationException("La venta no está pendiente de autorización");
            }

            venta.EstadoAutorizacion = EstadoAutorizacionVenta.Rechazada;
            venta.UsuarioAutoriza = usuarioAutoriza;
            venta.FechaAutorizacion = DateTime.Now;
            venta.MotivoRechazo = motivo;
            venta.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RequiereAutorizacionAsync(VentaViewModel viewModel)
        {
            if (viewModel.TipoPago != TipoPago.CreditoPersonal)
                return false;

            var cliente = await _context.Clientes
                .Include(c => c.Creditos)
                .FirstOrDefaultAsync(c => c.Id == viewModel.ClienteId);

            if (cliente == null)
                return false;

            return await ValidarLimiteCreditoClienteAsync(cliente, viewModel.Total);
        }
        public async Task<bool> GuardarDatosTarjetaAsync(int ventaId, DatosTarjetaViewModel datosTarjeta)
        {
            var venta = await _context.Ventas.FindAsync(ventaId);

            if (venta == null)
                return false;

            var datosTarjetaEntity = _mapper.Map<DatosTarjeta>(datosTarjeta);
            datosTarjetaEntity.VentaId = ventaId;

            // Si es tarjeta de crédito con cuotas, calcular datos
            if (datosTarjeta.TipoTarjeta == TipoTarjeta.Credito &&
                datosTarjeta.CantidadCuotas.HasValue &&
                datosTarjeta.ConfiguracionTarjetaId.HasValue)
            {
                var calculado = await CalcularCuotasTarjetaAsync(
                    datosTarjeta.ConfiguracionTarjetaId.Value,
                    venta.Total,
                    datosTarjeta.CantidadCuotas.Value
                );

                datosTarjetaEntity.TasaInteres = calculado.TasaInteres;
                datosTarjetaEntity.MontoCuota = calculado.MontoCuota;
                datosTarjetaEntity.MontoTotalConInteres = calculado.MontoTotalConInteres;
            }

            // Si es débito con recargo
            if (datosTarjeta.TipoTarjeta == TipoTarjeta.Debito && datosTarjeta.RecargoAplicado.HasValue)
            {
                venta.Total += datosTarjeta.RecargoAplicado.Value;
            }

            _context.DatosTarjeta.Add(datosTarjetaEntity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> GuardarDatosChequeAsync(int ventaId, DatosChequeViewModel datosCheque)
        {
            var venta = await _context.Ventas.FindAsync(ventaId);

            if (venta == null)
                return false;

            var datosChequeEntity = _mapper.Map<DatosCheque>(datosCheque);
            datosChequeEntity.VentaId = ventaId;

            _context.DatosCheque.Add(datosChequeEntity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<DatosTarjetaViewModel> CalcularCuotasTarjetaAsync(int tarjetaId, decimal monto, int cuotas)
        {
            var configuracion = await _context.ConfiguracionesTarjeta.FindAsync(tarjetaId);

            if (configuracion == null)
                throw new InvalidOperationException("Configuración de tarjeta no encontrada");

            var resultado = new DatosTarjetaViewModel
            {
                ConfiguracionTarjetaId = tarjetaId,
                NombreTarjeta = configuracion.NombreTarjeta,
                TipoTarjeta = configuracion.TipoTarjeta,
                CantidadCuotas = cuotas,
                TipoCuota = configuracion.TipoCuota
            };

            if (configuracion.TipoCuota == TipoCuotaTarjeta.SinInteres)
            {
                resultado.TasaInteres = 0;
                resultado.MontoCuota = monto / cuotas;
                resultado.MontoTotalConInteres = monto;
            }
            else if (configuracion.TipoCuota == TipoCuotaTarjeta.ConInteres && configuracion.TasaInteresesMensual.HasValue)
            {
                var tasaDecimal = configuracion.TasaInteresesMensual.Value / 100;
                resultado.TasaInteres = configuracion.TasaInteresesMensual.Value;

                // Fórmula del sistema francés
                var montoCuota = monto * (tasaDecimal * Math.Pow((double)(1 + tasaDecimal), cuotas)) /
                                 (Math.Pow((double)(1 + tasaDecimal), cuotas) - 1);

                resultado.MontoCuota = (decimal)montoCuota;
                resultado.MontoTotalConInteres = resultado.MontoCuota.Value * cuotas;
            }

            return resultado;
        }
        #region Métodos Privados

        private void CalcularTotales(Venta venta)
        {
            venta.Subtotal = venta.Detalles.Sum(d => d.Subtotal);

            var subtotalConDescuento = venta.Subtotal - venta.Descuento;
            venta.IVA = subtotalConDescuento * 0.21m;
            venta.Total = subtotalConDescuento + venta.IVA;
        }

        private async Task<string> GenerarNumeroVentaAsync(EstadoVenta estado)
        {
            var prefijo = estado == EstadoVenta.Cotizacion ? "COT" : "VTA";
            var fecha = DateTime.Now;
            var periodo = fecha.ToString("yyyyMM");

            var ultimaVenta = await _context.Ventas
                .Where(v => v.Numero.StartsWith($"{prefijo}-{periodo}"))
                .OrderByDescending(v => v.Numero)
                .FirstOrDefaultAsync();

            int siguiente = 1;
            if (ultimaVenta != null)
            {
                var partes = ultimaVenta.Numero.Split('-');
                if (partes.Length == 3 && int.TryParse(partes[2], out int ultimo))
                {
                    siguiente = ultimo + 1;
                }
            }

            return $"{prefijo}-{periodo}-{siguiente:D6}";
        }

        private async Task<string> GenerarNumeroFacturaAsync(TipoFactura tipo)
        {
            var prefijo = tipo switch
            {
                TipoFactura.A => "FA-A",
                TipoFactura.B => "FA-B",
                TipoFactura.C => "FA-C",
                TipoFactura.NotaCredito => "NC",
                TipoFactura.NotaDebito => "ND",
                _ => "FA"
            };

            var fecha = DateTime.Now;
            var periodo = fecha.ToString("yyyyMM");

            var ultimaFactura = await _context.Facturas
                .Where(f => f.Numero.StartsWith($"{prefijo}-{periodo}"))
                .OrderByDescending(f => f.Numero)
                .FirstOrDefaultAsync();

            int siguiente = 1;
            if (ultimaFactura != null)
            {
                var partes = ultimaFactura.Numero.Split('-');
                if (partes.Length >= 3 && int.TryParse(partes[2], out int ultimo))
                {
                    siguiente = ultimo + 1;
                }
            }

            return $"{prefijo}-{periodo}-{siguiente:D6}";
        }

        private async Task<bool> ValidarLimiteCreditoClienteAsync(Cliente cliente, decimal montoVenta)
        {
            // Obtener el límite de crédito total del cliente (suma de créditos activos)
            var creditosActivos = cliente.Creditos
                .Where(c => c.Estado == EstadoCredito.Activo)
                .ToList();

            if (!creditosActivos.Any())
                return true; // Si no tiene créditos, requiere autorización

            var limiteTotal = creditosActivos.Sum(c => c.MontoAprobado);
            var saldoDisponible = creditosActivos.Sum(c => c.SaldoPendiente);

            // Si el monto de la venta supera el saldo disponible, requiere autorización
            if (montoVenta > saldoDisponible)
                return true;

            return false;
        }

        #endregion
    }
}