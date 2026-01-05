using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Helpers;
using TheBuryProject.Data;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente + "," + Roles.Vendedor)]
    public class CreditoController : Controller
    {
        private readonly ICreditoService _creditoService;
        private readonly IEvaluacionCreditoService _evaluacionService;
        private readonly IFinancialCalculationService _financialService;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<CreditoController> _logger;
        private readonly IClienteLookupService _clienteLookup;
        private readonly IProductoService _productoService;

        private string? GetSafeReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null;
        }

        private IActionResult RedirectToReturnUrlOrDetails(string? returnUrl, int creditoId)
        {
            var safeReturnUrl = GetSafeReturnUrl(returnUrl);
            return safeReturnUrl != null
                ? LocalRedirect(safeReturnUrl)
                : RedirectToAction(nameof(Details), new { id = creditoId });
        }

        private IActionResult RedirectToReturnUrlOrIndex(string? returnUrl)
        {
            var safeReturnUrl = GetSafeReturnUrl(returnUrl);
            return safeReturnUrl != null
                ? LocalRedirect(safeReturnUrl)
                : RedirectToAction(nameof(Index));
        }

        public CreditoController(
            ICreditoService creditoService,
            IEvaluacionCreditoService evaluacionService,
            IFinancialCalculationService financialService,
            IDbContextFactory<AppDbContext> contextFactory,
            IMapper mapper,
            ILogger<CreditoController> logger,
            IClienteLookupService clienteLookup,
            IProductoService productoService)
        {
            _creditoService = creditoService;
            _evaluacionService = evaluacionService;
            _financialService = financialService;
            _contextFactory = contextFactory;
            _mapper = mapper;
            _logger = logger;
            _clienteLookup = clienteLookup;
            _productoService = productoService;
        }

        // GET: Credito
        public async Task<IActionResult> Index(CreditoFilterViewModel filter)
        {
            try
            {
                var creditos = await _creditoService.GetAllAsync(filter);
                ViewBag.Filter = filter;
                return View(creditos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar créditos");
                TempData["Error"] = "Error al cargar los créditos";
                return View(new List<CreditoViewModel>());
            }
        }

        // GET: Credito/Simular
        [HttpGet]
        public IActionResult Simular(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
            return View(new SimularCreditoViewModel
            {
                CantidadCuotas = 12,
                TasaInteresMensual = 0.05m
            });
        }

        // POST: Credito/Simular
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Simular(SimularCreditoViewModel modelo, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

            if (!ModelState.IsValid)
                return View(modelo);

            try
            {
                var resultado = await _creditoService.SimularCreditoAsync(modelo);
                return View(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al simular crédito");
                TempData["Error"] = "Error al simular el crédito: " + ex.Message;
                return View(modelo);
            }
        }

        // GET: Credito/Details/5
        public async Task<IActionResult> Details(int id, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var evaluacion = await _evaluacionService.GetEvaluacionByCreditoIdAsync(id);
                var detalle = new CreditoDetalleViewModel
                {
                    Credito = credito,
                    Evaluacion = evaluacion
                };

                return View(detalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener crédito {Id}", id);
                TempData["Error"] = "Error al cargar el crédito";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id, string? returnUrl = null)
        {
            try
            {
                var aprobadoPor = User.Identity?.Name ?? "Sistema";

                var ok = await _creditoService.AprobarCreditoAsync(id, aprobadoPor);
                TempData[ok ? "Success" : "Error"] = ok
                    ? "Crédito aprobado exitosamente"
                    : "No se pudo aprobar el crédito";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aprobar crédito {Id}", id);
                TempData["Error"] = "Error al aprobar el crédito: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string motivo, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["Error"] = "Debe especificar un motivo para rechazar.";
                return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
            }

            try
            {
                var ok = await _creditoService.RechazarCreditoAsync(id, motivo);
                TempData[ok ? "Success" : "Error"] = ok
                    ? "Crédito rechazado."
                    : "No se pudo rechazar el crédito";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar crédito {Id}", id);
                TempData["Error"] = "Error al rechazar el crédito: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id, string motivo, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["Error"] = "Debe especificar un motivo para cancelar.";
                return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
            }

            try
            {
                var ok = await _creditoService.CancelarCreditoAsync(id, motivo);
                TempData[ok ? "Success" : "Error"] = ok
                    ? "Crédito cancelado."
                    : "No se pudo cancelar el crédito";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar crédito {Id}", id);
                TempData["Error"] = "Error al cancelar el crédito: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
        }

        [HttpGet]
        public async Task<IActionResult> ConfigurarVenta(int id, int? ventaId, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

            var credito = await _creditoService.GetByIdAsync(id);
            if (credito == null)
            {
                TempData["Error"] = "Crédito no encontrado";
                return RedirectToAction(nameof(Index));
            }

            // Validar que el crédito esté en estado que permita configuración
            if (credito.Estado != EstadoCredito.PendienteConfiguracion && 
                credito.Estado != EstadoCredito.Solicitado)
            {
                // Si ya está Configurado, Generado o más avanzado, no permitir reconfigurar
                if (credito.Estado == EstadoCredito.Configurado)
                {
                    TempData["Info"] = "El crédito ya está configurado. Puede confirmar la venta.";
                }
                else if (credito.Estado == EstadoCredito.Generado || 
                         credito.Estado == EstadoCredito.Activo ||
                         credito.Estado == EstadoCredito.Finalizado)
                {
                    TempData["Warning"] = "El crédito ya fue generado y no puede reconfigurarse.";
                }
                else
                {
                    TempData["Error"] = $"El crédito no puede configurarse en estado {credito.Estado}.";
                }

                if (ventaId.HasValue)
                    return RedirectToAction("Details", "Venta", new { id = ventaId });
                return RedirectToAction("Details", new { id });
            }

            decimal montoVenta = credito.MontoAprobado;

            if (montoVenta <= 0)
                montoVenta = credito.MontoSolicitado;

            if (ventaId.HasValue)
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var venta = await context.Ventas
                    .Include(v => v.Detalles)
                    .FirstOrDefaultAsync(v => v.Id == ventaId.Value && !v.IsDeleted);

                if (venta != null)
                {
                    // Prioriza el total guardado; si no existe, recalcula desde el detalle
                    montoVenta = venta.Total;

                    var detallesVenta = (venta.Detalles ?? new List<VentaDetalle>())
                        .Where(d => !d.IsDeleted)
                        .ToList();

                    if (montoVenta <= 0 && detallesVenta.Count > 0)
                    {
                        var subtotal = detallesVenta.Sum(d =>
                            d.Subtotal > 0
                                ? d.Subtotal
                                : Math.Max(0, (d.Cantidad * d.PrecioUnitario) - d.Descuento));

                        var subtotalConDescuento = subtotal - venta.Descuento;
                        var iva = venta.IVA > 0 ? venta.IVA : subtotalConDescuento * VentaConstants.IVA_RATE;
                        montoVenta = subtotalConDescuento + iva;
                    }

                    if (montoVenta <= 0)
                    {
                        // Último recurso: traer los detalles directamente y recalcular cuando la navegación no trae datos
                        var detallesPersistidos = await context.VentaDetalles
                            .Where(d => d.VentaId == venta.Id && !d.IsDeleted)
                            .ToListAsync();

                        if (detallesPersistidos.Any())
                        {
                            var subtotalPersistido = detallesPersistidos.Sum(d =>
                                d.Subtotal > 0
                                    ? d.Subtotal
                                    : Math.Max(0, (d.Cantidad * d.PrecioUnitario) - d.Descuento));

                            var subtotalConDescuento = subtotalPersistido - venta.Descuento;
                            var iva = venta.IVA > 0 ? venta.IVA : subtotalConDescuento * VentaConstants.IVA_RATE;
                            montoVenta = subtotalConDescuento + iva;
                        }
                    }

                }
            }

            var modelo = new ConfiguracionCreditoVentaViewModel
            {
                CreditoId = credito.Id,
                VentaId = ventaId,
                ClienteNombre = credito.ClienteNombre ?? string.Empty,
                NumeroCredito = credito.Numero,
                Monto = montoVenta,
                Anticipo = 0,
                MontoFinanciado = montoVenta,
                CantidadCuotas = credito.CantidadCuotas > 0 ? credito.CantidadCuotas : 0,
                TasaMensual = credito.TasaInteres > 0 ? credito.TasaInteres : 0,
                GastosAdministrativos = 0,
                FechaPrimeraCuota = credito.FechaPrimeraCuota
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigurarVenta(ConfiguracionCreditoVentaViewModel modelo, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
                return View(modelo);
            }

            // Normalizar campos opcionales a valores por defecto
            var anticipo = modelo.Anticipo ?? 0m;
            var tasaMensual = modelo.TasaMensual ?? 0m;
            var gastosAdministrativos = modelo.GastosAdministrativos ?? 0m;

            var credito = await _creditoService.GetByIdAsync(modelo.CreditoId);
            if (credito == null)
            {
                TempData["Error"] = "Crédito no encontrado";
                return RedirectToAction(nameof(Index));
            }

            credito.CantidadCuotas = modelo.CantidadCuotas;
            credito.TasaInteres = tasaMensual;
            credito.FechaPrimeraCuota = modelo.FechaPrimeraCuota;
            credito.MontoAprobado = Math.Max(0, modelo.Monto - anticipo);
            credito.MontoSolicitado = credito.MontoAprobado;
            credito.SaldoPendiente = credito.MontoAprobado;
            // Marcar como Configurado (no Solicitado) para evitar loop al confirmar
            credito.Estado = EstadoCredito.Configurado;

            if (gastosAdministrativos > 0)
            {
                credito.Observaciones = string.IsNullOrWhiteSpace(credito.Observaciones)
                    ? $"Gastos administrativos declarados: ${gastosAdministrativos:N2}"
                    : $"{credito.Observaciones} | Gastos administrativos: ${gastosAdministrativos:N2}";
            }

            await _creditoService.UpdateAsync(credito);

            // Actualizar la venta: marcar financiación configurada y cambiar estado
            if (modelo.VentaId.HasValue)
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var venta = await context.Ventas.FindAsync(modelo.VentaId.Value);
                if (venta != null)
                {
                    // Marcar flag persistente de configuración
                    if (!venta.FechaConfiguracionCredito.HasValue)
                    {
                        venta.FechaConfiguracionCredito = DateTime.UtcNow;
                    }
                    
                    // Cambiar estado de PendienteFinanciacion a Presupuesto (listo para confirmar)
                    if (venta.Estado == EstadoVenta.PendienteFinanciacion)
                    {
                        venta.Estado = EstadoVenta.Presupuesto;
                        _logger.LogInformation(
                            "Venta {VentaId} cambiada de PendienteFinanciacion a Presupuesto", 
                            modelo.VentaId.Value);
                    }
                    
                    await context.SaveChangesAsync();
                    _logger.LogInformation(
                        "Flag FechaConfiguracionCredito establecido para venta {VentaId}", 
                        modelo.VentaId.Value);
                }

                // Redirigir a Venta/Details (flujo lineal)
                TempData["Success"] = "Crédito configurado. Puede confirmar la venta.";
                return RedirectToAction("Details", "Venta", new { id = modelo.VentaId.Value });
            }

            TempData["Success"] = "Crédito configurado y listo para confirmación.";
            return RedirectToReturnUrlOrDetails(returnUrl, credito.Id);
        }

        /// <summary>
        /// Simula el plan de cuotas para una venta. Los parámetros opcionales se normalizan a 0 si vienen vacíos.
        /// </summary>
        [HttpGet]
        public IActionResult SimularPlanVenta(
            decimal totalVenta,
            decimal? anticipo,
            int cuotas,
            decimal? tasaMensual,
            decimal? gastosAdministrativos,
            string? fechaPrimeraCuota)
        {
            try
            {
                // Normalizar campos opcionales
                var anticipoVal = anticipo ?? 0m;
                var tasaVal = tasaMensual ?? 0m;
                var gastosVal = gastosAdministrativos ?? 0m;

                if (totalVenta <= 0)
                    return BadRequest(new { error = "El monto total de la venta debe ser mayor a cero." });

                if (anticipoVal < 0)
                    return BadRequest(new { error = "El anticipo no puede ser negativo." });

                if (cuotas <= 0)
                    return BadRequest(new { error = "Ingresá una cantidad de cuotas mayor a cero." });

                if (tasaVal < 0)
                    return BadRequest(new { error = "La tasa mensual no puede ser negativa." });

                if (gastosVal < 0)
                    return BadRequest(new { error = "Los gastos administrativos no pueden ser negativos." });

                var fecha = DateTime.TryParse(fechaPrimeraCuota, out var parsed) ? parsed : DateTime.Today.AddMonths(1);

                var montoFinanciado = _financialService.ComputeFinancedAmount(totalVenta, anticipoVal);
                var tasaDecimal = tasaVal / 100;
                var cuota = _financialService.ComputePmt(tasaDecimal, cuotas, montoFinanciado);
                var interesTotal = _financialService.CalcularInteresTotal(montoFinanciado, tasaDecimal, cuotas);
                var totalCuotas = cuota * cuotas;
                var totalPlan = totalCuotas + gastosVal;

                var semaforo = CalcularSemaforo(cuota, montoFinanciado);

                return Json(new
                {
                    montoFinanciado,
                    cuotaEstimada = cuota,
                    tasaAplicada = tasaVal,
                    interesTotal,
                    totalAPagar = totalCuotas,
                    gastosAdministrativos = gastosVal,
                    totalPlan,
                    fechaPrimerPago = fecha.ToString("yyyy-MM-dd"),
                    semaforoEstado = semaforo.Estado,
                    semaforoMensaje = semaforo.Mensaje,
                    mostrarMsgIngreso = semaforo.MostrarIngreso,
                    mostrarMsgAntiguedad = semaforo.MostrarAntiguedad
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al simular plan de crédito");
                return StatusCode(500, new { error = "Ocurrió un error al calcular el plan de crédito." });
            }
        }

        private static SemaforoPrecalificacion CalcularSemaforo(decimal cuota, decimal montoFinanciado)
        {
            if (montoFinanciado <= 0 || cuota <= 0)
                return new SemaforoPrecalificacion("sinDatos", "Completa los datos para precalificar.", false, false);

            var ratio = cuota / montoFinanciado;

            if (ratio <= 0.08m)
                return new SemaforoPrecalificacion("verde", "Condiciones preliminares saludables.", false, false);

            if (ratio <= 0.15m)
                return new SemaforoPrecalificacion("amarillo", "Revisar ingresos declarados.", true, false);

            return new SemaforoPrecalificacion("rojo", "Las condiciones requieren ajustes.", true, true);
        }

        private record SemaforoPrecalificacion(string Estado, string Mensaje, bool MostrarIngreso, bool MostrarAntiguedad);

        // GET: Credito/Create
        public async Task<IActionResult> Create(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
            await CargarViewBags();
            return View(new CreditoViewModel
            {
                FechaSolicitud = DateTime.UtcNow,
                TasaInteres = 0.05m,
                CantidadCuotas = 12
            });
        }

        // POST: Credito/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditoViewModel viewModel, string? returnUrl = null)
        {
            _logger.LogInformation("=== INICIANDO CREACIÓN DE LÍNEA DE CRÉDITO ===");
            _logger.LogInformation("ClienteId: {ClienteId}", viewModel.ClienteId);
            _logger.LogInformation("MontoSolicitado: {Monto}", viewModel.MontoSolicitado);
            _logger.LogInformation("TasaInteres: {Tasa}", viewModel.TasaInteres);
            _logger.LogInformation("RequiereGarante: {RequiereGarante}", viewModel.RequiereGarante);

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState inválido al crear crédito");
                    ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
                    await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                    return View(viewModel);
                }

                var credito = await _creditoService.CreateAsync(viewModel);

                TempData["Success"] = $"Línea de Crédito {credito.Numero} creada exitosamente";
                return RedirectToAction(nameof(Details), new { id = credito.Id, returnUrl = GetSafeReturnUrl(returnUrl) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear línea de crédito");
                ModelState.AddModelError("", "Error al crear la línea de crédito: " + ex.Message);
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
                await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                return View(viewModel);
            }
        }

        // GET: Credito/Edit/5
        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (credito.Estado != EstadoCredito.Solicitado)
                {
                    TempData["Error"] = "Solo se pueden editar créditos en estado Solicitado";
                    return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
                }

                await CargarViewBags(credito.ClienteId, credito.GaranteId);
                return View(credito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar crédito para editar: {Id}", id);
                TempData["Error"] = "Error al cargar el crédito";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Credito/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreditoViewModel viewModel, string? returnUrl = null)
        {
            if (id != viewModel.Id)
                return RedirectToAction(nameof(Index));

            try
            {
                if (!ModelState.IsValid)
                {
                    ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
                    await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                    return View(viewModel);
                }

                var resultado = await _creditoService.UpdateAsync(viewModel);
                if (resultado)
                {
                    TempData["Success"] = "Crédito actualizado exitosamente";
                    return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
                }

                TempData["Error"] = "No se pudo actualizar el crédito";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar crédito: {Id}", id);
                ModelState.AddModelError("", "Error al actualizar el crédito: " + ex.Message);
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
                await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                return View(viewModel);
            }
        }

        // GET: Credito/Delete/5
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(credito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar crédito para eliminar: {Id}", id);
                TempData["Error"] = "Error al cargar el crédito";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Credito/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl = null)
        {
            try
            {
                var resultado = await _creditoService.DeleteAsync(id);
                if (resultado)
                    TempData["Success"] = "Crédito eliminado exitosamente";
                else
                    TempData["Error"] = "No se pudo eliminar el crédito";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar crédito: {Id}", id);
                TempData["Error"] = "Error al eliminar el crédito: " + ex.Message;
            }

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        // GET: Credito/PagarCuota/5
        public async Task<IActionResult> PagarCuota(int id, int? cuotaId = null, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var cuotasDisponibles = (credito.Cuotas ?? new List<CuotaViewModel>())
                    .Where(c => c.Estado == EstadoCuota.Pendiente || c.Estado == EstadoCuota.Vencida || c.Estado == EstadoCuota.Parcial)
                    .OrderBy(c => c.NumeroCuota)
                    .ToList();

                if (!cuotasDisponibles.Any())
                {
                    TempData["Warning"] = "No hay cuotas pendientes o vencidas para registrar pago.";
                    return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
                }

                var cuotasPendientes = cuotasDisponibles
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = $"Cuota #{c.NumeroCuota} - Vto: {c.FechaVencimiento:dd/MM/yyyy} - {c.MontoTotal:C}"
                    })
                    .ToList();

                ViewBag.Cuotas = cuotasPendientes;

                var cuotaSeleccionada = cuotaId.HasValue
                    ? cuotasDisponibles.FirstOrDefault(c => c.Id == cuotaId.Value)
                    : cuotasDisponibles.FirstOrDefault();

                if (cuotaSeleccionada == null)
                {
                    TempData["Error"] = "Cuota no encontrada.";
                    return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
                }

                var estaVencida = cuotaSeleccionada.FechaVencimiento.Date < DateTime.Today;
                var diasAtraso = estaVencida ? (DateTime.Today - cuotaSeleccionada.FechaVencimiento.Date).Days : 0;

                var modelo = new PagarCuotaViewModel
                {
                    CreditoId = credito.Id,
                    CuotaId = cuotaSeleccionada.Id,
                    NumeroCuota = cuotaSeleccionada.NumeroCuota,
                    MontoCuota = cuotaSeleccionada.MontoTotal,
                    MontoPunitorio = cuotaSeleccionada.MontoPunitorio,
                    TotalAPagar = cuotaSeleccionada.MontoTotal + cuotaSeleccionada.MontoPunitorio,
                    MontoPagado = cuotaSeleccionada.MontoTotal + cuotaSeleccionada.MontoPunitorio,
                    ClienteNombre = credito.ClienteNombre,
                    NumeroCreditoTexto = credito.Numero,
                    FechaVencimiento = cuotaSeleccionada.FechaVencimiento,
                    EstaVencida = estaVencida,
                    DiasAtraso = diasAtraso,
                    FechaPago = DateTime.UtcNow
                };

                return View(modelo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar pago de cuota: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Credito/PagarCuota
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagarCuota(PagarCuotaViewModel modelo, string? returnUrl = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                    var credito = await _creditoService.GetByIdAsync(modelo.CreditoId);
                    if (credito == null)
                    {
                        TempData["Error"] = "Crédito no encontrado";
                        return RedirectToAction(nameof(Index));
                    }

                    var cuotasPendientes = (credito.Cuotas ?? new List<CuotaViewModel>())
                        .Where(c => c.Estado == EstadoCuota.Pendiente || c.Estado == EstadoCuota.Vencida || c.Estado == EstadoCuota.Parcial)
                        .OrderBy(c => c.NumeroCuota)
                        .Select(c => new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = $"Cuota #{c.NumeroCuota} - Vto: {c.FechaVencimiento:dd/MM/yyyy} - {c.MontoTotal:C}"
                        })
                        .ToList();

                    ViewBag.Cuotas = cuotasPendientes;

                    return View(modelo);
                }

                var resultado = await _creditoService.PagarCuotaAsync(modelo);

                if (resultado)
                {
                    TempData["Success"] = "Pago registrado exitosamente";
                    return RedirectToReturnUrlOrDetails(returnUrl, modelo.CreditoId);
                }

                ModelState.AddModelError(string.Empty, "No se pudo registrar el pago");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al pagar cuota");
                ModelState.AddModelError("", "Error al registrar el pago: " + ex.Message);
            }

            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

            try
            {
                var credito = await _creditoService.GetByIdAsync(modelo.CreditoId);
                var cuotasPendientes = (credito?.Cuotas ?? new List<CuotaViewModel>())
                    .Where(c => c.Estado == EstadoCuota.Pendiente || c.Estado == EstadoCuota.Vencida || c.Estado == EstadoCuota.Parcial)
                    .OrderBy(c => c.NumeroCuota)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = $"Cuota #{c.NumeroCuota} - Vto: {c.FechaVencimiento:dd/MM/yyyy} - {c.MontoTotal:C}"
                    })
                    .ToList();

                ViewBag.Cuotas = cuotasPendientes;
            }
            catch
            {
                // si falla, igual mostramos la vista con errores
            }

            return View(modelo);
        }

        // GET: Credito/AdelantarCuota/5
        public async Task<IActionResult> AdelantarCuota(int id, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Obtener la ÚLTIMA cuota pendiente (la que se cancela al adelantar)
                var ultimaCuota = await _creditoService.GetUltimaCuotaPendienteAsync(id);
                if (ultimaCuota == null)
                {
                    TempData["Warning"] = "No hay cuotas pendientes para adelantar.";
                    return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
                }

                var modelo = new PagarCuotaViewModel
                {
                    CreditoId = credito.Id,
                    CuotaId = ultimaCuota.Id,
                    NumeroCuota = ultimaCuota.NumeroCuota,
                    MontoCuota = ultimaCuota.MontoTotal,
                    MontoPunitorio = 0, // No hay punitorio en adelanto
                    TotalAPagar = ultimaCuota.MontoTotal,
                    MontoPagado = ultimaCuota.MontoTotal,
                    ClienteNombre = credito.ClienteNombre,
                    NumeroCreditoTexto = credito.Numero,
                    FechaVencimiento = ultimaCuota.FechaVencimiento,
                    EstaVencida = false,
                    DiasAtraso = 0,
                    FechaPago = DateTime.UtcNow
                };

                return View(modelo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar adelanto de cuota: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Credito/AdelantarCuota
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdelantarCuota(PagarCuotaViewModel modelo, string? returnUrl = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
                    return View(modelo);
                }

                var resultado = await _creditoService.AdelantarCuotaAsync(modelo);

                if (resultado)
                {
                    TempData["Success"] = $"Cuota #{modelo.NumeroCuota} adelantada exitosamente. Se ha reducido el plazo del crédito.";
                    return RedirectToReturnUrlOrDetails(returnUrl, modelo.CreditoId);
                }

                ModelState.AddModelError(string.Empty, "No se pudo registrar el adelanto");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al adelantar cuota");
                ModelState.AddModelError("", "Error al registrar el adelanto: " + ex.Message);
            }

            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
            return View(modelo);
        }

        // GET: API endpoint para evaluar crédito en tiempo real
        [HttpGet]
        public async Task<IActionResult> EvaluarCredito(int clienteId, decimal montoSolicitado, int? garanteId = null)
        {
            try
            {
                _logger.LogInformation("Evaluando crédito para cliente {ClienteId}, monto {Monto}", clienteId, montoSolicitado);

                var evaluacion = await _evaluacionService.EvaluarSolicitudAsync(clienteId, montoSolicitado, garanteId);

                return Json(evaluacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar crédito");
                return StatusCode(500, new { error = "Error al evaluar crédito: " + ex.Message });
            }
        }

        // GET: Credito/CuotasVencidas
        public async Task<IActionResult> CuotasVencidas(string? returnUrl = null)
        {
            try
            {
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                await using var context = await _contextFactory.CreateDbContextAsync();

                var cuotas = await context.Cuotas
                    .Include(c => c.Credito)
                        .ThenInclude(cr => cr.Cliente)
                    .Where(c => !c.IsDeleted
                             && !c.Credito.IsDeleted
                             && !c.Credito.Cliente.IsDeleted
                             && (c.Estado == EstadoCuota.Vencida ||
                                 (c.Estado == EstadoCuota.Pendiente && c.FechaVencimiento < DateTime.Today)))
                    .OrderBy(c => c.FechaVencimiento)
                    .ToListAsync();

                var cuotasViewModel = cuotas.Select(c => new CuotaViewModel
                {
                    Id = c.Id,
                    CreditoId = c.CreditoId,
                    CreditoNumero = c.Credito.Numero,
                    ClienteNombre = c.Credito.Cliente != null ? c.Credito.Cliente.ToDisplayName() : string.Empty,
                    NumeroCuota = c.NumeroCuota,
                    MontoCapital = c.MontoCapital,
                    MontoInteres = c.MontoInteres,
                    MontoTotal = c.MontoTotal,
                    FechaVencimiento = c.FechaVencimiento,
                    MontoPagado = c.MontoPagado,
                    MontoPunitorio = c.MontoPunitorio,
                    Estado = c.Estado,
                    MedioPago = c.MedioPago
                }).ToList();

                return View(cuotasViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cuotas vencidas");
                TempData["Error"] = "Error al cargar las cuotas vencidas";
                return View(new List<CuotaViewModel>());
            }
        }

        #region Métodos Privados

        private async Task CargarViewBags(int? clienteIdSeleccionado = null, int? garanteIdSeleccionado = null)
        {
            _logger.LogInformation("Cargando ViewBags...");

            // Usar servicio centralizado para clientes y garantes
            var clientes = await _clienteLookup.GetClientesSelectListAsync(clienteIdSeleccionado);
            ViewBag.Clientes = new SelectList(clientes, "Value", "Text", clienteIdSeleccionado?.ToString());

            var garantes = await _clienteLookup.GetClientesSelectListAsync(garanteIdSeleccionado);
            ViewBag.Garantes = new SelectList(garantes, "Value", "Text", garanteIdSeleccionado?.ToString());

            var productos = await _productoService.SearchAsync(soloActivos: true, orderBy: "nombre");
            ViewBag.Productos = new SelectList(
                productos
                    .Where(p => p.StockActual > 0)
                    .Select(p => new
                    {
                        p.Id,
                        Detalle = $"{p.Codigo} - {p.Nombre} (Stock: {p.StockActual}) - ${p.PrecioVenta:N2}"
                    }),
                "Id",
                "Detalle");
        }

        #endregion
    }
}