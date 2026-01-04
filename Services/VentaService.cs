using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.Services.Validators;
using TheBuryProject.ViewModels;
using TheBuryProject.ViewModels.Requests;
using TheBuryProject.ViewModels.Responses;

namespace TheBuryProject.Services
{
    public class VentaService : IVentaService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<VentaService> _logger;
        private readonly IPrecioService _precioService;
        private readonly IConfiguracionPagoService _configuracionPagoService;
        private readonly IAlertaStockService _alertaStockService;
        private readonly IMovimientoStockService _movimientoStockService;
        private readonly IFinancialCalculationService _financialService;
        private readonly IVentaValidator _validator;
        private readonly VentaNumberGenerator _numberGenerator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidacionVentaService _validacionVentaService;

        public VentaService(
            AppDbContext context,
            IMapper mapper,
            ILogger<VentaService> logger,
            IConfiguracionPagoService configuracionPagoService,
            IAlertaStockService alertaStockService,
            IMovimientoStockService movimientoStockService,
            IFinancialCalculationService financialService,
            IVentaValidator validator,
            VentaNumberGenerator numberGenerator,
            IPrecioService precioService,
            IHttpContextAccessor httpContextAccessor,
            IValidacionVentaService validacionVentaService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _configuracionPagoService = configuracionPagoService;
            _alertaStockService = alertaStockService;
            _movimientoStockService = movimientoStockService;
            _financialService = financialService;
            _validator = validator;
            _numberGenerator = numberGenerator;
            _precioService = precioService;
            _httpContextAccessor = httpContextAccessor;
            _validacionVentaService = validacionVentaService;
        }

        #region Consultas

        public async Task<List<VentaViewModel>> GetAllAsync(VentaFilterViewModel? filter = null)
        {
            var query = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Credito)
                .Include(v => v.Detalles.Where(d => !d.IsDeleted && d.Producto != null && !d.Producto.IsDeleted)).ThenInclude(d => d.Producto)
                .Include(v => v.DatosTarjeta)
                .Include(v => v.DatosCheque)
                .Where(v =>
                    !v.IsDeleted &&
                    (v.Cliente == null || !v.Cliente.IsDeleted) &&
                    (v.Credito == null || (!v.Credito.IsDeleted && v.Credito.Cliente != null && !v.Credito.Cliente.IsDeleted)))
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
                .Include(v => v.Detalles.Where(d => !d.IsDeleted && d.Producto != null && !d.Producto.IsDeleted)).ThenInclude(d => d.Producto)
                .Include(v => v.Facturas)
                .Include(v => v.DatosTarjeta).ThenInclude(dt => dt!.ConfiguracionTarjeta)
                .Include(v => v.DatosCheque)
                .Include(v => v.VentaCreditoCuotas.OrderBy(c => c.NumeroCuota))
                .FirstOrDefaultAsync(v =>
                    v.Id == id &&
                    !v.IsDeleted &&
                    (v.Cliente == null || !v.Cliente.IsDeleted) &&
                    (v.Credito == null || (!v.Credito.IsDeleted && v.Credito.Cliente != null && !v.Credito.Cliente.IsDeleted)));

            if (venta == null)
                return null;

            var viewModel = _mapper.Map<VentaViewModel>(venta);

            // Mapear estado del crédito para control de flujo en la vista
            if (venta.Credito != null)
            {
                viewModel.CreditoEstado = venta.Credito.Estado;
            }

            if (venta.TipoPago == TipoPago.CreditoPersonall &&
                venta.CreditoId.HasValue &&
                venta.VentaCreditoCuotas.Any())
            {
                viewModel.DatosCreditoPersonall = await ObtenerDatosCreditoVentaAsync(id);
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

                await AplicarPrecioVigenteADetallesAsync(venta);

                CalcularTotales(venta);

                // Validación unificada para crédito personal
                ValidacionVentaResult? validacion = null;
                if (viewModel.TipoPago == TipoPago.CreditoPersonall)
                {
                    validacion = await _validacionVentaService.ValidarVentaCreditoPersonalAsync(
                        viewModel.ClienteId, 
                        venta.Total, 
                        viewModel.CreditoId);

                    // E2: Si NoViable, rechazar guardado completamente
                    if (validacion.NoViable)
                    {
                        throw new InvalidOperationException(
                            $"No es posible crear la venta con crédito personal. {validacion.MensajeResumen}");
                    }

                    await AplicarResultadoValidacionAsync(venta, validacion);
                }
                else
                {
                    await VerificarAutorizacionSiCorrespondeAsync(venta, viewModel);
                }

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                // Para crédito personal, crear el crédito inmediatamente después de guardar la venta
                if (viewModel.TipoPago == TipoPago.CreditoPersonall && 
                    venta.Estado == EstadoVenta.PendienteFinanciacion &&
                    !venta.CreditoId.HasValue)
                {
                    await CrearCreditoPendienteParaVentaAsync(venta);
                }

                // Solo guardar datos adicionales de crédito si no hay requisitos pendientes
                if (venta.Estado != EstadoVenta.PendienteRequisitos && 
                    venta.Estado != EstadoVenta.PendienteFinanciacion)
                {
                    await GuardarDatosAdicionales(venta.Id, viewModel);
                }

                await transaction.CommitAsync();

                var resultado = _mapper.Map<VentaViewModel>(venta);
                resultado.ValidacionCredito = validacion;
                resultado.CreditoId = venta.CreditoId; // Asegurar que el CreditoId se propague

                _logger.LogInformation("Venta {Numero} creada exitosamente. Estado: {Estado}", venta.Numero, venta.Estado);
                return resultado;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al crear venta");
                throw;
            }
        }

        /// <summary>
        /// Aplica el resultado de la validación de crédito a la venta.
        /// NOTA: Si NoViable=true, esta función no debe ser llamada (se rechaza antes).
        /// Para crédito personal aprobable:
        /// - Crea el crédito en estado PendienteConfiguracion
        /// - Pone la venta en estado PendienteFinanciacion
        /// </summary>
        private async Task AplicarResultadoValidacionAsync(Venta venta, ValidacionVentaResult validacion)
        {
            // Seguridad: NoViable nunca debería llegar aquí, pero por si acaso
            if (validacion.NoViable)
            {
                throw new InvalidOperationException(
                    $"Error interno: Se intentó aplicar validación NoViable. {validacion.MensajeResumen}");
            }

            if (validacion.RequiereAutorizacion)
            {
                // E2: Guardar venta en estado PendienteAutorizacion con razones persistidas
                venta.RequiereAutorizacion = true;
                venta.Estado = EstadoVenta.PendienteFinanciacion; // También PendienteFinanciacion
                venta.EstadoAutorizacion = EstadoAutorizacionVenta.PendienteAutorizacion;
                venta.FechaSolicitudAutorizacion = DateTime.Now;
                venta.RazonesAutorizacionJson = System.Text.Json.JsonSerializer.Serialize(
                    validacion.RazonesAutorizacion.Select(r => new { r.Tipo, r.Descripcion, r.DetalleAdicional }));

                _logger.LogInformation(
                    "Venta requiere autorización. Razones: {Razones}",
                    validacion.MensajeResumen);
            }
            else
            {
                // Aprobable - Crear crédito y poner venta en PendienteFinanciacion
                venta.RequiereAutorizacion = false;
                venta.EstadoAutorizacion = EstadoAutorizacionVenta.NoRequiere;
                venta.Estado = EstadoVenta.PendienteFinanciacion;
                
                _logger.LogInformation(
                    "Venta con crédito personal aprobable. Estado: {Estado}. Crédito será creado después de persistir.",
                    venta.Estado);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Crea el crédito para una venta con CreditoPersonal después de que la venta fue guardada.
        /// </summary>
        private async Task CrearCreditoPendienteParaVentaAsync(Venta venta)
        {
            // Generar número de crédito
            var ultimoCredito = await _context.Creditos
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();
            var numeroSecuencial = ultimoCredito != null ? ultimoCredito.Id + 1 : 1;
            var numeroCredito = $"CRE-{DateTime.Now:yyyyMM}-{numeroSecuencial:D6}";

            // Obtener puntaje de riesgo del cliente
            var cliente = await _context.Clientes.FindAsync(venta.ClienteId);
            var puntajeRiesgo = cliente?.PuntajeRiesgo ?? 0;

            var credito = new Credito
            {
                ClienteId = venta.ClienteId,
                Numero = numeroCredito,
                MontoSolicitado = venta.Total,
                MontoAprobado = venta.Total,
                SaldoPendiente = venta.Total,
                TasaInteres = 0, // Se configurará después
                CantidadCuotas = 0, // Se configurará después
                Estado = EstadoCredito.PendienteConfiguracion,
                FechaSolicitud = DateTime.Now,
                PuntajeRiesgoInicial = puntajeRiesgo
            };

            _context.Creditos.Add(credito);
            await _context.SaveChangesAsync();

            // Asociar crédito a la venta
            venta.CreditoId = credito.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Crédito {NumeroCredito} (PendienteConfiguracion) creado y asociado a venta {VentaId}",
                numeroCredito, venta.Id);
        }

        public async Task<VentaViewModel?> UpdateAsync(int id, VentaViewModel viewModel)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            if (venta == null)
                return null;

            _validator.ValidarEstadoParaEdicion(venta);

            if (viewModel.RowVersion == null || viewModel.RowVersion.Length == 0)
                throw new InvalidOperationException("Falta información de concurrencia (RowVersion). Recargá la venta e intentá nuevamente.");

            _context.Entry(venta).Property(v => v.RowVersion).OriginalValue = viewModel.RowVersion;

            ActualizarDatosVenta(venta, viewModel);
            ActualizarDetalles(venta, viewModel.Detalles);

            await AplicarPrecioVigenteADetallesAsync(venta);

            CalcularTotales(venta);

            await VerificarAutorizacionSiCorrespondeAsync(venta, viewModel);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    "La venta fue modificada por otro usuario. Recargá la página y volvé a intentar.");
            }

            _logger.LogInformation("Venta {Id} actualizada exitosamente", id);
            return _mapper.Map<VentaViewModel>(venta);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
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

                // Validación previa del estado
                _validator.ValidarEstadoParaConfirmacion(venta);
                _validator.ValidarStock(venta);

                // Para crédito personal, re-validar requisitos antes de confirmar
                if (venta.TipoPago == TipoPago.CreditoPersonall)
                {
                    var validacion = await _validacionVentaService.ValidarConfirmacionVentaAsync(id);
                    
                    if (validacion.PendienteRequisitos)
                    {
                        throw new InvalidOperationException(
                            $"No se puede confirmar la venta. {validacion.MensajeResumen}");
                    }

                    if (validacion.RequiereAutorizacion && venta.EstadoAutorizacion != EstadoAutorizacionVenta.Autorizada)
                    {
                        throw new InvalidOperationException(
                            $"La venta requiere autorización. {validacion.MensajeResumen}");
                    }
                }
                else
                {
                    _validator.ValidarAutorizacion(venta);
                }

                await DescontarStockYRegistrarMovimientos(venta);

                // E4: Procesar crédito personal solo si hay datos JSON y la venta está autorizada
                if (venta.TipoPago == TipoPago.CreditoPersonall && 
                    !string.IsNullOrEmpty(venta.DatosCreditoPersonallJson))
                {
                    // Verificar que la venta esté autorizada (o no requiera autorización)
                    if (venta.RequiereAutorizacion && venta.EstadoAutorizacion != EstadoAutorizacionVenta.Autorizada)
                    {
                        throw new InvalidOperationException(
                            "No se puede crear el crédito: la venta requiere autorización y no está autorizada.");
                    }

                    // E4: Crear cuotas y asignar CreditoId desde JSON (solo al confirmar post-autorización)
                    await CrearCreditoDefinitivoDesdeJsonAsync(venta);
                }

                await GenerarAlertasStockBajo(venta);

                venta.Estado = EstadoVenta.Confirmada;
                venta.FechaConfirmacion = DateTime.Now;
                // Limpiar requisitos pendientes y datos temporales al confirmar
                venta.RequisitosPendientesJson = null;
                venta.DatosCreditoPersonallJson = null;

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

        /// <summary>
        /// Confirma una venta con crédito personal ya configurado: genera cuotas y marca crédito como Generado.
        /// Este método asume que el crédito ya pasó por ConfigurarVenta (estado = Configurado).
        /// </summary>
        public async Task<bool> ConfirmarVentaCreditoAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var venta = await CargarVentaCompleta(id);
                if (venta == null)
                    return false;

                if (venta.TipoPago != TipoPago.CreditoPersonall)
                    throw new InvalidOperationException("Esta venta no es de tipo Crédito Personal.");

                if (!venta.CreditoId.HasValue)
                    throw new InvalidOperationException("La venta no tiene un crédito asociado.");

                var credito = await _context.Creditos.FindAsync(venta.CreditoId.Value);
                if (credito == null)
                    throw new InvalidOperationException("Crédito no encontrado.");

                // Permitir confirmar si el crédito está Configurado O si la venta tiene el flag
                if (credito.Estado != EstadoCredito.Configurado && !venta.FechaConfiguracionCredito.HasValue)
                    throw new InvalidOperationException(
                        $"El crédito debe estar en estado Configurado para confirmar. Estado actual: {credito.Estado}");

                // Si el crédito no está en Configurado pero la venta tiene el flag, corregir el estado
                if (credito.Estado != EstadoCredito.Configurado && venta.FechaConfiguracionCredito.HasValue)
                {
                    _logger.LogWarning(
                        "Corrigiendo estado de crédito {CreditoId} de {EstadoActual} a Configurado (flag presente en venta)",
                        credito.Id, credito.Estado);
                    credito.Estado = EstadoCredito.Configurado;
                }

                // Validar stock antes de confirmar
                _validator.ValidarStock(venta);
                _validator.ValidarAutorizacion(venta);

                await DescontarStockYRegistrarMovimientos(venta);

                // Generar las cuotas del crédito
                await GenerarCuotasCreditoAsync(credito, venta.Total);

                // Marcar crédito como Generado
                credito.Estado = EstadoCredito.Generado;
                credito.FechaAprobacion = DateTime.Now;

                await GenerarAlertasStockBajo(venta);

                venta.Estado = EstadoVenta.Confirmada;
                venta.FechaConfirmacion = DateTime.Now;
                venta.RequisitosPendientesJson = null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Venta {VentaId} confirmada con crédito {CreditoId} generado ({Cuotas} cuotas)",
                    id, credito.Id, credito.CantidadCuotas);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al confirmar venta con crédito {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Genera las cuotas del crédito según la configuración del plan
        /// </summary>
        private async Task GenerarCuotasCreditoAsync(Credito credito, decimal montoVenta)
        {
            // Calcular cuota usando el servicio financiero
            var montoFinanciado = credito.MontoAprobado > 0 ? credito.MontoAprobado : montoVenta;
            var tasaDecimal = credito.TasaInteres / 100m;
            var cuotaMensual = _financialService.ComputePmt(tasaDecimal, credito.CantidadCuotas, montoFinanciado);

            // Calcular componentes de la cuota (sistema francés simplificado)
            var interesTotal = (cuotaMensual * credito.CantidadCuotas) - montoFinanciado;
            var capitalPorCuota = montoFinanciado / credito.CantidadCuotas;
            var interesPorCuota = interesTotal / credito.CantidadCuotas;

            credito.MontoCuota = cuotaMensual;
            credito.TotalAPagar = cuotaMensual * credito.CantidadCuotas;
            credito.SaldoPendiente = credito.TotalAPagar;

            // Crear las cuotas
            var fechaCuota = credito.FechaPrimeraCuota ?? DateTime.Today.AddMonths(1);
            for (int i = 1; i <= credito.CantidadCuotas; i++)
            {
                var cuota = new Cuota
                {
                    CreditoId = credito.Id,
                    NumeroCuota = i,
                    MontoCapital = capitalPorCuota,
                    MontoInteres = interesPorCuota,
                    MontoTotal = cuotaMensual,
                    FechaVencimiento = fechaCuota,
                    Estado = EstadoCuota.Pendiente
                };
                _context.Cuotas.Add(cuota);
                fechaCuota = fechaCuota.AddMonths(1);
            }

            await Task.CompletedTask;
        }

        public async Task AsociarCreditoAVentaAsync(int ventaId, int creditoId)
        {
            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.Id == ventaId && !v.IsDeleted);
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

                if (venta.TipoPago == TipoPago.CreditoPersonall)
                {
                    // Si la venta fue confirmada, restaurar el crédito
                    if (venta.Estado == EstadoVenta.Confirmada || venta.Estado == EstadoVenta.Facturada)
                    {
                        await RestaurarCreditoPersonall(venta);
                    }
                    else
                    {
                        // Si la venta nunca fue confirmada, solo limpiar datos temporales
                        await LimpiarDatosCreditoVentaAsync(venta);
                    }
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
                .Include(v => v.Detalles.Where(d => !d.IsDeleted)).ThenInclude(d => d.Producto)
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
            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
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
            // E3: Motivo/observación es obligatorio
            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw new ArgumentException(
                    "El motivo/observación es obligatorio para autorizar una venta.",
                    nameof(motivo));
            }

            // E3: Solo se puede autorizar si está en PendienteAutorizacion
            var venta = await ObtenerVentaPendienteAutorizacionAsync(id);
            if (venta == null)
                return false;

            // E3: Registrar auditoría completa
            venta.EstadoAutorizacion = EstadoAutorizacionVenta.Autorizada;
            venta.UsuarioAutoriza = usuarioAutoriza;
            venta.FechaAutorizacion = DateTime.Now;
            venta.MotivoAutorizacion = motivo.Trim();
            
            // Las razones autorizadas ya están en RazonesAutorizacionJson (guardadas al crear)
            // No se modifican, quedan como registro de qué se autorizó

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Venta {Id} autorizada por {Usuario}. Motivo: {Motivo}. Razones: {Razones}",
                id, usuarioAutoriza, motivo, venta.RazonesAutorizacionJson ?? "N/A");
            return true;
        }

        public async Task<bool> RechazarVentaAsync(int id, string usuarioAutoriza, string motivo)
        {
            var venta = await ObtenerVentaPendienteAutorizacionAsync(id);
            if (venta == null)
                return false;

            venta.EstadoAutorizacion = EstadoAutorizacionVenta.Rechazada;
            venta.UsuarioAutoriza = usuarioAutoriza;
            venta.FechaAutorizacion = DateTime.Now;
            venta.MotivoRechazo = motivo;

            // Limpiar datos de crédito al rechazar para evitar "créditos fantasma"
            await LimpiarDatosCreditoVentaAsync(venta);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Venta {Id} rechazada por {Usuario}. Motivo: {Motivo}", id, usuarioAutoriza, motivo);
            return true;
        }

        /// <summary>
        /// Limpia todos los datos de crédito asociados a una venta rechazada o cancelada.
        /// </summary>
        private async Task LimpiarDatosCreditoVentaAsync(Venta venta)
        {
            // Limpiar plan de crédito JSON temporal
            venta.DatosCreditoPersonallJson = null;

            // Eliminar cuotas si existen (no deberían existir si el flujo es correcto)
            var cuotasExistentes = await _context.VentaCreditoCuotas
                .Where(c => c.VentaId == venta.Id)
                .ToListAsync();

            if (cuotasExistentes.Any())
            {
                _context.VentaCreditoCuotas.RemoveRange(cuotasExistentes);
                _logger.LogWarning(
                    "Se eliminaron {Count} cuotas huérfanas de la venta rechazada {VentaId}",
                    cuotasExistentes.Count, venta.Id);
            }

            // Limpiar asociación con crédito
            venta.CreditoId = null;

            _logger.LogInformation("Datos de crédito limpiados para venta {VentaId}", venta.Id);
        }

        public async Task<bool> RequiereAutorizacionAsync(VentaViewModel viewModel)
        {
            if (viewModel.TipoPago != TipoPago.CreditoPersonall)
                return false;

            var cliente = await _context.Clientes
                .Include(c => c.Creditos.Where(cr => !cr.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == viewModel.ClienteId && !c.IsDeleted);

            if (cliente == null)
                return false;

            return await ValidarLimiteCreditoClienteAsync(cliente, viewModel.Total);
        }

        #endregion

        #region MÃ©todos de CÃ¡lculo - Tarjetas

        public async Task<DatosTarjetaViewModel> CalcularCuotasTarjetaAsync(int tarjetaId, decimal monto, int cuotas)
        {
            var configuracion = await _context.ConfiguracionesTarjeta
                .FirstOrDefaultAsync(t => t.Id == tarjetaId && !t.IsDeleted);
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
            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.Id == ventaId && !v.IsDeleted);
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

        public async Task<DatosCreditoPersonallViewModel> CalcularCreditoPersonallAsync(
            int creditoId,
            decimal montoAFinanciar,
            int cuotas,
            DateTime fechaPrimeraCuota)
        {
            var credito = await _context.Creditos
                .Include(c => c.Cuotas)
                .FirstOrDefaultAsync(c => c.Id == creditoId &&
                                          !c.IsDeleted &&
                                          c.Cliente != null &&
                                          !c.Cliente.IsDeleted);

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

            return GenerarDatosCreditoPersonall(
                credito, montoAFinanciar, cuotas, montoCuota,
                totalAPagar, fechaPrimeraCuota);
        }

        public async Task<DatosCreditoPersonallViewModel?> ObtenerDatosCreditoVentaAsync(int ventaId)
        {
            var venta = await _context.Ventas
                .Include(v => v.Credito)
                    .ThenInclude(c => c!.Cliente)
                .Include(v => v.VentaCreditoCuotas.OrderBy(c => c.NumeroCuota))
                .FirstOrDefaultAsync(v => v.Id == ventaId &&
                                          !v.IsDeleted &&
                                          v.CreditoId != null &&
                                          v.Credito != null &&
                                          !v.Credito.IsDeleted &&
                                          v.Credito.Cliente != null &&
                                          !v.Credito.Cliente.IsDeleted);

            if (venta == null || !venta.VentaCreditoCuotas.Any())
                return null;

            var credito = venta.Credito!;
            var totalCuotas = venta.VentaCreditoCuotas.Sum(c => c.Monto);
            var primeraCuota = venta.VentaCreditoCuotas.OrderBy(c => c.NumeroCuota).First();

            var resultado = new DatosCreditoPersonallViewModel
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
                .FirstOrDefaultAsync(c => c.Id == creditoId &&
                                          !c.IsDeleted &&
                                          c.Cliente != null &&
                                          !c.Cliente.IsDeleted);

            if (credito == null || credito.Estado != EstadoCredito.Activo)
                return false;

            return credito.SaldoPendiente >= monto;
        }

        public CalculoTotalesVentaResponse CalcularTotalesPreview(List<DetalleCalculoVentaRequest> detalles, decimal descuentoGeneral, bool descuentoEsPorcentaje)
        {
            return CalcularTotalesInterno(detalles, descuentoGeneral, descuentoEsPorcentaje);
        }

        #endregion

        #region MÃ©todos Auxiliares - Cheques

        public async Task<bool> GuardarDatosChequeAsync(int ventaId, DatosChequeViewModel datosCheque)
        {
            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.Id == ventaId && !v.IsDeleted);
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
                .Include(v => v.Detalles.Where(d => !d.IsDeleted && d.Producto != null && !d.Producto.IsDeleted)).ThenInclude(d => d.Producto)
                .Include(v => v.Credito)
                .Include(v => v.Cliente)
                .Include(v => v.VentaCreditoCuotas)
                .FirstOrDefaultAsync(v =>
                    v.Id == id &&
                    !v.IsDeleted &&
                    (v.Cliente == null || !v.Cliente.IsDeleted) &&
                    (v.Credito == null || (!v.Credito.IsDeleted && v.Credito.Cliente != null && !v.Credito.Cliente.IsDeleted)));
        }

        private async Task<Venta?> ObtenerVentaPendienteAutorizacionAsync(int id)
        {
            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (venta == null)
                return null;

            _validator.ValidarEstadoAutorizacion(venta, EstadoAutorizacionVenta.PendienteAutorizacion);
            return venta;
        }

        private void CalcularTotales(Venta venta)
        {
            var detallesList = venta.Detalles.Where(d => !d.IsDeleted).ToList();

            var detalleRequests = detallesList
                .Select(d => new DetalleCalculoVentaRequest
                {
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Descuento = d.Descuento
                })
                .ToList();

            var resultado = CalcularTotalesInterno(detalleRequests, venta.Descuento, false);

            for (var i = 0; i < detallesList.Count; i++)
            {
                var detalle = detallesList[i];
                var request = detalleRequests[i];
                var subtotalDetalle = Math.Max(0, (request.PrecioUnitario * request.Cantidad) - request.Descuento);
                detalle.Subtotal = subtotalDetalle;
            }

            venta.Subtotal = resultado.Subtotal;
            venta.IVA = resultado.IVA;
            venta.Total = resultado.Total;
        }

        private CalculoTotalesVentaResponse CalcularTotalesInterno(IEnumerable<DetalleCalculoVentaRequest> detalles, decimal descuentoGeneral, bool descuentoEsPorcentaje)
        {
            var subtotal = detalles
                .Select(d => Math.Max(0, (d.PrecioUnitario * d.Cantidad) - d.Descuento))
                .Sum();

            var descuentoCalculado = descuentoEsPorcentaje
                ? subtotal * (descuentoGeneral / 100)
                : descuentoGeneral;

            var subtotalConDescuento = Math.Max(0, subtotal - descuentoCalculado);
            var iva = subtotalConDescuento * VentaConstants.IVA_RATE;
            var total = subtotalConDescuento + iva;

            return new CalculoTotalesVentaResponse
            {
                Subtotal = subtotal,
                DescuentoGeneralAplicado = descuentoCalculado,
                IVA = iva,
                Total = total
            };
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
            foreach (var existente in venta.Detalles.Where(d => !d.IsDeleted))
            {
                existente.IsDeleted = true;
            }

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
            var usuario = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";

            var referencia = $"Venta {venta.Numero}";
            var motivo = $"Confirmación de venta - Cliente: {venta.Cliente?.Nombre ?? "(sin cliente)"}";

            var salidas = venta.Detalles
                .Where(d => !d.IsDeleted)
                .Select(d => (d.ProductoId, (decimal)d.Cantidad, (string?)referencia))
                .ToList();

            await _movimientoStockService.RegistrarSalidasAsync(
                salidas,
                motivo,
                usuario);
        }

        private async Task DevolverStock(Venta venta, string motivo)
        {
            var usuario = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";

            var referencia = $"Cancelación Venta {venta.Numero}";

            var entradas = venta.Detalles
                .Where(d => !d.IsDeleted)
                .Select(d => (d.ProductoId, (decimal)d.Cantidad, (string?)referencia))
                .ToList();

            await _movimientoStockService.RegistrarEntradasAsync(
                entradas,
                motivo,
                usuario);
        }

        private async Task AplicarPrecioVigenteADetallesAsync(Venta venta)
        {
            var detalles = venta.Detalles.Where(d => !d.IsDeleted).ToList();
            if (detalles.Count == 0)
                return;

            var listaPredeterminada = await _precioService.GetListaPredeterminadaAsync();
            var productoIds = detalles.Select(d => d.ProductoId).Distinct().ToList();

            var preciosListaPorProductoId = new Dictionary<int, decimal>();
            if (listaPredeterminada != null && productoIds.Count > 0)
            {
                var fecha = DateTime.UtcNow;

                var precios = await _context.ProductosPrecios
                    .AsNoTracking()
                    .Where(p => productoIds.Contains(p.ProductoId)
                             && p.ListaId == listaPredeterminada.Id
                             && p.VigenciaDesde <= fecha
                             && (p.VigenciaHasta == null || p.VigenciaHasta >= fecha)
                             && p.EsVigente
                             && !p.IsDeleted)
                    .Select(p => new { p.ProductoId, p.Precio })
                    .ToListAsync();

                // Defensive: si por datos sucios hubiese duplicados, priorizar el último leído.
                foreach (var p in precios)
                    preciosListaPorProductoId[p.ProductoId] = p.Precio;
            }

            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => productoIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => new { p.Id, p.PrecioVenta })
                .ToDictionaryAsync(p => p.Id, p => p.PrecioVenta);

            var cache = new Dictionary<int, decimal>();

            foreach (var detalle in detalles)
            {
                if (!cache.TryGetValue(detalle.ProductoId, out var precioUnitario))
                {
                    productos.TryGetValue(detalle.ProductoId, out var fallbackPrecioVenta);
                    precioUnitario = fallbackPrecioVenta;

                    if (preciosListaPorProductoId.TryGetValue(detalle.ProductoId, out var precioLista))
                        precioUnitario = precioLista;

                    cache[detalle.ProductoId] = precioUnitario;
                }

                detalle.PrecioUnitario = precioUnitario;
            }
        }

        private async Task RestaurarCreditoPersonall(Venta venta)
        {
            if (!venta.CreditoId.HasValue || !venta.VentaCreditoCuotas.Any())
                return;

            var credito = venta.Credito ?? await _context.Creditos
                .FirstOrDefaultAsync(c => c.Id == venta.CreditoId!.Value &&
                                          !c.IsDeleted &&
                                          c.Cliente != null &&
                                          !c.Cliente.IsDeleted);
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
            var productoIds = venta.Detalles
                .Where(d => !d.IsDeleted)
                .Select(d => d.ProductoId)
                .Distinct()
                .ToList();

            await _alertaStockService.VerificarYGenerarAlertasAsync(productoIds);
        }

        private async Task VerificarAutorizacionSiCorrespondeAsync(Venta venta, VentaViewModel viewModel)
        {
            if (viewModel.TipoPago == TipoPago.CreditoPersonall)
            {
                var cliente = await _context.Clientes
                    .Include(c => c.Creditos.Where(cr => !cr.IsDeleted))
                    .FirstOrDefaultAsync(c => c.Id == viewModel.ClienteId && !c.IsDeleted);

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

            // Para crédito personal: guardar plan como JSON, NO crear cuotas todavía
            // Las cuotas se crean solo al confirmar la venta
            if (viewModel.DatosCreditoPersonall != null && viewModel.TipoPago == TipoPago.CreditoPersonall)
            {
                await GuardarPlanCreditoPersonallAsync(ventaId, viewModel.DatosCreditoPersonall);
            }
        }

        /// <summary>
        /// Guarda el plan de crédito personal como JSON para usarlo al confirmar.
        /// NO crea cuotas ni modifica el saldo del crédito.
        /// </summary>
        private async Task GuardarPlanCreditoPersonallAsync(int ventaId, DatosCreditoPersonallViewModel datos)
        {
            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.Id == ventaId && !v.IsDeleted);
            if (venta == null)
                throw new InvalidOperationException(VentaConstants.ErrorMessages.VENTA_NO_ENCONTRADA);

            // Serializar el plan de crédito para usarlo al confirmar
            venta.DatosCreditoPersonallJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                datos.CreditoId,
                datos.MontoAFinanciar,
                datos.CantidadCuotas,
                datos.MontoCuota,
                datos.TotalAPagar,
                datos.TasaInteresMensual,
                datos.FechaPrimeraCuota,
                datos.InteresTotal
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Plan de crédito personal guardado para venta {VentaId}. CreditoId: {CreditoId}, Monto: {Monto}, Cuotas: {Cuotas}",
                ventaId, datos.CreditoId, datos.MontoAFinanciar, datos.CantidadCuotas);
        }

        /// <summary>
        /// Crea las cuotas desde el plan JSON almacenado. Solo se llama al confirmar la venta.
        /// </summary>
        private async Task CrearCuotasDesdeJsonAsync(Venta venta)
        {
            if (string.IsNullOrEmpty(venta.DatosCreditoPersonallJson))
            {
                _logger.LogWarning("No hay datos de crédito JSON para la venta {VentaId}", venta.Id);
                return;
            }

            try
            {
                var planJson = System.Text.Json.JsonDocument.Parse(venta.DatosCreditoPersonallJson);
                var root = planJson.RootElement;

                var creditoId = root.GetProperty("CreditoId").GetInt32();
                var montoAFinanciar = root.GetProperty("MontoAFinanciar").GetDecimal();
                var cantidadCuotas = root.GetProperty("CantidadCuotas").GetInt32();
                var montoCuota = root.GetProperty("MontoCuota").GetDecimal();
                var fechaPrimeraCuota = root.GetProperty("FechaPrimeraCuota").GetDateTime();

                // Crear las cuotas
                for (int i = 0; i < cantidadCuotas; i++)
                {
                    var cuota = new VentaCreditoCuota
                    {
                        VentaId = venta.Id,
                        CreditoId = creditoId,
                        NumeroCuota = i + 1,
                        FechaVencimiento = fechaPrimeraCuota.AddMonths(i),
                        Monto = montoCuota,
                        Saldo = montoAFinanciar, // Saldo original de la venta
                        Pagada = false
                    };
                    _context.VentaCreditoCuotas.Add(cuota);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Cuotas creadas para venta {VentaId}: {CantidadCuotas} cuotas de {MontoCuota:C2}",
                    venta.Id, cantidadCuotas, montoCuota);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Error al deserializar datos de crédito JSON para venta {VentaId}", venta.Id);
                throw new InvalidOperationException("Error al procesar los datos del plan de crédito");
            }
        }

        /// <summary>
        /// E4: Crea el crédito definitivo, cuotas y descuenta del cupo.
        /// Solo se llama al confirmar una venta autorizada (o que no requiere autorización).
        /// </summary>
        private async Task CrearCreditoDefinitivoDesdeJsonAsync(Venta venta)
        {
            if (string.IsNullOrEmpty(venta.DatosCreditoPersonallJson))
            {
                throw new InvalidOperationException(
                    "No hay datos del plan de crédito para crear el crédito definitivo.");
            }

            try
            {
                var planJson = System.Text.Json.JsonDocument.Parse(venta.DatosCreditoPersonallJson);
                var root = planJson.RootElement;

                var creditoId = root.GetProperty("CreditoId").GetInt32();
                var montoAFinanciar = root.GetProperty("MontoAFinanciar").GetDecimal();
                var cantidadCuotas = root.GetProperty("CantidadCuotas").GetInt32();
                var montoCuota = root.GetProperty("MontoCuota").GetDecimal();
                var fechaPrimeraCuota = root.GetProperty("FechaPrimeraCuota").GetDateTime();

                // Obtener el crédito y validar saldo disponible
                var credito = await _context.Creditos
                    .FirstOrDefaultAsync(c => c.Id == creditoId && !c.IsDeleted);
                
                if (credito == null)
                {
                    throw new InvalidOperationException(VentaConstants.ErrorMessages.CREDITO_NO_ENCONTRADO);
                }

                if (credito.SaldoPendiente < montoAFinanciar)
                {
                    throw new InvalidOperationException(
                        $"Saldo de crédito insuficiente. Disponible: ${credito.SaldoPendiente:N2}, Requerido: ${montoAFinanciar:N2}");
                }

                // E4: Asignar CreditoId a la venta (ahora sí, post-autorización)
                venta.CreditoId = creditoId;

                // Crear las cuotas
                for (int i = 0; i < cantidadCuotas; i++)
                {
                    var cuota = new VentaCreditoCuota
                    {
                        VentaId = venta.Id,
                        CreditoId = creditoId,
                        NumeroCuota = i + 1,
                        FechaVencimiento = fechaPrimeraCuota.AddMonths(i),
                        Monto = montoCuota,
                        Saldo = montoAFinanciar,
                        Pagada = false
                    };
                    _context.VentaCreditoCuotas.Add(cuota);
                }

                // E4: Descontar del cupo del crédito
                credito.SaldoPendiente -= montoAFinanciar;
                _context.Creditos.Update(credito);

                _logger.LogInformation(
                    "E4: Crédito definitivo creado para venta {VentaId}. CreditoId: {CreditoId}, " +
                    "Monto: {Monto:C2}, Cuotas: {Cuotas}, Nuevo saldo disponible: {SaldoDisponible:C2}",
                    venta.Id, creditoId, montoAFinanciar, cantidadCuotas, credito.SaldoPendiente);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Error al deserializar datos de crédito JSON para venta {VentaId}", venta.Id);
                throw new InvalidOperationException("Error al procesar los datos del plan de crédito");
            }
        }

        private async Task GuardarCuotasCreditoPersonallAsync(int ventaId, DatosCreditoPersonallViewModel datos)
        {
            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.Id == ventaId && !v.IsDeleted);
            if (venta == null)
                throw new InvalidOperationException(VentaConstants.ErrorMessages.VENTA_NO_ENCONTRADA);

            var credito = await _context.Creditos
                .FirstOrDefaultAsync(c => c.Id == datos.CreditoId &&
                                          !c.IsDeleted &&
                                          c.Cliente != null &&
                                          !c.Cliente.IsDeleted);
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

        private async Task ProcesarCreditoPersonallVentaAsync(Venta venta)
        {
            if (venta.Credito == null)
            {
                if (venta.CreditoId.HasValue)
                {
                    venta.Credito = await _context.Creditos
                        .FirstOrDefaultAsync(c => c.Id == venta.CreditoId.Value &&
                                                  !c.IsDeleted &&
                                                  c.Cliente != null &&
                                                  !c.Cliente.IsDeleted);
                }
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

        private DatosCreditoPersonallViewModel GenerarDatosCreditoPersonall(
            Credito credito,
            decimal montoAFinanciar,
            int cuotas,
            decimal montoCuota,
            decimal totalAPagar,
            DateTime fechaPrimeraCuota)
        {
            var resultado = new DatosCreditoPersonallViewModel
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
