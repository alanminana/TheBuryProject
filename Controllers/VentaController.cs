using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize(Roles = "SuperAdmin,Gerente,Vendedor")]
    public class VentaController : Controller
    {
        private readonly IVentaService _ventaService;
        private readonly IConfiguracionPagoService _configuracionPagoService;
        private readonly ILogger<VentaController> _logger;
        private readonly IFinancialCalculationService _financialCalculationService;
        private readonly IPrequalificationService _prequalificationService;
        private readonly IDocumentoClienteService _documentoClienteService;
        private readonly ICreditoService _creditoService;
        private readonly IDocumentacionService _documentacionService;
        private readonly IClienteService _clienteService;
        private readonly IProductoService _productoService;

        public VentaController(
            IVentaService ventaService,
            IConfiguracionPagoService configuracionPagoService,
            ILogger<VentaController> logger,
            IFinancialCalculationService financialCalculationService,
            IPrequalificationService prequalificationService,
            IDocumentoClienteService documentoClienteService,
            ICreditoService creditoService,
            IDocumentacionService documentacionService,
            IClienteService clienteService,
            IProductoService productoService)
        {
            _ventaService = ventaService;
            _configuracionPagoService = configuracionPagoService;
            _logger = logger;
            _financialCalculationService = financialCalculationService;
            _prequalificationService = prequalificationService;
            _documentoClienteService = documentoClienteService;
            _creditoService = creditoService;
            _documentacionService = documentacionService;
            _clienteService = clienteService;
            _productoService = productoService;
        }

        // GET: Venta
        public async Task<IActionResult> Index(VentaFilterViewModel filter)
        {
            try
            {
                var ventas = await _ventaService.GetAllAsync(filter);

                // Cargar datos para filtros
                var clientes = await _clienteService.SearchAsync(soloActivos: true, orderBy: "apellido");

                ViewBag.Clientes = clientes
                    .Select(c => new SelectListItem
                    {
                        Value = c.NumeroDocumento,
                        Text = $"{c.Apellido}, {c.Nombre} - {c.NumeroDocumento}"
                    })
                    .ToList();

                ViewBag.Estados = new SelectList(Enum.GetValues(typeof(EstadoVenta)));
                ViewBag.TiposPago = new SelectList(Enum.GetValues(typeof(TipoPago)));
                ViewBag.EstadosAutorizacion = new SelectList(Enum.GetValues(typeof(EstadoAutorizacionVenta)));
                ViewBag.Filter = filter;

                return View(ventas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las ventas");
                TempData["Error"] = "Error al cargar las ventas";
                return View(new List<VentaViewModel>());
            }
        }

        // GET: Venta/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la venta {Id}", id);
                TempData["Error"] = "Error al cargar los detalles de la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Venta/Cotizar
        [HttpGet]


        public async Task<IActionResult> Cotizar()
        {
            await CargarViewBags();
            ViewBag.IvaRate = VentaConstants.IVA_RATE;
            return View("Create", CrearVentaInicial(EstadoVenta.Cotizacion));
        }
        // POST: Venta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VentaViewModel viewModel, string? DatosCreditoPersonalJson)
        {
            try
            {
                if (!ModelState.IsValid || !ValidarDetalles(viewModel))
                {
                    return await RetornarVistaConDatos(viewModel);
                }

                var venta = await _ventaService.CreateAsync(viewModel);

                var mensajeCreacion = venta.RequiereAutorizacion
                    ? $"Venta {venta.Numero} creada. Requiere autorización antes de confirmar."
                    : $"Venta {venta.Numero} creada exitosamente";

                if (venta.TipoPago == TipoPago.CreditoPersonal)
                {
                    var documentacion = await _documentacionService.ProcesarDocumentacionVentaAsync(venta.Id);

                    if (!documentacion.DocumentacionCompleta)
                    {
                        TempData["Warning"] =
                            $"Falta documentación obligatoria para otorgar crédito: {documentacion.MensajeFaltantes}";
                        TempData["Info"] = mensajeCreacion;

                        return RedirectToAction(
                            "Index",
                            "DocumentoCliente",
                            new { clienteId = venta.ClienteId, returnToVentaId = venta.Id });
                    }

                    TempData["Success"] = mensajeCreacion;
                    TempData["Info"] = documentacion.CreditoCreado
                        ? "Documentación completa. Crédito generado y listo para configurar."
                        : "Documentación completa. Crédito listo para configurar.";

                    return RedirectToAction(
                        "ConfigurarVenta",
                        "Credito",
                        new { id = documentacion.CreditoId, ventaId = venta.Id });
                }

                TempData[venta.RequiereAutorizacion ? "Warning" : "Success"] = mensajeCreacion;
                return RedirectToAction(nameof(Details), new { id = venta.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear venta");
                ModelState.AddModelError("", "Error al crear la venta: " + ex.Message);
                await CargarViewBags(viewModel.ClienteId);
                ViewBag.IvaRate = VentaConstants.IVA_RATE;
                return View(viewModel);
            }
        }
        // GET: Venta/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarViewBags();
            ViewBag.IvaRate = VentaConstants.IVA_RATE;
            return View(CrearVentaInicial(EstadoVenta.Presupuesto));
        }

        // GET: Venta/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (venta.Estado != EstadoVenta.Cotizacion && venta.Estado != EstadoVenta.Presupuesto)
                {
                    TempData["Error"] = "Solo se pueden editar ventas en estado Cotización o Presupuesto";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await CargarViewBags(venta.ClienteId);
                ViewBag.IvaRate = VentaConstants.IVA_RATE;
                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar venta para editar: {Id}", id);
                TempData["Error"] = "Error al cargar la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VentaViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid || !ValidarDetalles(viewModel))
                {
                    return await RetornarVistaConDatos(viewModel);
                }

                var resultado = await _ventaService.UpdateAsync(id, viewModel);

                if (resultado == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (resultado.TipoPago == TipoPago.CreditoPersonal)
                {
                    var documentacion = await _documentacionService.ProcesarDocumentacionVentaAsync(resultado.Id);

                    if (!documentacion.DocumentacionCompleta)
                    {
                        TempData["Warning"] =
                            $"Falta documentación obligatoria para otorgar crédito: {documentacion.MensajeFaltantes}";

                        return RedirectToAction(
                            "Index",
                            "DocumentoCliente",
                            new { clienteId = resultado.ClienteId, returnToVentaId = resultado.Id });
                    }

                    TempData["Success"] = "Venta actualizada. Crédito listo para configurar.";

                    return RedirectToAction(
                        "ConfigurarVenta",
                        "Credito",
                        new { id = documentacion.CreditoId, ventaId = resultado.Id });
                }

                TempData["Success"] = "Venta actualizada exitosamente";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar venta: {Id}", id);
                ModelState.AddModelError("", "Error al actualizar la venta: " + ex.Message);
                await CargarViewBags(viewModel.ClienteId);
                return View(viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CalcularFinanciamiento([FromBody] CalculoFinanciamientoViewModel request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Solicitud inválida" });
            }

            try
            {
                var montoFinanciado = _financialCalculationService.ComputeFinancedAmount(request.Total, request.Anticipo);
                var cuota = _financialCalculationService.ComputePmt(request.TasaMensual, request.Cuotas, montoFinanciado);

                var prequalification = _prequalificationService.Evaluate(
                    cuota,
                    request.IngresoNeto,
                    request.OtrasDeudas,
                    request.AntiguedadLaboralMeses);

                return Ok(new
                {
                    financedAmount = montoFinanciado,
                    installment = cuota,
                    prequalification
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ValidarDocumentacionCredito(int ventaId)
        {
            var venta = await _ventaService.GetByIdAsync(ventaId);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToAction(nameof(Index));
            }

            if (venta.TipoPago != TipoPago.CreditoPersonal)
            {
                TempData["Error"] = "La venta no utiliza crédito personal";
                return RedirectToAction(nameof(Details), new { id = ventaId });
            }

            var resultado = await _documentacionService.ProcesarDocumentacionVentaAsync(ventaId);

            if (!resultado.DocumentacionCompleta)
            {
                TempData["Warning"] =
                    $"Falta documentación obligatoria para otorgar crédito: {resultado.MensajeFaltantes}";

                return RedirectToAction(
                    "Index",
                    "DocumentoCliente",
                    new { clienteId = resultado.ClienteId, returnToVentaId = resultado.VentaId });
            }

            TempData["Success"] = resultado.CreditoCreado
                ? "Documentación validada. Crédito creado y pendiente de configuración."
                : "Documentación validada. Crédito listo para configurar.";

            return RedirectToAction("ConfigurarVenta", "Credito", new { id = resultado.CreditoId, ventaId });
        }

        // GET: Venta/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (venta.Estado != EstadoVenta.Cotizacion && venta.Estado != EstadoVenta.Presupuesto)
                {
                    TempData["Error"] = "Solo se pueden eliminar ventas en estado Cotización o Presupuesto";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar venta para eliminar: {Id}", id);
                TempData["Error"] = "Error al cargar la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _ventaService.DeleteAsync(id);
                TempData["Success"] = "Venta eliminada exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar venta: {Id}", id);
                TempData["Error"] = "Error al eliminar la venta: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Venta/Confirmar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (venta.TipoPago == TipoPago.CreditoPersonal)
                {
                    var documentacion = await _documentacionService.ProcesarDocumentacionVentaAsync(id);

                    if (!documentacion.DocumentacionCompleta)
                    {
                        TempData["Warning"] =
                            $"Falta documentación obligatoria para otorgar crédito: {documentacion.MensajeFaltantes}";

                        return RedirectToAction(
                            "Index",
                            "DocumentoCliente",
                            new { clienteId = venta.ClienteId, returnToVentaId = venta.Id });
                    }

                    return RedirectToAction(
                        "ConfigurarVenta",
                        "Credito",
                        new { id = documentacion.CreditoId, ventaId = venta.Id });
                }

                var resultado = await _ventaService.ConfirmarVentaAsync(id);
                if (resultado)
                {
                    TempData["Success"] = "Venta confirmada exitosamente. El stock ha sido descontado.";
                }
                else
                {
                    TempData["Error"] = "No se pudo confirmar la venta";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar venta: {Id}", id);
                TempData["Error"] = "Error al confirmar la venta: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Venta/Cancelar/5
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar venta para cancelar: {Id}", id);
                TempData["Error"] = "Error al cargar la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Cancelar/5
        [HttpPost, ActionName("Cancelar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarConfirmed(int id, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["Error"] = "Debe indicar el motivo de la cancelación";
                    return RedirectToAction(nameof(Cancelar), new { id });
                }

                var resultado = await _ventaService.CancelarVentaAsync(id, motivo);
                if (resultado)
                {
                    TempData["Success"] = "Venta cancelada exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo cancelar la venta";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar venta: {Id}", id);
                TempData["Error"] = "Error al cancelar la venta: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Venta/Autorizar/5
        public async Task<IActionResult> Autorizar(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (venta.EstadoAutorizacion != EstadoAutorizacionVenta.PendienteAutorizacion)
                {
                    TempData["Error"] = "La venta no está pendiente de autorización";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar venta para autorizar: {Id}", id);
                TempData["Error"] = "Error al cargar la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Autorizar/5
        [HttpPost, ActionName("Autorizar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutorizarConfirmed(int id, string motivo)
        {
            try
            {
                var usuarioAutoriza = User.Identity?.Name ?? "Administrador";

                var resultado = await _ventaService.AutorizarVentaAsync(id, usuarioAutoriza, motivo);
                if (resultado)
                {
                    TempData["Success"] = "Venta autorizada exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo autorizar la venta";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al autorizar venta: {Id}", id);
                TempData["Error"] = "Error al autorizar la venta: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Venta/Rechazar/5
        public async Task<IActionResult> Rechazar(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (venta.EstadoAutorizacion != EstadoAutorizacionVenta.PendienteAutorizacion)
                {
                    TempData["Error"] = "La venta no está pendiente de autorización";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar venta para rechazar: {Id}", id);
                TempData["Error"] = "Error al cargar la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Rechazar/5
        [HttpPost, ActionName("Rechazar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarConfirmed(int id, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["Error"] = "Debe indicar el motivo del rechazo";
                    return RedirectToAction(nameof(Rechazar), new { id });
                }

                var usuarioAutoriza = User.Identity?.Name ?? "Administrador";

                var resultado = await _ventaService.RechazarVentaAsync(id, usuarioAutoriza, motivo);
                if (resultado)
                {
                    TempData["Success"] = "Venta rechazada";
                }
                else
                {
                    TempData["Error"] = "No se pudo rechazar la venta";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar venta: {Id}", id);
                TempData["Error"] = "Error al rechazar la venta: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        // GET: Venta/Facturar/5
        public async Task<IActionResult> Facturar(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (venta.Estado != EstadoVenta.Confirmada)
                {
                    TempData["Error"] = "Solo se pueden facturar ventas confirmadas";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (venta.RequiereAutorizacion && venta.EstadoAutorizacion != EstadoAutorizacionVenta.Autorizada)
                {
                    TempData["Error"] = "La venta requiere autorización antes de ser facturada";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var facturaViewModel = new FacturaViewModel
                {
                    VentaId = venta.Id,
                    FechaEmision = DateTime.Today,
                    Tipo = TipoFactura.B,
                    Subtotal = venta.Subtotal,
                    IVA = venta.IVA,
                    Total = venta.Total
                };

                ViewBag.Venta = venta;
                ViewBag.TiposFactura = new SelectList(Enum.GetValues(typeof(TipoFactura)));

                return View(facturaViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de facturación: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Facturar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Facturar(FacturaViewModel facturaViewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var venta = await _ventaService.GetByIdAsync(facturaViewModel.VentaId);
                    ViewBag.Venta = venta;
                    ViewBag.TiposFactura = new SelectList(Enum.GetValues(typeof(TipoFactura)));
                    return View(facturaViewModel);
                }

                var resultado = await _ventaService.FacturarVentaAsync(facturaViewModel.VentaId, facturaViewModel);
                if (resultado)
                {
                    TempData["Success"] = "Factura generada exitosamente";
                    return RedirectToAction(nameof(Details), new { id = facturaViewModel.VentaId });
                }
                else
                {
                    TempData["Error"] = "No se pudo generar la factura";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al facturar venta");
                ModelState.AddModelError("", "Error al generar la factura: " + ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: API endpoint para obtener tarjetas activas
        [HttpGet]
        public async Task<IActionResult> GetTarjetasActivas()
        {
            try
            {
                var tarjetas = await _configuracionPagoService.GetTarjetasActivasAsync();

                var resultado = tarjetas.Select(t => new
                {
                    id = t.Id,
                    nombre = t.NombreTarjeta,
                    tipo = t.TipoTarjeta,
                    permiteCuotas = t.PermiteCuotas,
                    cantidadMaximaCuotas = t.CantidadMaximaCuotas,
                    tipoCuota = t.TipoCuota,
                    tasaInteres = t.TasaInteresesMensual,
                    tieneRecargo = t.TieneRecargoDebito,
                    porcentajeRecargo = t.PorcentajeRecargoDebito
                });

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tarjetas activas");
                return StatusCode(500, "Error al obtener las tarjetas");
            }
        }

        #region Métodos Privados

        private async Task CargarViewBags(int? clienteIdSeleccionado = null)
        {
            var clientes = await _clienteService.SearchAsync(soloActivos: true, orderBy: "apellido");

            ViewBag.Clientes = new SelectList(
                clientes.Select(c => new
                {
                    c.Id,
                    NombreCompleto = $"{c.Apellido}, {c.Nombre} - DNI: {c.NumeroDocumento}"
                }),
                "Id",
                "NombreCompleto",
                clienteIdSeleccionado);

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
            ViewBag.TiposPago = new SelectList(Enum.GetValues(typeof(TipoPago)));

            // Cargar créditos disponibles del cliente si hay uno seleccionado
            if (clienteIdSeleccionado.HasValue)
            {
                var creditosDisponibles = (await _creditoService.GetByClienteIdAsync(clienteIdSeleccionado.Value))
                    .Where(c => (c.Estado == EstadoCredito.Activo || c.Estado == EstadoCredito.Aprobado)
                                && c.SaldoPendiente > 0)
                    .OrderByDescending(c => c.FechaAprobacion ?? DateTime.MinValue)
                    .Select(c => new
                    {
                        c.Id,
                        Detalle = $"{c.Numero} - Saldo: ${c.SaldoPendiente:N2}"
                    });

                ViewBag.Creditos = new SelectList(creditosDisponibles, "Id", "Detalle");
            }
            else
            {
                ViewBag.Creditos = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            // Cargar tarjetas activas
            var tarjetas = await _configuracionPagoService.GetTarjetasActivasAsync();
            ViewBag.Tarjetas = new SelectList(
                tarjetas.Select(t => new
                {
                    t.Id,
                    Detalle = $"{t.NombreTarjeta} ({t.TipoTarjeta})"
                }),
                "Id",
                "Detalle"
            );
        }

        #endregion
        // GET: API endpoint para calcular crédito personal
        [HttpGet]
        public async Task<IActionResult> CalcularCreditoPersonal(int creditoId, decimal monto, int cuotas, string fechaPrimeraCuota)
        {
            try
            {
                if (!DateTime.TryParse(fechaPrimeraCuota, out DateTime fecha))
                    fecha = DateTime.Today.AddMonths(1);

                var resultado = await _ventaService.CalcularCreditoPersonalAsync(creditoId, monto, cuotas, fecha);

                return Json(new
                {
                    creditoNumero = resultado.CreditoNumero,
                    creditoTotalAsignado = resultado.CreditoTotalAsignado,
                    creditoDisponible = resultado.CreditoDisponible,
                    montoAFinanciar = resultado.MontoAFinanciar,
                    cantidadCuotas = resultado.CantidadCuotas,
                    montoCuota = resultado.MontoCuota,
                    tasaInteres = resultado.TasaInteresMensual,
                    totalAPagar = resultado.TotalAPagar,
                    interesTotal = resultado.InteresTotal,
                    saldoRestante = resultado.SaldoRestante,
                    cuotas = resultado.Cuotas.Select(c => new
                    {
                        numeroCuota = c.NumeroCuota,
                        fechaVencimiento = c.FechaVencimiento.ToString("dd/MM/yyyy"),
                        monto = c.Monto,
                        saldo = c.Saldo
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular crédito personal");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: API endpoint para validar disponibilidad de crédito
        [HttpGet]
        public async Task<IActionResult> ValidarCreditoDisponible(int creditoId, decimal monto)
        {
            try
            {
                var disponible = await _ventaService.ValidarDisponibilidadCreditoAsync(creditoId, monto);

                return Json(new
                {
                    disponible = disponible,
                    mensaje = disponible
                        ? "Crédito suficiente"
                        : "Crédito insuficiente para este monto"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar crédito");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private VentaViewModel CrearVentaInicial(EstadoVenta estadoInicial)
        {
            return new VentaViewModel
            {
                FechaVenta = DateTime.Today,
                Estado = estadoInicial,
                TipoPago = TipoPago.Efectivo
            };
        }

        private bool ValidarDetalles(VentaViewModel viewModel)
        {
            if (viewModel.Detalles != null && viewModel.Detalles.Any())
            {
                return true;
            }

            ModelState.AddModelError("", "Debe agregar al menos un producto a la venta");
            return false;
        }

        private async Task<IActionResult> RetornarVistaConDatos(VentaViewModel viewModel)
        {
            await CargarViewBags(viewModel.ClienteId);
            ViewBag.IvaRate = VentaConstants.IVA_RATE;
            return View(viewModel);
        }
    }
}
