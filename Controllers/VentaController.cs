using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TheBuryProject.Data;
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
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<VentaController> _logger;
        private readonly IFinancialCalculationService _financialCalculationService;
        private readonly IPrequalificationService _prequalificationService;

        public VentaController(
            IVentaService ventaService,
            IConfiguracionPagoService configuracionPagoService,
            AppDbContext context,
            IMapper mapper,
            ILogger<VentaController> logger,
            IFinancialCalculationService financialCalculationService,
            IPrequalificationService prequalificationService)
        {
            _ventaService = ventaService;
            _configuracionPagoService = configuracionPagoService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _financialCalculationService = financialCalculationService;
            _prequalificationService = prequalificationService;
        }

        // GET: Venta
        public async Task<IActionResult> Index(VentaFilterViewModel filter)
        {
            try
            {
                var ventas = await _ventaService.GetAllAsync(filter);

                // Cargar datos para filtros
                ViewBag.Clientes = await _context.Clientes
                    .Where(c => c.Activo)
                    .OrderBy(c => c.Apellido)
                    .Select(c => new SelectListItem
                    {
                        Value = c.NumeroDocumento,
                        Text = $"{c.Apellido}, {c.Nombre} - {c.NumeroDocumento}"
                    })
                    .ToListAsync();

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

                if (!TryAsignarDatosCreditoPersonal(DatosCreditoPersonalJson, viewModel))
                {
                    return await RetornarVistaConDatos(viewModel);
                }

                if (!await ValidarCreditoPersonalAsync(viewModel))
                {
                    return await RetornarVistaConDatos(viewModel);
                }

                var venta = await _ventaService.CreateAsync(viewModel);

                if (venta.RequiereAutorizacion)
                {
                    TempData["Warning"] = $"Venta {venta.Numero} creada. Requiere autorización antes de confirmar.";
                }
                else
                {
                    TempData["Success"] = $"Venta {venta.Numero} creada exitosamente";
                }

                return RedirectToAction(nameof(Details), new { id = venta.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear venta");
                ModelState.AddModelError("", "Error al crear la venta: " + ex.Message);
                await CargarViewBags(viewModel.ClienteId);
                return View(viewModel);
            }
        }
        // GET: Venta/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarViewBags();
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

        // GET: API endpoint para obtener precio del producto
        [HttpGet]
        public async Task<IActionResult> GetProductoPrecio(int productoId)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(productoId);
                if (producto == null)
                    return NotFound();

                return Json(new
                {
                    precioVenta = producto.PrecioVenta,
                    stockActual = producto.StockActual,
                    codigo = producto.Codigo,
                    nombre = producto.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener precio del producto: {ProductoId}", productoId);
                return StatusCode(500, "Error al obtener el precio del producto");
            }
        }

        public async Task<IActionResult> GetCreditosCliente(int clienteId)
        {
            try
            {
                var creditos = await _context.Creditos
                    .Where(c => c.ClienteId == clienteId
                             && (c.Estado == EstadoCredito.Activo || c.Estado == EstadoCredito.Aprobado)
                             && c.SaldoPendiente > 0)
                    .OrderByDescending(c => c.FechaAprobacion)
                    .Select(c => new
                    {
                        id = c.Id,
                        numero = c.Numero,
                        montoAprobado = c.MontoAprobado,
                        saldoPendiente = c.SaldoPendiente,
                        tasaInteres = c.TasaInteres,
                        detalle = $"{c.Numero} - Saldo disponible: ${c.SaldoPendiente:N2}"
                    })
                    .ToListAsync();

                if (!creditos.Any())
                {
                    _logger.LogWarning("No se encontraron créditos disponibles para cliente {ClienteId}", clienteId);
                }

                return Json(creditos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener créditos del cliente: {ClienteId}", clienteId);
                return StatusCode(500, "Error al obtener los créditos del cliente");
            }
        }
        // GET: API endpoint para obtener información completa de un crédito
        [HttpGet]
        public async Task<IActionResult> GetInfoCredito(int creditoId)
        {
            try
            {
                _logger.LogInformation("Obteniendo información del crédito {CreditoId}", creditoId);

                var credito = await _context.Creditos
                    .Where(c => c.Id == creditoId
                             && (c.Estado == EstadoCredito.Activo || c.Estado == EstadoCredito.Aprobado))
                    .Select(c => new
                    {
                        id = c.Id,
                        numero = c.Numero,
                        montoAprobado = c.MontoAprobado,
                        saldoPendiente = c.SaldoPendiente,
                        tasaInteres = c.TasaInteres,
                        estado = c.Estado
                    })
                    .FirstOrDefaultAsync();

                if (credito == null)
                {
                    _logger.LogWarning("Crédito {CreditoId} no encontrado o no está en estado Activo/Aprobado", creditoId);

                    // Verificar si el crédito existe pero en otro estado
                    var creditoExiste = await _context.Creditos
                        .Where(c => c.Id == creditoId)
                        .Select(c => new { c.Numero, c.Estado })
                        .FirstOrDefaultAsync();

                    if (creditoExiste != null)
                    {
                        _logger.LogWarning("El crédito {Numero} existe pero está en estado {Estado}",
                            creditoExiste.Numero, creditoExiste.Estado);
                        return NotFound(new { error = $"El crédito está en estado {creditoExiste.Estado} y no se puede usar" });
                    }

                    return NotFound(new { error = "Crédito no encontrado" });
                }

                _logger.LogInformation("Crédito {CreditoId} encontrado: {Numero}, Estado: {Estado}, Saldo: {Saldo}",
                    creditoId, credito.numero, credito.estado, credito.saldoPendiente);

                return Json(credito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información del crédito: {CreditoId}", creditoId);
                return StatusCode(500, new { error = "Error al obtener información del crédito" });
            }
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

        // GET: API endpoint para calcular cuotas de tarjeta
        [HttpGet]
        public async Task<IActionResult> CalcularCuotasTarjeta(int tarjetaId, decimal monto, int cuotas)
        {
            try
            {
                var resultado = await _ventaService.CalcularCuotasTarjetaAsync(tarjetaId, monto, cuotas);

                return Json(new
                {
                    montoCuota = resultado.MontoCuota,
                    montoTotal = resultado.MontoTotalConInteres,
                    interes = resultado.MontoTotalConInteres - monto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular cuotas de tarjeta");
                return StatusCode(500, "Error al calcular las cuotas");
            }
        }

        #region Métodos Privados

        private async Task CargarViewBags(int? clienteIdSeleccionado = null)
        {
            var clientes = await _context.Clientes
                .Where(c => c.Activo)
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    NombreCompleto = $"{c.Apellido}, {c.Nombre} - DNI: {c.NumeroDocumento}"
                })
                .ToListAsync();

            ViewBag.Clientes = new SelectList(clientes, "Id", "NombreCompleto", clienteIdSeleccionado);

            var productos = await _context.Productos
                .Where(p => p.Activo && p.StockActual > 0)
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    p.Id,
                    Detalle = $"{p.Codigo} - {p.Nombre} (Stock: {p.StockActual}) - ${p.PrecioVenta:N2}"
                })
                .ToListAsync();

            ViewBag.Productos = new SelectList(productos, "Id", "Detalle");
            ViewBag.TiposPago = new SelectList(Enum.GetValues(typeof(TipoPago)));

            // Cargar créditos disponibles del cliente si hay uno seleccionado
            if (clienteIdSeleccionado.HasValue)
            {
                var creditosDisponibles = await _context.Creditos
                    .Where(c => c.ClienteId == clienteIdSeleccionado.Value
                             && c.Estado == EstadoCredito.Activo
                             && c.SaldoPendiente > 0)
                    .OrderByDescending(c => c.FechaAprobacion)
                    .Select(c => new
                    {
                        c.Id,
                        Detalle = $"{c.Numero} - Saldo: ${c.SaldoPendiente:N2}"
                    })
                    .ToListAsync();

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

        private bool TryAsignarDatosCreditoPersonal(string? datosCreditoJson, VentaViewModel viewModel)
        {
            if (viewModel.TipoPago != TipoPago.CreditoPersonal || string.IsNullOrEmpty(datosCreditoJson))
            {
                return true;
            }

            try
            {
                var datosCredito = JsonSerializer.Deserialize<DatosCreditoPersonalViewModel>(
                    datosCreditoJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (datosCredito != null)
                {
                    viewModel.DatosCreditoPersonal = datosCredito;
                    viewModel.CreditoId = datosCredito.CreditoId;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al parsear datos de crédito personal");
                ModelState.AddModelError("", "Error al procesar los datos de crédito personal");
                return false;
            }
        }

        private async Task<bool> ValidarCreditoPersonalAsync(VentaViewModel viewModel)
        {
            if (viewModel.TipoPago != TipoPago.CreditoPersonal)
            {
                return true;
            }

            if (!viewModel.CreditoId.HasValue)
            {
                ModelState.AddModelError("", "Debe seleccionar un crédito personal");
                return false;
            }

            if (viewModel.DatosCreditoPersonal == null || !viewModel.DatosCreditoPersonal.Cuotas.Any())
            {
                ModelState.AddModelError("", "Debe calcular el plan de financiamiento antes de guardar");
                return false;
            }

            var disponible = await _ventaService.ValidarDisponibilidadCreditoAsync(
                viewModel.CreditoId.Value,
                viewModel.DatosCreditoPersonal.MontoAFinanciar);

            if (disponible)
            {
                return true;
            }

            ModelState.AddModelError("", "El monto a financiar supera el crédito disponible");
            return false;
        }

        private async Task<IActionResult> RetornarVistaConDatos(VentaViewModel viewModel)
        {
            await CargarViewBags(viewModel.ClienteId);
            return View(viewModel);
        }
    }
}
