using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.Services.Validators;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    public class VentaService : IVentaService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<VentaService> _logger;
        private readonly IConfiguracionPagoService _configuracionPagoService;
        private readonly IAlertaStockService _alertaStockService;
        private readonly IFinancialCalculationService _financialService;
        private readonly IVentaValidator _validator;
        private readonly VentaNumberGenerator _numberGenerator;

        public VentaService(
            AppDbContext context,
            IMapper mapper,
            ILogger<VentaService> logger,
            IConfiguracionPagoService configuracionPagoService,
            IAlertaStockService alertaStockService,
            IFinancialCalculationService financialService,
            IVentaValidator validator,
            VentaNumberGenerator numberGenerator)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _configuracionPagoService = configuracionPagoService;
            _alertaStockService = alertaStockService;
            _financialService = financialService;
            _validator = validator;
            _numberGenerator = numberGenerator;
        }

        #region Consultas

        public async Task<List<VentaViewModel>> GetAllAsync(VentaFilterViewModel? filter = null)
        {
            var query = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Credito)
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.DatosTarjeta)
                .Include(v => v.DatosCheque)
                .Where(v => !v.IsDeleted)
                .AsQueryable();

            query = AplicarFiltros(query, filter);

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
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Facturas)
                .Include(v => v.DatosTarjeta).ThenInclude(dt => dt!.ConfiguracionTarjeta)
                .Include(v => v.DatosCheque)
                .Include(v => v.VentaCreditoCuotas.OrderBy(c => c.NumeroCuota))
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            if (venta == null)
                return null;

            var viewModel = _mapper.Map<VentaViewModel>(venta);

            if (venta.TipoPago == TipoPago.CreditoPersonal &&
                venta.CreditoId.HasValue &&
                venta.VentaCreditoCuotas.Any())
            {
                viewModel.DatosCreditoPersonal = await ObtenerDatosCreditoVentaAsync(id);
            }

            return viewModel;
        }

        #endregion

        #region Crear y Actualizar

        public async Task<VentaViewModel> CreateAsync(VentaViewModel viewModel)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var venta = _mapper.Map<Venta>(viewModel);

                venta.Numero = await _numberGenerator.GenerarNumeroAsync(viewModel.Estado);

                AgregarDetalles(venta, viewModel.Detalles);

                CalcularTotales(venta);

                await VerificarAutorizacionSiCorrespondeAsync(venta, viewModel);

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                await GuardarDatosAdicionales(venta.Id, viewModel);

                await transaction.CommitAsync();

                _logger.LogInformation("Venta {Numero} creada exitosamente", venta.Numero);
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

            _validator.ValidarEstadoParaEdicion(venta);

            ActualizarDatosVenta(venta, viewModel);
            ActualizarDetalles(venta, viewModel.Detalles);
            CalcularTotales(venta);

            await VerificarAutorizacionSiCorrespondeAsync(venta, viewModel);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Venta {Id} actualizada exitosamente", id);
            return _mapper.Map<VentaViewModel>(venta);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var venta = await _context.Ventas.FindAsync(id);
            if (venta == null)
                return false;

            _validator.ValidarEstadoParaEliminacion(venta);

            venta.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Venta {Id} eliminada", id);
            return true;
        }

        #endregion

        #region Flujo de Venta

        public async Task<bool> ConfirmarVentaAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var venta = await CargarVentaCompleta(id);
                if (venta == null)
                    return false;

                _validator.ValidarEstadoParaConfirmacion(venta);
                _validator.ValidarAutorizacion(venta);
                _validator.ValidarStock(venta);

                await DescontarStockYRegistrarMovimientos(venta);

                if (venta.TipoPago == TipoPago.CreditoPersonal && venta.CreditoId.HasValue)
                {
                    await ProcesarCreditoPersonalVentaAsync(venta);
                }

                await GenerarAlertasStockBajo(venta);

                venta.Estado = EstadoVenta.Confirmada;
                venta.FechaConfirmacion = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Venta {Id} confirmada exitosamente", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al confirmar venta {Id}", id);
                throw;
            }
        }

        public async Task AsociarCreditoAVentaAsync(int ventaId, int creditoId)
        {
            var venta = await _context.Ventas.FindAsync(ventaId);
            if (venta == null)
                throw new InvalidOperationException(VentaConstants.ErrorMessages.VENTA_NO_ENCONTRADA);

            venta.CreditoId = creditoId;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CancelarVentaAsync(int id, string motivo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var venta = await CargarVentaCompleta(id);
                if (venta == null)
                    return false;

                _validator.ValidarNoEstaCancelada(venta);

                if (venta.Estado == EstadoVenta.Confirmada || venta.Estado == EstadoVenta.Facturada)
                {
                    await DevolverStock(venta, motivo);
                }

                if (venta.TipoPago == TipoPago.CreditoPersonal)
                {
                    await RestaurarCreditoPersonal(venta);
                }

                venta.Estado = EstadoVenta.Cancelada;
                venta.FechaCancelacion = DateTime.Now;
                venta.MotivoCancelacion = motivo;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Venta {Id} cancelada. Motivo: {Motivo}", id, motivo);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al cancelar venta {Id}", id);
                throw;
            }
        }

        public async Task<bool> FacturarVentaAsync(int id, FacturaViewModel facturaViewModel)
        {
            var venta = await _context.Ventas
                .Include(v => v.Facturas)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            if (venta == null)
                return false;

            _validator.ValidarEstadoParaFacturacion(venta);
            _validator.ValidarAutorizacion(venta);

            var factura = _mapper.Map<Factura>(facturaViewModel);
            factura.VentaId = venta.Id;
            factura.Numero = await _numberGenerator.GenerarNumeroFacturaAsync(factura.Tipo);

            _context.Facturas.Add(factura);

            venta.Estado = EstadoVenta.Facturada;
            venta.FechaFacturacion = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Venta {Id} facturada con factura {NumeroFactura}", id, factura.Numero);
            return true;
        }

        public async Task<bool> ValidarStockAsync(int ventaId)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == ventaId && !v.IsDeleted);

            if (venta == null)
                return false;

            try
            {
                _validator.ValidarStock(venta);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region AutorizaciÃ³n

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

            await _context.SaveChangesAsync();

            _logger.LogInformation("Solicitud de autorizaciÃ³n creada para venta {Id} por {Usuario}", id, usuarioSolicita);
            return true;
        }

        public async Task<bool> AutorizarVentaAsync(int id, string usuarioAutoriza, string motivo)
        {
            var venta = await _context.Ventas.FindAsync(id);
            if (venta == null)
                return false;

            _validator.ValidarEstadoAutorizacion(venta, EstadoAutorizacionVenta.PendienteAutorizacion);

            venta.EstadoAutorizacion = EstadoAutorizacionVenta.Autorizada;
            venta.UsuarioAutoriza = usuarioAutoriza;
            venta.FechaAutorizacion = DateTime.Now;
            venta.MotivoAutorizacion = motivo;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Venta {Id} autorizada por {Usuario}", id, usuarioAutoriza);
            return true;
        }

        public async Task<bool> RechazarVentaAsync(int id, string usuarioAutoriza, string motivo)
        {
            var venta = await _context.Ventas.FindAsync(id);
            if (venta == null)
                return false;

            _validator.ValidarEstadoAutorizacion(venta, EstadoAutorizacionVenta.PendienteAutorizacion);

            venta.EstadoAutorizacion = EstadoAutorizacionVenta.Rechazada;
            venta.UsuarioAutoriza = usuarioAutoriza;
            venta.FechaAutorizacion = DateTime.Now;
            venta.MotivoRechazo = motivo;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Venta {Id} rechazada por {Usuario}. Motivo: {Motivo}", id, usuarioAutoriza, motivo);
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

        #endregion

        #region MÃ©todos de CÃ¡lculo - Tarjetas

        public async Task<DatosTarjetaViewModel> CalcularCuotasTarjetaAsync(int tarjetaId, decimal monto, int cuotas)
        {
            var configuracion = await _context.ConfiguracionesTarjeta.FindAsync(tarjetaId);
            if (configuracion == null)
                throw new InvalidOperationException("ConfiguraciÃ³n de tarjeta no encontrada");

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
            else if (configuracion.TipoCuota == TipoCuotaTarjeta.ConInteres &&
                     configuracion.TasaInteresesMensual.HasValue)
            {
                var tasaDecimal = configuracion.TasaInteresesMensual.Value / 100;
                resultado.TasaInteres = configuracion.TasaInteresesMensual.Value;

                resultado.MontoCuota = _financialService.CalcularCuotaSistemaFrances(
                    monto, tasaDecimal, cuotas);
                resultado.MontoTotalConInteres = resultado.MontoCuota.Value * cuotas;
            }

            return resultado;
        }

        public async Task<bool> GuardarDatosTarjetaAsync(int ventaId, DatosTarjetaViewModel datosTarjeta)
        {
            var venta = await _context.Ventas.FindAsync(ventaId);
            if (venta == null)
                return false;

            var datosTarjetaEntity = _mapper.Map<DatosTarjeta>(datosTarjeta);
            datosTarjetaEntity.VentaId = ventaId;

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

            if (datosTarjeta.TipoTarjeta == TipoTarjeta.Debito && datosTarjeta.RecargoAplicado.HasValue)
            {
                venta.Total += datosTarjeta.RecargoAplicado.Value;
            }

            _context.DatosTarjeta.Add(datosTarjetaEntity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region MÃ©todos de CÃ¡lculo - CrÃ©dito Personal

        public async Task<DatosCreditoPersonalViewModel> CalcularCreditoPersonalAsync(
            int creditoId,
            decimal montoAFinanciar,
            int cuotas,
            DateTime fechaPrimeraCuota)
        {
            var credito = await _context.Creditos
                .Include(c => c.Cuotas)
                .FirstOrDefaultAsync(c => c.Id == creditoId && !c.IsDeleted);

            if (credito == null)
                throw new InvalidOperationException(VentaConstants.ErrorMessages.CREDITO_NO_ENCONTRADO);

            if (credito.Estado != EstadoCredito.Activo && credito.Estado != EstadoCredito.Aprobado)
                throw new InvalidOperationException("El crÃ©dito debe estar en estado Activo o Aprobado");

            var creditoDisponible = credito.SaldoPendiente;

            if (montoAFinanciar > creditoDisponible)
                throw new InvalidOperationException(
                    string.Format(VentaConstants.ErrorMessages.CREDITO_INSUFICIENTE,
                        montoAFinanciar, creditoDisponible));

            var tasaDecimal = credito.TasaInteres / 100;

            var montoCuota = _financialService.CalcularCuotaSistemaFrances(
                montoAFinanciar, tasaDecimal, cuotas);
            var totalAPagar = _financialService.CalcularTotalConInteres(
                montoAFinanciar, tasaDecimal, cuotas);

            return GenerarDatosCreditoPersonal(
                credito, montoAFinanciar, cuotas, montoCuota,
                totalAPagar, fechaPrimeraCuota);
        }

        public async Task<DatosCreditoPersonalViewModel?> ObtenerDatosCreditoVentaAsync(int ventaId)
        {
            var venta = await _context.Ventas
                .Include(v => v.Credito)
                .Include(v => v.VentaCreditoCuotas.OrderBy(c => c.NumeroCuota))
                .FirstOrDefaultAsync(v => v.Id == ventaId && !v.IsDeleted);

            if (venta == null || venta.CreditoId == null || !venta.VentaCreditoCuotas.Any())
                return null;

            var credito = venta.Credito!;
            var totalCuotas = venta.VentaCreditoCuotas.Sum(c => c.Monto);
            var primeraCuota = venta.VentaCreditoCuotas.OrderBy(c => c.NumeroCuota).First();

            var resultado = new DatosCreditoPersonalViewModel
            {
                CreditoId = credito.Id,
                CreditoNumero = credito.Numero,
                CreditoTotalAsignado = credito.MontoAprobado,
                CreditoDisponible = credito.SaldoPendiente + primeraCuota.Saldo,
                MontoAFinanciar = primeraCuota.Saldo,
                CantidadCuotas = venta.VentaCreditoCuotas.Count,
                MontoCuota = primeraCuota.Monto,
                TasaInteresMensual = credito.TasaInteres,
                TotalAPagar = totalCuotas,
                InteresTotal = totalCuotas - primeraCuota.Saldo,
                SaldoRestante = credito.SaldoPendiente,
                FechaPrimeraCuota = primeraCuota.FechaVencimiento,
                Cuotas = venta.VentaCreditoCuotas.Select(c => new VentaCreditoCuotaViewModel
                {
                    NumeroCuota = c.NumeroCuota,
                    FechaVencimiento = c.FechaVencimiento,
                    Monto = c.Monto,
                    Saldo = c.Saldo,
                    Pagada = c.Pagada,
                    FechaPago = c.FechaPago
                }).ToList()
            };

            return resultado;
        }

        public async Task<bool> ValidarDisponibilidadCreditoAsync(int creditoId, decimal monto)
        {
            var credito = await _context.Creditos
                .FirstOrDefaultAsync(c => c.Id == creditoId && !c.IsDeleted);

            if (credito == null || credito.Estado != EstadoCredito.Activo)
                return false;

            return credito.SaldoPendiente >= monto;
        }

        #endregion

        #region MÃ©todos Auxiliares - Cheques

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

        #endregion

        #region MÃ©todos Privados - Helpers

        private IQueryable<Venta> AplicarFiltros(IQueryable<Venta> query, VentaFilterViewModel? filter)
        {
            if (filter == null)
                return query;

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

            return query;
        }

        private async Task<Venta?> CargarVentaCompleta(int id)
        {
            return await _context.Ventas
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Credito)
                .Include(v => v.Cliente)
                .Include(v => v.VentaCreditoCuotas)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
        }

        private void CalcularTotales(Venta venta)
        {
            venta.Subtotal = venta.Detalles.Sum(d => d.Subtotal);
            var subtotalConDescuento = venta.Subtotal - venta.Descuento;
            venta.IVA = subtotalConDescuento * VentaConstants.IVA_RATE;
            venta.Total = subtotalConDescuento + venta.IVA;
        }

        private void ActualizarDatosVenta(Venta venta, VentaViewModel viewModel)
        {
            venta.ClienteId = viewModel.ClienteId;
            venta.FechaVenta = viewModel.FechaVenta;
            venta.TipoPago = viewModel.TipoPago;
            venta.Descuento = viewModel.Descuento;
            venta.VendedorNombre = viewModel.VendedorNombre;
            venta.Observaciones = viewModel.Observaciones;
            venta.CreditoId = viewModel.CreditoId;
        }

        private void ActualizarDetalles(Venta venta, List<VentaDetalleViewModel> detallesVM)
        {
            _context.VentaDetalles.RemoveRange(venta.Detalles);

            foreach (var detalleVM in detallesVM)
            {
                var detalle = _mapper.Map<VentaDetalle>(detalleVM);
                detalle.VentaId = venta.Id;
                venta.Detalles.Add(detalle);
            }
        }

        private void AgregarDetalles(Venta venta, List<VentaDetalleViewModel> detallesVM)
        {
            foreach (var detalleVM in detallesVM)
            {
                var detalle = _mapper.Map<VentaDetalle>(detalleVM);
                detalle.Venta = venta;
                venta.Detalles.Add(detalle);
            }
        }

        private async Task DescontarStockYRegistrarMovimientos(Venta venta)
        {
            foreach (var detalle in venta.Detalles)
            {
                var stockAnterior = detalle.Producto.StockActual;
                detalle.Producto.StockActual -= detalle.Cantidad;

                var movimiento = new MovimientoStock
                {
                    ProductoId = detalle.ProductoId,
                    Tipo = TipoMovimiento.Salida,
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = detalle.Producto.StockActual,
                    Referencia = $"Venta {venta.Numero}",
                    Motivo = $"ConfirmaciÃ³n de venta - Cliente: {venta.Cliente.Nombre}"
                };

                _context.MovimientosStock.Add(movimiento);
            }
        }

        private async Task DevolverStock(Venta venta, string motivo)
        {
            foreach (var detalle in venta.Detalles)
            {
                var stockAnterior = detalle.Producto.StockActual;
                detalle.Producto.StockActual += detalle.Cantidad;

                var movimiento = new MovimientoStock
                {
                    ProductoId = detalle.ProductoId,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = detalle.Producto.StockActual,
                    Referencia = $"CancelaciÃ³n Venta {venta.Numero}",
                    Motivo = motivo
                };

                _context.MovimientosStock.Add(movimiento);
            }
        }

        private async Task RestaurarCreditoPersonal(Venta venta)
        {
            if (!venta.CreditoId.HasValue || !venta.VentaCreditoCuotas.Any())
                return;

            var credito = venta.Credito ?? await _context.Creditos.FindAsync(venta.CreditoId);
            if (credito == null)
                return;

            var montoFinanciado = venta.VentaCreditoCuotas.First().Saldo;
            credito.SaldoPendiente += montoFinanciado;
            _context.Creditos.Update(credito);

            _context.VentaCreditoCuotas.RemoveRange(venta.VentaCreditoCuotas);

            _logger.LogInformation(
                "CrÃ©dito {CreditoId} restaurado por cancelaciÃ³n de venta {VentaId}. Monto: ${Monto}",
                credito.Id, venta.Id, montoFinanciado);
        }

        private async Task GenerarAlertasStockBajo(Venta venta)
        {
            foreach (var detalle in venta.Detalles)
            {
                await _alertaStockService.VerificarYGenerarAlertaAsync(detalle.ProductoId);
            }
        }

        private async Task VerificarAutorizacionSiCorrespondeAsync(Venta venta, VentaViewModel viewModel)
        {
            if (viewModel.TipoPago == TipoPago.CreditoPersonal)
            {
                var cliente = await _context.Clientes
                    .Include(c => c.Creditos)
                    .FirstOrDefaultAsync(c => c.Id == viewModel.ClienteId);

                if (cliente != null)
                {
                    venta.RequiereAutorizacion = await ValidarLimiteCreditoClienteAsync(cliente, venta.Total);

                    if (venta.RequiereAutorizacion &&
                        venta.EstadoAutorizacion == EstadoAutorizacionVenta.NoRequiere)
                    {
                        venta.EstadoAutorizacion = EstadoAutorizacionVenta.PendienteAutorizacion;
                        venta.FechaSolicitudAutorizacion = DateTime.Now;
                    }
                }
            }
        }

        private async Task GuardarDatosAdicionales(int ventaId, VentaViewModel viewModel)
        {
            if (viewModel.DatosTarjeta != null &&
                (viewModel.TipoPago == TipoPago.TarjetaCredito || viewModel.TipoPago == TipoPago.TarjetaDebito))
            {
                await GuardarDatosTarjetaAsync(ventaId, viewModel.DatosTarjeta);
            }

            if (viewModel.DatosCheque != null && viewModel.TipoPago == TipoPago.Cheque)
            {
                await GuardarDatosChequeAsync(ventaId, viewModel.DatosCheque);
            }

            if (viewModel.DatosCreditoPersonal != null && viewModel.TipoPago == TipoPago.CreditoPersonal)
            {
                await GuardarCuotasCreditoPersonalAsync(ventaId, viewModel.DatosCreditoPersonal);
            }
        }

        private async Task GuardarCuotasCreditoPersonalAsync(int ventaId, DatosCreditoPersonalViewModel datos)
        {
            var venta = await _context.Ventas.FindAsync(ventaId);
            if (venta == null)
                throw new InvalidOperationException(VentaConstants.ErrorMessages.VENTA_NO_ENCONTRADA);

            var credito = await _context.Creditos.FindAsync(datos.CreditoId);
            if (credito == null)
                throw new InvalidOperationException(VentaConstants.ErrorMessages.CREDITO_NO_ENCONTRADO);

            if (credito.SaldoPendiente < datos.MontoAFinanciar)
                throw new InvalidOperationException("Saldo de crÃ©dito insuficiente");

            foreach (var cuotaVM in datos.Cuotas)
            {
                var cuota = new VentaCreditoCuota
                {
                    VentaId = ventaId,
                    CreditoId = datos.CreditoId,
                    NumeroCuota = cuotaVM.NumeroCuota,
                    FechaVencimiento = cuotaVM.FechaVencimiento,
                    Monto = cuotaVM.Monto,
                    Saldo = cuotaVM.Saldo,
                    Pagada = false
                };

                _context.VentaCreditoCuotas.Add(cuota);
            }

            await _context.SaveChangesAsync();
        }

        private async Task ProcesarCreditoPersonalVentaAsync(Venta venta)
        {
            if (venta.Credito == null)
            {
                venta.Credito = await _context.Creditos.FindAsync(venta.CreditoId);
            }

            if (venta.Credito == null)
                throw new InvalidOperationException(VentaConstants.ErrorMessages.CREDITO_NO_ENCONTRADO);

            var datosCredito = venta.VentaCreditoCuotas.Any()
                ? await ObtenerDatosCreditoVentaAsync(venta.Id)
                : null;

            if (datosCredito == null || !datosCredito.Cuotas.Any())
                throw new InvalidOperationException("No se encontraron datos de crÃ©dito personal para esta venta");

            if (datosCredito.MontoAFinanciar > venta.Credito.SaldoPendiente)
                throw new InvalidOperationException(
                    $"Saldo insuficiente en el crÃ©dito. Disponible: ${venta.Credito.SaldoPendiente:N2}");

            venta.Credito.SaldoPendiente -= datosCredito.MontoAFinanciar;
            _context.Creditos.Update(venta.Credito);
        }

        private Task<bool> ValidarLimiteCreditoClienteAsync(Cliente cliente, decimal montoVenta)
        {
            var creditosActivos = cliente.Creditos
                .Where(c => c.Estado == EstadoCredito.Activo)
                .ToList();

            if (!creditosActivos.Any())
                return Task.FromResult(true);

            var saldoDisponible = creditosActivos.Sum(c => c.SaldoPendiente);

            return Task.FromResult(montoVenta > saldoDisponible);
        }

        private DatosCreditoPersonalViewModel GenerarDatosCreditoPersonal(
            Credito credito,
            decimal montoAFinanciar,
            int cuotas,
            decimal montoCuota,
            decimal totalAPagar,
            DateTime fechaPrimeraCuota)
        {
            var resultado = new DatosCreditoPersonalViewModel
            {
                CreditoId = credito.Id,
                CreditoNumero = credito.Numero,
                CreditoTotalAsignado = credito.MontoAprobado,
                CreditoDisponible = credito.SaldoPendiente,
                MontoAFinanciar = montoAFinanciar,
                CantidadCuotas = cuotas,
                MontoCuota = montoCuota,
                FechaPrimeraCuota = fechaPrimeraCuota,
                TasaInteresMensual = credito.TasaInteres,
                TotalAPagar = totalAPagar,
                InteresTotal = totalAPagar - montoAFinanciar,
                SaldoRestante = credito.SaldoPendiente - montoAFinanciar,
                Cuotas = new List<VentaCreditoCuotaViewModel>()
            };

            decimal saldoRestante = totalAPagar;
            DateTime fechaVencimiento = fechaPrimeraCuota;

            for (int i = 1; i <= cuotas; i++)
            {
                resultado.Cuotas.Add(new VentaCreditoCuotaViewModel
                {
                    NumeroCuota = i,
                    FechaVencimiento = fechaVencimiento,
                    Monto = montoCuota,
                    Saldo = saldoRestante,
                    Pagada = false
                });

                saldoRestante -= montoCuota;
                fechaVencimiento = fechaVencimiento.AddMonths(1);
            }

            return resultado;
        }

        #endregion
    }
}