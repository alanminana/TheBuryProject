using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [AllowAnonymous]
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

        // GET: Credito/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
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

        [HttpGet]
        public async Task<IActionResult> ConfigurarVenta(int id, int? ventaId)
        {
            var credito = await _creditoService.GetByIdAsync(id);
            if (credito == null)
            {
                TempData["Error"] = "Crédito no encontrado";
                return RedirectToAction(nameof(Index));
            }

            decimal montoVenta = credito.MontoAprobado;

            if (montoVenta <= 0)
                montoVenta = credito.MontoSolicitado;

            if (ventaId.HasValue)
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var venta = await context.Ventas
                    .Include(v => v.Detalles)
                    .FirstOrDefaultAsync(v => v.Id == ventaId.Value);

                if (venta != null)
                {
                    // Prioriza el total guardado; si no existe, recalcula desde el detalle
                    montoVenta = venta.Total;

                    if (montoVenta <= 0 && venta.Detalles != null && venta.Detalles.Any())
                    {
                        var subtotal = venta.Detalles.Sum(d =>
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
        public async Task<IActionResult> ConfigurarVenta(ConfiguracionCreditoVentaViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            var credito = await _creditoService.GetByIdAsync(modelo.CreditoId);
            if (credito == null)
            {
                TempData["Error"] = "Crédito no encontrado";
                return RedirectToAction(nameof(Index));
            }

            credito.CantidadCuotas = modelo.CantidadCuotas;
            credito.TasaInteres = modelo.TasaMensual;
            credito.FechaPrimeraCuota = modelo.FechaPrimeraCuota;
            credito.MontoAprobado = Math.Max(0, modelo.Monto - modelo.Anticipo);
            credito.MontoSolicitado = credito.MontoAprobado;
            credito.SaldoPendiente = credito.MontoAprobado;
            credito.Estado = EstadoCredito.Solicitado;

            if (modelo.GastosAdministrativos > 0)
            {
                credito.Observaciones = string.IsNullOrWhiteSpace(credito.Observaciones)
                    ? $"Gastos administrativos declarados: ${modelo.GastosAdministrativos:N2}"
                    : $"{credito.Observaciones} | Gastos administrativos: ${modelo.GastosAdministrativos:N2}";
            }

            await _creditoService.UpdateAsync(credito);

            TempData["Success"] = "Crédito configurado y listo para aprobación.";
            return RedirectToAction(nameof(Details), new { id = credito.Id });
        }

        [HttpGet]
        public IActionResult SimularPlanVenta(
            decimal totalVenta,
            decimal anticipo,
            int cuotas,
            decimal tasaMensual,
            decimal gastosAdministrativos,
            string? fechaPrimeraCuota)
        {
            try
            {
                if (totalVenta <= 0)
                    return BadRequest(new { error = "El monto total de la venta debe ser mayor a cero." });

                if (anticipo < 0)
                    return BadRequest(new { error = "El anticipo no puede ser negativo." });

                if (cuotas <= 0)
                    return BadRequest(new { error = "Ingresá una cantidad de cuotas mayor a cero." });

                if (tasaMensual < 0)
                    return BadRequest(new { error = "La tasa mensual no puede ser negativa." });

                if (gastosAdministrativos < 0)
                    return BadRequest(new { error = "Los gastos administrativos no pueden ser negativos." });

                var fecha = DateTime.TryParse(fechaPrimeraCuota, out var parsed) ? parsed : DateTime.Today.AddMonths(1);

                var montoFinanciado = _financialService.ComputeFinancedAmount(totalVenta, anticipo);
                var tasaDecimal = tasaMensual / 100;
                var cuota = _financialService.ComputePmt(tasaDecimal, cuotas, montoFinanciado);
                var interesTotal = _financialService.CalcularInteresTotal(montoFinanciado, tasaDecimal, cuotas);
                var totalCuotas = cuota * cuotas;
                var totalPlan = totalCuotas + gastosAdministrativos;

                var semaforo = CalcularSemaforo(cuota, montoFinanciado);

                return Json(new
                {
                    montoFinanciado,
                    cuotaEstimada = cuota,
                    tasaAplicada = tasaMensual,
                    interesTotal,
                    totalAPagar = totalCuotas,
                    gastosAdministrativos,
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
        public async Task<IActionResult> Create()
        {
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
        public async Task<IActionResult> Create(CreditoViewModel viewModel)
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
                    await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                    return View(viewModel);
                }

                var credito = await _creditoService.CreateAsync(viewModel);

                TempData["Success"] = $"Línea de Crédito {credito.Numero} creada exitosamente";
                return RedirectToAction(nameof(Details), new { id = credito.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear línea de crédito");
                ModelState.AddModelError("", "Error al crear la línea de crédito: " + ex.Message);
                await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                return View(viewModel);
            }
        }

        // GET: Credito/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (credito.Estado != EstadoCredito.Solicitado)
                {
                    TempData["Error"] = "Solo se pueden editar créditos en estado Solicitado";
                    return RedirectToAction(nameof(Details), new { id });
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
        public async Task<IActionResult> Edit(int id, CreditoViewModel viewModel)
        {
            if (id != viewModel.Id)
                return RedirectToAction(nameof(Index));

            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                    return View(viewModel);
                }

                var resultado = await _creditoService.UpdateAsync(viewModel);
                if (resultado)
                {
                    TempData["Success"] = "Crédito actualizado exitosamente";
                    return RedirectToAction(nameof(Details), new { id });
                }

                TempData["Error"] = "No se pudo actualizar el crédito";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar crédito: {Id}", id);
                ModelState.AddModelError("", "Error al actualizar el crédito: " + ex.Message);
                await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                return View(viewModel);
            }
        }

        // GET: Credito/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
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
        public async Task<IActionResult> DeleteConfirmed(int id)
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

            return RedirectToAction(nameof(Index));
        }

        // GET: Credito/PagarCuota/5
        public async Task<IActionResult> PagarCuota(int id)
        {
            try
            {
                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var cuotasPendientes = (credito.Cuotas ?? new List<CuotaViewModel>())
                    .Where(c => c.Estado == EstadoCuota.Pendiente || c.Estado == EstadoCuota.Vencida)
                    .OrderBy(c => c.NumeroCuota)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = $"Cuota #{c.NumeroCuota} - Vto: {c.FechaVencimiento:dd/MM/yyyy} - {c.MontoTotal:C}"
                    })
                    .ToList();

                ViewBag.Cuotas = cuotasPendientes;

                var modelo = new PagarCuotaViewModel
                {
                    CreditoId = credito.Id,
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
        public async Task<IActionResult> PagarCuota(PagarCuotaViewModel modelo)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var credito = await _creditoService.GetByIdAsync(modelo.CreditoId);
                    if (credito == null)
                    {
                        TempData["Error"] = "Crédito no encontrado";
                        return RedirectToAction(nameof(Index));
                    }

                    var cuotasPendientes = (credito.Cuotas ?? new List<CuotaViewModel>())
                        .Where(c => c.Estado == EstadoCuota.Pendiente || c.Estado == EstadoCuota.Vencida)
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
                    return RedirectToAction(nameof(Details), new { id = modelo.CreditoId });
                }

                TempData["Error"] = "No se pudo registrar el pago";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al pagar cuota");
                ModelState.AddModelError("", "Error al registrar el pago: " + ex.Message);
            }

            return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> CuotasVencidas()
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var cuotas = await context.Cuotas
                    .Include(c => c.Credito)
                        .ThenInclude(cr => cr.Cliente)
                    .Where(c => c.Estado == EstadoCuota.Vencida ||
                               (c.Estado == EstadoCuota.Pendiente && c.FechaVencimiento < DateTime.Today))
                    .OrderBy(c => c.FechaVencimiento)
                    .ToListAsync();

                var cuotasViewModel = cuotas.Select(c => new CuotaViewModel
                {
                    Id = c.Id,
                    CreditoId = c.CreditoId,
                    CreditoNumero = c.Credito.Numero,
                    ClienteNombre = $"{c.Credito.Cliente.Apellido}, {c.Credito.Cliente.Nombre}",
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