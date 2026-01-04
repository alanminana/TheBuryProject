using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TheBuryProject.Data;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;
using TheBuryProject.ViewModels.Requests;

namespace TheBuryProject.Controllers
{
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente + "," + Roles.Vendedor)]
    public class ClienteController : Controller
    {
        private readonly IClienteService _clienteService;
        private readonly IDocumentoClienteService _documentoService;
        private readonly ICreditoService _creditoService;
        private readonly IEvaluacionCreditoService _evaluacionService;
        private readonly IClienteAptitudService _aptitudService;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly IFinancialCalculationService _financialService;
        private readonly IMapper _mapper;
        private readonly ILogger<ClienteController> _logger;
        private readonly IClienteLookupService _clienteLookup;

        private string? GetSafeReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null;
        }

        private IActionResult RedirectToReturnUrlOrIndex(string? returnUrl)
        {
            var safeReturnUrl = GetSafeReturnUrl(returnUrl);
            return safeReturnUrl != null
                ? LocalRedirect(safeReturnUrl)
                : RedirectToAction(nameof(Index));
        }

        public ClienteController(
            IClienteService clienteService,
            IDocumentoClienteService documentoService,
            ICreditoService creditoService,
            IEvaluacionCreditoService evaluacionService,
            IClienteAptitudService aptitudService,
            IDbContextFactory<AppDbContext> contextFactory,
            IFinancialCalculationService financialService,
            IMapper mapper,
            ILogger<ClienteController> logger,
            IClienteLookupService clienteLookup)
        {
            _clienteService = clienteService;
            _documentoService = documentoService;
            _creditoService = creditoService;
            _evaluacionService = evaluacionService;
            _aptitudService = aptitudService;
            _contextFactory = contextFactory;
            _financialService = financialService;
            _mapper = mapper;
            _logger = logger;
            _clienteLookup = clienteLookup;
        }

        public async Task<IActionResult> Index(ClienteFilterViewModel filter, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                var clientes = await _clienteService.SearchAsync(
                    searchTerm: filter.SearchTerm,
                    tipoDocumento: filter.TipoDocumento,
                    soloActivos: filter.SoloActivos,
                    conCreditosActivos: filter.ConCreditosActivos,
                    puntajeMinimo: filter.PuntajeMinimo,
                    orderBy: filter.OrderBy,
                    orderDirection: filter.OrderDirection);

                // FIX punto 4.2: no recalcular Edad acá (AutoMapperProfile ya la calcula).
                var viewModels = _mapper.Map<List<ClienteViewModel>>(clientes);

                filter.Clientes = viewModels;
                filter.TotalResultados = viewModels.Count;

                CargarDropdowns();
                return View(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener clientes");
                TempData["Error"] = "Error al cargar los clientes";
                return View(new ClienteFilterViewModel());
            }
        }

        public async Task<IActionResult> Details(int id, string? tab, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                var cliente = await _clienteService.GetByIdAsync(id);
                if (!ClienteValidationHelper.ClienteExiste(cliente))
                    return RedirectToReturnUrlOrIndex(returnUrl);

                var detalleViewModel = await ConstructDetalleViewModel(cliente!, tab);
                return View(detalleViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cliente {Id}", id);
                TempData["Error"] = "Error al cargar el cliente";
                return RedirectToReturnUrlOrIndex(returnUrl);
            }
        }

        public IActionResult Create(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
            CargarDropdowns();
            return View(new ClienteViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClienteViewModel viewModel, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                if (!ModelState.IsValid)
                {
                    CargarDropdowns();
                    return View(viewModel);
                }

                var cliente = _mapper.Map<Cliente>(viewModel);
                await _clienteService.CreateAsync(cliente);

                TempData["Success"] = $"Cliente {cliente.NombreCompleto} creado exitosamente";
                return RedirectToAction(nameof(Details), new { id = cliente.Id, returnUrl = GetSafeReturnUrl(returnUrl) });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                CargarDropdowns();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cliente");
                ModelState.AddModelError("", "Error al crear el cliente");
                CargarDropdowns();
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                var cliente = await _clienteService.GetByIdAsync(id);
                if (!ClienteValidationHelper.ClienteExiste(cliente))
                    return RedirectToReturnUrlOrIndex(returnUrl);

                var viewModel = _mapper.Map<ClienteViewModel>(cliente!);
                CargarDropdowns();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cliente {Id} para edición", id);
                TempData["Error"] = "Error al cargar el cliente";
                return RedirectToReturnUrlOrIndex(returnUrl);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClienteViewModel viewModel, string? returnUrl = null)
        {
            if (id != viewModel.Id)
                return NotFound();

            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                if (!ModelState.IsValid)
                {
                    CargarDropdowns();
                    return View(viewModel);
                }

                var cliente = _mapper.Map<Cliente>(viewModel);
                await _clienteService.UpdateAsync(cliente);

                TempData["Success"] = "Cliente actualizado exitosamente";
                return RedirectToAction(nameof(Details), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                CargarDropdowns();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cliente {Id}", id);
                ModelState.AddModelError("", "Error al actualizar el cliente");
                CargarDropdowns();
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);

                var cliente = await _clienteService.GetByIdAsync(id);
                if (!ClienteValidationHelper.ClienteExiste(cliente))
                    return RedirectToReturnUrlOrIndex(returnUrl);

                var viewModel = _mapper.Map<ClienteViewModel>(cliente!);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cliente {Id} para eliminación", id);
                TempData["Error"] = "Error al cargar el cliente";
                return RedirectToReturnUrlOrIndex(returnUrl);
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl = null)
        {
            try
            {
                await _clienteService.DeleteAsync(id);
                TempData["Success"] = "Cliente eliminado exitosamente";
                return RedirectToReturnUrlOrIndex(returnUrl);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Delete), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cliente {Id}", id);
                TempData["Error"] = "Error al eliminar el cliente";
                return RedirectToAction(nameof(Delete), new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
            }
        }

        /// <summary>
        /// Asigna o actualiza el límite de crédito (cupo) de un cliente.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
        public async Task<IActionResult> AsignarLimiteCredito(int clienteId, decimal limiteCredito, string? motivo = null, string? returnUrl = null)
        {
            try
            {
                if (limiteCredito < 0)
                {
                    TempData["Error"] = "El límite de crédito no puede ser negativo";
                    return RedirectToAction(nameof(Details), new { id = clienteId, returnUrl = GetSafeReturnUrl(returnUrl) });
                }

                var exito = await _aptitudService.AsignarLimiteCreditoAsync(clienteId, limiteCredito, motivo);

                if (exito)
                {
                    // Re-evaluar aptitud después de asignar cupo
                    await _aptitudService.EvaluarAptitudAsync(clienteId, guardarResultado: true);
                    TempData["Success"] = $"Límite de crédito actualizado a {limiteCredito:C0}";
                }
                else
                {
                    TempData["Error"] = "Error al actualizar el límite de crédito";
                }

                return RedirectToAction(nameof(Details), new { id = clienteId, returnUrl = GetSafeReturnUrl(returnUrl) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar límite de crédito al cliente {ClienteId}", clienteId);
                TempData["Error"] = "Error al actualizar el límite de crédito";
                return RedirectToAction(nameof(Details), new { id = clienteId, returnUrl = GetSafeReturnUrl(returnUrl) });
            }
        }

        /// <summary>
        /// Recalcula la aptitud crediticia del cliente.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalcularAptitud(int clienteId, string? returnUrl = null)
        {
            try
            {
                await _aptitudService.EvaluarAptitudAsync(clienteId, guardarResultado: true);
                TempData["Success"] = "Aptitud crediticia recalculada";
                return RedirectToAction(nameof(Details), new { id = clienteId, returnUrl = GetSafeReturnUrl(returnUrl) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recalcular aptitud del cliente {ClienteId}", clienteId);
                TempData["Error"] = "Error al recalcular aptitud";
                return RedirectToAction(nameof(Details), new { id = clienteId, returnUrl = GetSafeReturnUrl(returnUrl) });
            }
        }

        /// <summary>
        /// [OBSOLETO] Este action ya no se usa en el flujo actual.
        /// Los créditos ahora se generan automáticamente desde ventas con TipoPago = CreditoPersonal.
        /// Mantenido temporalmente por si hay referencias externas pendientes de migrar.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Obsolete("Los créditos se generan automáticamente desde ventas. Este action será removido en futuras versiones.")]
        public async Task<IActionResult> SolicitarCredito(SolicitudCreditoViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor complete todos los campos requeridos";
                return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "evaluacion", returnUrl = GetSafeReturnUrl(returnUrl) });
            }

            var calculos = CalcularParametrosCredito(model.MontoSolicitado, model.TasaInteres, model.CantidadCuotas);

            const int maxIntentos = 3;

            for (var intento = 1; intento <= maxIntentos; intento++)
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var cliente = await context.Clientes
                    .FirstOrDefaultAsync(c => c.Id == model.ClienteId && !c.IsDeleted);
                if (!ClienteValidationHelper.ClienteExiste(cliente))
                    return RedirectToAction(nameof(Index));

                var evaluacionActual = await ObtenerEvaluacionActualAsync(cliente!);
                var requiereGarante = evaluacionActual.RequiereGarante;
                var puntajeRiesgoInicial = CalcularPuntajeRiesgoInicial(evaluacionActual);

                var noAportoGarante = !model.GaranteId.HasValue && string.IsNullOrWhiteSpace(model.GaranteDocumento);
                if (requiereGarante && noAportoGarante)
                {
                    TempData["Error"] = "La evaluación crediticia requiere un garante para continuar.";
                    return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "evaluacion" });
                }

                await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    int? garanteId = await ProcesarGaranteAsync(context, cliente!, model);

                    var numeroCredito = await GenerarNumeroCreditoAsync(context, cliente!.NumeroDocumento);

                    var credito = CrearCredito(
                        model,
                        cliente!,
                        garanteId,
                        calculos,
                        numeroCredito,
                        requiereGarante,
                        puntajeRiesgoInicial);

                    context.Creditos.Add(credito);
                    await context.SaveChangesAsync();

                    await GenerarCuotasAsync(context, credito, model, calculos.TasaMensualDecimal);

                    await tx.CommitAsync();

                    TempData["Success"] =
                        $"Crédito {numeroCredito} solicitado exitosamente. " +
                        $"Monto: {model.MontoSolicitado:C}, {model.CantidadCuotas} cuotas de {calculos.CuotaMensual:C}";

                    return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "creditos" });
                }
                catch (DbUpdateException ex) when (intento < maxIntentos && EsPosibleColisionNumeroCredito(ex))
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning(ex, "Posible colisión de NumeroCredito. Reintentando intento {Intento}/{Max}.", intento, maxIntentos);
                    continue;
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error al solicitar crédito para cliente {ClienteId}", model.ClienteId);
                    TempData["Error"] = $"Error al solicitar el crédito: {ex.Message}";
                    return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "evaluacion" });
                }
            }

            TempData["Error"] = "No se pudo generar un número de crédito único (reintentos agotados).";
            return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "evaluacion" });
        }

        /// <summary>
        /// [OBSOLETO] Usado por el formulario de solicitar crédito que ya no existe.
        /// Los créditos ahora se generan automáticamente desde ventas.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Obsolete("Los créditos se generan automáticamente desde ventas. Este action será removido en futuras versiones.")]
        public IActionResult CalcularCreditoPreview([FromBody] CalcularCreditoPreviewRequest request)
        {
            try
            {
                if (request == null || request.MontoSolicitado <= 0 || request.CantidadCuotas < 1)
                    return BadRequest(new { error = "Los datos de cálculo no son válidos" });

                var calculos = CalcularParametrosCredito(request.MontoSolicitado, request.TasaInteres, request.CantidadCuotas);

                var excedeCapacidad = request.CapacidadPagoMensual.HasValue &&
                                      calculos.CuotaMensual > request.CapacidadPagoMensual.Value;

                return Json(new
                {
                    cuotaMensual = calculos.CuotaMensual,
                    montoTotal = calculos.TotalAPagar,
                    superaCapacidadPago = excedeCapacidad
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular preview de crédito para monto {Monto} y {Cuotas} cuotas", request?.MontoSolicitado, request?.CantidadCuotas);
                return StatusCode(500, new { error = "No se pudo calcular la cuota" });
            }
        }

        #region Métodos Privados

        private CreditoCalculos CalcularParametrosCredito(decimal montoSolicitado, decimal tasaInteres, int cantidadCuotas)
        {
            var tasaMensualDecimal = tasaInteres / 100m;

            var cuotaMensual = _financialService.CalcularCuotaSistemaFrances(
                montoSolicitado,
                tasaMensualDecimal,
                cantidadCuotas);

            var totalAPagar = _financialService.CalcularTotalConInteres(
                montoSolicitado,
                tasaMensualDecimal,
                cantidadCuotas);

            var cftea = _financialService.CalcularCFTEA(
                totalAPagar,
                montoSolicitado,
                cantidadCuotas);

            return new CreditoCalculos
            {
                TasaMensualDecimal = tasaMensualDecimal,
                CuotaMensual = cuotaMensual,
                TotalAPagar = totalAPagar,
                CFTEA = cftea
            };
        }

        private async Task<ClienteDetalleViewModel> ConstructDetalleViewModel(Cliente cliente, string? tab)
        {
            var detalleViewModel = new ClienteDetalleViewModel
            {
                TabActivo = tab ?? "informacion",
                Cliente = _mapper.Map<ClienteViewModel>(cliente)
            };

            // Ensure display name is consistent with lookup service formatting
            var display = await _clienteLookup.GetClienteDisplayNameAsync(cliente.Id);
            if (!string.IsNullOrWhiteSpace(display))
            {
                detalleViewModel.Cliente.NombreCompleto = display;
            }

            // FIX punto 4.2: no recalcular Edad acá (AutoMapperProfile ya la calcula).
            detalleViewModel.Documentos = await _documentoService.GetByClienteIdAsync(cliente.Id);

            var creditos = await _creditoService.GetByClienteIdAsync(cliente.Id);
            detalleViewModel.CreditosActivos = creditos
                .Where(c => c.Estado == EstadoCredito.Activo)
                .ToList();

            detalleViewModel.EvaluacionCredito = await EvaluarCapacidadCrediticia(cliente.Id, detalleViewModel);

            // Evaluar aptitud crediticia (semáforo)
            detalleViewModel.AptitudCrediticia = await _aptitudService.EvaluarAptitudAsync(cliente.Id, guardarResultado: true);

            return detalleViewModel;
        }

        private async Task<int?> ProcesarGaranteAsync(AppDbContext context, Cliente cliente, SolicitudCreditoViewModel model)
        {
            if (model.GaranteId.HasValue)
            {
                cliente.GaranteId = model.GaranteId.Value;
                return model.GaranteId.Value;
            }

            if (string.IsNullOrWhiteSpace(model.GaranteDocumento))
                return null;

            var garante = new Garante
            {
                ClienteId = model.ClienteId,
                TipoDocumento = "DNI",
                NumeroDocumento = model.GaranteDocumento,
                Nombre = model.GaranteNombre,
                Telefono = model.GaranteTelefono,
                Relacion = "Garante",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            context.Garantes.Add(garante);
            await context.SaveChangesAsync();

            cliente.GaranteId = garante.Id;

            return garante.Id;
        }

        private async Task<EvaluacionCreditoResult> ObtenerEvaluacionActualAsync(Cliente cliente)
        {
            // Reuse ConstructDetalleViewModel to avoid duplicating mapping + document/credit loading
            var detalle = await ConstructDetalleViewModel(cliente, tab: null);

            // ConstructDetalleViewModel already runs EvaluarCapacidadCrediticia and populates EvaluacionCredito
            return detalle.EvaluacionCredito ?? new EvaluacionCreditoResult();
        }

        private static decimal CalcularPuntajeRiesgoInicial(EvaluacionCreditoResult eval)
        {
            return eval.NivelRiesgo switch
            {
                "Bajo" => 2.0m,
                "Medio" => 5.0m,
                "Alto" => 8.0m,
                _ => 5.0m
            };
        }

        private async Task<string> GenerarNumeroCreditoAsync(AppDbContext context, string numeroDocumento)
        {
            var baseKey = $"CRE-{DateTime.UtcNow:yyyyMMdd}-{numeroDocumento}";
            var prefix = baseKey + "-";

            var ultimo = await context.Creditos
                .AsNoTracking()
                .Where(c => c.Numero.StartsWith(prefix))
                .OrderByDescending(c => c.Numero)
                .Select(c => c.Numero)
                .FirstOrDefaultAsync();

            var next = 1;

            if (!string.IsNullOrWhiteSpace(ultimo))
            {
                var idx = ultimo.LastIndexOf('-');
                if (idx >= 0 && idx < ultimo.Length - 1 && int.TryParse(ultimo[(idx + 1)..], out var n))
                    next = n + 1;
            }

            return $"{baseKey}-{next:D3}";
        }

        private static bool EsPosibleColisionNumeroCredito(DbUpdateException ex)
        {
            var msg = ex.GetBaseException().Message;

            return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                   || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                   || msg.Contains("IX_Creditos_Numero", StringComparison.OrdinalIgnoreCase)
                   || (msg.Contains("Creditos", StringComparison.OrdinalIgnoreCase) && msg.Contains("Numero", StringComparison.OrdinalIgnoreCase));
        }

        private Credito CrearCredito(
            SolicitudCreditoViewModel model,
            Cliente cliente,
            int? garanteId,
            CreditoCalculos calculos,
            string numeroCredito,
            bool requiereGarante,
            decimal puntajeRiesgoInicial)
        {
            var ahora = DateTime.UtcNow;

            return new Credito
            {
                ClienteId = model.ClienteId,
                Numero = numeroCredito,
                MontoSolicitado = model.MontoSolicitado,
                MontoAprobado = model.MontoSolicitado,
                TasaInteres = model.TasaInteres,
                CantidadCuotas = model.CantidadCuotas,
                MontoCuota = calculos.CuotaMensual,
                CFTEA = calculos.CFTEA,
                TotalAPagar = calculos.TotalAPagar,
                SaldoPendiente = calculos.TotalAPagar,
                Estado = model.AprobarConExcepcion ? EstadoCredito.Solicitado : EstadoCredito.Aprobado,
                FechaSolicitud = ahora,
                FechaAprobacion = model.AprobarConExcepcion ? null : ahora,
                FechaPrimeraCuota = model.AprobarConExcepcion ? null : ahora.AddMonths(1),
                PuntajeRiesgoInicial = puntajeRiesgoInicial,
                GaranteId = garanteId,
                RequiereGarante = requiereGarante,
                AprobadoPor = model.AprobarConExcepcion ? model.AutorizadoPor : "Sistema",
                Observaciones = model.AprobarConExcepcion
                    ? $"APROBADO CON EXCEPCIÓN: {model.MotivoExcepcion}\n{model.Observaciones}"
                    : model.Observaciones,
                CreatedAt = ahora,
                UpdatedAt = ahora,
                IsDeleted = false
            };
        }

        private async Task GenerarCuotasAsync(AppDbContext context, Credito credito, SolicitudCreditoViewModel model, decimal tasaMensualDecimal)
        {
            if (credito.Estado != EstadoCredito.Aprobado || !credito.FechaPrimeraCuota.HasValue)
                return;

            var fechaVencimiento = credito.FechaPrimeraCuota.Value;
            var cuotas = new List<Cuota>(model.CantidadCuotas);

            var saldo = model.MontoSolicitado;

            for (int i = 1; i <= model.CantidadCuotas; i++)
            {
                var montoInteres = saldo * tasaMensualDecimal;
                var montoCapital = credito.MontoCuota - montoInteres;

                cuotas.Add(new Cuota
                {
                    CreditoId = credito.Id,
                    NumeroCuota = i,
                    MontoCapital = montoCapital,
                    MontoInteres = montoInteres,
                    MontoTotal = credito.MontoCuota,
                    FechaVencimiento = fechaVencimiento,
                    Estado = EstadoCuota.Pendiente,
                    MontoPagado = 0,
                    MontoPunitorio = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });

                saldo -= montoCapital;
                fechaVencimiento = fechaVencimiento.AddMonths(1);
            }

            context.Cuotas.AddRange(cuotas);
            await context.SaveChangesAsync();
        }

        private void CargarDropdowns()
        {
            ViewBag.TiposDocumento = new SelectList(DropdownConstants.TiposDocumento);
            ViewBag.EstadosCiviles = new SelectList(DropdownConstants.EstadosCiviles);
            ViewBag.TiposEmpleo = new SelectList(DropdownConstants.TiposEmpleo);
            ViewBag.Provincias = new SelectList(DropdownConstants.Provincias);
        }

        private async Task<EvaluacionCreditoResult> EvaluarCapacidadCrediticia(int clienteId, ClienteDetalleViewModel modelo)
        {
            var evaluacion = new EvaluacionCreditoResult();

            try
            {
                var tiposDocumentosVerificados = modelo.Documentos
                    .Where(d => d.Estado == EstadoDocumento.Verificado)
                    .Select(d => d.TipoDocumentoNombre)
                    .ToList();

                evaluacion.TieneDocumentosCompletos = ClienteControllerHelper.VerificaDocumentosRequeridos(tiposDocumentosVerificados);
                evaluacion.DocumentosFaltantes = ClienteControllerHelper.ObtenerDocumentosFaltantes(tiposDocumentosVerificados);

                EvaluacionCrediticiaHelper.CalcularCapacidadFinanciera(evaluacion, modelo);

                evaluacion.ScoreCrediticio = CreditoScoringHelper.CalcularScoreCrediticio(modelo);
                evaluacion.NivelRiesgo = ClienteControllerHelper.DeterminarNivelRiesgo(evaluacion.ScoreCrediticio);

                evaluacion.RequiereGarante = EvaluacionCrediticiaHelper.DeterminarRequiereGarante(evaluacion);
                evaluacion.TieneGarante = await ClienteTieneGaranteAsync(clienteId);

                evaluacion.CumpleRequisitos = EvaluacionCrediticiaHelper.VerificaCumplimientoRequisitos(evaluacion);
                evaluacion.PuedeAprobarConExcepcion = EvaluacionCrediticiaHelper.DeterminarPuedeAprobarConExcepcion(evaluacion);

                ClienteControllerHelper.GenerarAlertasYRecomendaciones(evaluacion);

                return evaluacion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar capacidad crediticia del cliente {ClienteId}", clienteId);
                evaluacion.AlertasYRecomendaciones.Add("Error al evaluar capacidad crediticia");
                return evaluacion;
            }
        }

        private async Task<bool> ClienteTieneGaranteAsync(int clienteId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Set<Cliente>()
                .AsNoTracking()
                .Where(c => c.Id == clienteId && !c.IsDeleted)
                .Select(c => c.GaranteId != null)
                .FirstOrDefaultAsync();
        }

        private class CreditoCalculos
        {
            public decimal TasaMensualDecimal { get; init; }
            public decimal CuotaMensual { get; init; }
            public decimal TotalAPagar { get; init; }
            public decimal CFTEA { get; init; }
        }

        #endregion
    }
}
