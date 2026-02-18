using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheBuryProject.Data;
using TheBuryProject.Filters;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Exceptions;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize]
    [PermisoRequerido(Modulo = "clientes", Accion = "view")]
    public class ClienteController : Controller
    {
        private readonly IClienteService _clienteService;
        private readonly IDocumentoClienteService _documentoService;
        private readonly ICreditoService _creditoService;
        private readonly ICreditoDisponibleService _creditoDisponibleService;
        private readonly IClienteAptitudService _aptitudService;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
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
            ICreditoDisponibleService creditoDisponibleService,
            IClienteAptitudService aptitudService,
            IDbContextFactory<AppDbContext> contextFactory,
            IMapper mapper,
            ILogger<ClienteController> logger,
            IClienteLookupService clienteLookup)
        {
            _clienteService = clienteService;
            _documentoService = documentoService;
            _creditoService = creditoService;
            _creditoDisponibleService = creditoDisponibleService;
            _aptitudService = aptitudService;
            _contextFactory = contextFactory;
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

        public async Task<IActionResult> Create(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
            CargarDropdowns();
            await CargarPerfilesCredito(); // TAREA 8: Cargar perfiles para el selector
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
                await CargarPerfilesCredito(viewModel.PerfilCreditoPreferidoId); // TAREA 8: Cargar perfiles con selección actual
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

        [HttpGet]
        public async Task<IActionResult> LimitesPorPuntaje(string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
                ViewBag.PuedeAdministrarLimites = User.TienePermiso("clientes", "managecreditlimits");
                var model = await ConstruirModeloLimitesPorPuntajeAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar configuración de límites por puntaje");
                TempData["Error"] = "No se pudo cargar la configuración de límites por puntaje.";
                return RedirectToReturnUrlOrIndex(returnUrl);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Modulo = "clientes", Accion = "managecreditlimits")]
        public async Task<IActionResult> LimitesPorPuntaje(ClienteCreditoLimitesViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
            ViewBag.PuedeAdministrarLimites = true;

            var items = model.Items ?? new List<ClienteCreditoLimiteItemViewModel>();

            if (!items.Any())
            {
                ModelState.AddModelError(string.Empty, "No se recibieron registros para guardar.");
            }

            var puntajesEsperados = Enum.GetValues<NivelRiesgoCredito>();

            if (items.Any())
            {
                var puntajesRecibidos = items.Select(i => i.Puntaje).ToList();

                if (items.Any(i => i.LimiteMonto != decimal.Truncate(i.LimiteMonto)))
                {
                    ModelState.AddModelError(string.Empty, "Los límites por puntaje deben cargarse como números enteros.");
                }

                var repetidos = puntajesRecibidos
                    .GroupBy(p => p)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (repetidos.Any())
                {
                    ModelState.AddModelError(string.Empty, "Existen puntajes duplicados en la grilla de configuración.");
                }

                if (puntajesRecibidos.Count != puntajesEsperados.Length || puntajesEsperados.Except(puntajesRecibidos).Any())
                {
                    ModelState.AddModelError(string.Empty, "La configuración debe contener exactamente los puntajes del 1 al 5.");
                }
            }

            if (!ModelState.IsValid)
            {
                var fallback = await ConstruirModeloLimitesPorPuntajeAsync(items);
                return View(fallback);
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var puntajes = items.Select(i => i.Puntaje).ToList();
                var existentes = await context.PuntajesCreditoLimite
                    .Where(p => puntajes.Contains(p.Puntaje))
                    .ToListAsync();

                var usuario = User?.Identity?.Name ?? "System";
                var fecha = DateTime.UtcNow;

                foreach (var item in items)
                {
                    var existente = existentes.FirstOrDefault(x => x.Puntaje == item.Puntaje);

                    if (existente == null)
                    {
                        context.PuntajesCreditoLimite.Add(new PuntajeCreditoLimite
                        {
                            Puntaje = item.Puntaje,
                            LimiteMonto = item.LimiteMonto,
                            Activo = item.Activo,
                            FechaActualizacion = fecha,
                            UsuarioActualizacion = usuario
                        });
                    }
                    else
                    {
                        existente.LimiteMonto = item.LimiteMonto;
                        existente.Activo = item.Activo;
                        existente.FechaActualizacion = fecha;
                        existente.UsuarioActualizacion = usuario;
                    }
                }

                await context.SaveChangesAsync();

                TempData["Success"] = "Configuración de límites por puntaje guardada correctamente.";
                return RedirectToAction(nameof(LimitesPorPuntaje), new { returnUrl = GetSafeReturnUrl(returnUrl) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar configuración de límites por puntaje");
                TempData["Error"] = "No se pudo guardar la configuración de límites por puntaje.";

                var fallback = await ConstruirModeloLimitesPorPuntajeAsync(items);
                return View(fallback);
            }
        }

        /// <summary>
        /// Asigna o actualiza el límite de crédito (cupo) de un cliente.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]        public async Task<IActionResult> AsignarLimiteCredito(int clienteId, decimal limiteCredito, string? motivo = null, string? returnUrl = null)
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

        #region Métodos Privados

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
            detalleViewModel.CreditosActivos = creditos;

            detalleViewModel.EvaluacionCredito = await EvaluarCapacidadCrediticia(cliente.Id, detalleViewModel);

            // Evaluar aptitud crediticia (semáforo)
            detalleViewModel.AptitudCrediticia = await _aptitudService.EvaluarAptitudAsync(cliente.Id, guardarResultado: true);

            // Panel de visibilidad del disponible (Tarea 4)
            detalleViewModel.CreditoDisponiblePanel.PuntajeActual = cliente.NivelRiesgo;
            try
            {
                detalleViewModel.CreditoDisponiblePanel.Valores = await _creditoDisponibleService.CalcularDisponibleAsync(cliente.Id);
            }
            catch (CreditoDisponibleException ex)
            {
                detalleViewModel.CreditoDisponiblePanel.TieneErrorConfiguracion = true;
                detalleViewModel.CreditoDisponiblePanel.MensajeError = ex.Message;
            }

            return detalleViewModel;
        }

        private void CargarDropdowns()
        {
            ViewBag.TiposDocumento = new SelectList(DropdownConstants.TiposDocumento);
            ViewBag.EstadosCiviles = new SelectList(DropdownConstants.EstadosCiviles);
            ViewBag.TiposEmpleo = new SelectList(DropdownConstants.TiposEmpleo);
            ViewBag.Provincias = new SelectList(DropdownConstants.Provincias);
            
            // Niveles de riesgo crediticio (1-5)
            ViewBag.NivelesRiesgo = Enum.GetValues<NivelRiesgoCredito>()
                .Select(n => new SelectListItem
                {
                    Value = ((int)n).ToString(),
                    Text = n.GetDisplayName()
                })
                .ToList();
        }

        // TAREA 8: Cargar perfiles de crédito para el selector
        private async Task CargarPerfilesCredito(int? perfilSeleccionadoId = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var perfiles = await context.PerfilesCredito
                .Where(p => !p.IsDeleted && p.Activo)
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Nombre)
                .ToListAsync();
            
            ViewBag.PerfilesCredito = new SelectList(perfiles, "Id", "Nombre", perfilSeleccionadoId);
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

        private async Task<ClienteCreditoLimitesViewModel> ConstruirModeloLimitesPorPuntajeAsync(
            IEnumerable<ClienteCreditoLimiteItemViewModel>? cambiosUsuario = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var dbItems = await context.PuntajesCreditoLimite
                .AsNoTracking()
                .OrderBy(x => x.Puntaje)
                .ToListAsync();

            var cambiosMap = (cambiosUsuario ?? Enumerable.Empty<ClienteCreditoLimiteItemViewModel>())
                .GroupBy(x => x.Puntaje)
                .ToDictionary(g => g.Key, g => g.First());

            var items = new List<ClienteCreditoLimiteItemViewModel>();

            foreach (var puntaje in Enum.GetValues<NivelRiesgoCredito>().OrderBy(x => (int)x))
            {
                if (cambiosMap.TryGetValue(puntaje, out var cambio))
                {
                    var baseDb = dbItems.FirstOrDefault(x => x.Puntaje == puntaje);
                    items.Add(new ClienteCreditoLimiteItemViewModel
                    {
                        Id = baseDb?.Id ?? cambio.Id,
                        Puntaje = puntaje,
                        LimiteMonto = cambio.LimiteMonto,
                        Activo = cambio.Activo,
                        FechaActualizacion = baseDb?.FechaActualizacion,
                        UsuarioActualizacion = baseDb?.UsuarioActualizacion
                    });
                    continue;
                }

                var existente = dbItems.FirstOrDefault(x => x.Puntaje == puntaje);
                if (existente != null)
                {
                    items.Add(new ClienteCreditoLimiteItemViewModel
                    {
                        Id = existente.Id,
                        Puntaje = existente.Puntaje,
                        LimiteMonto = existente.LimiteMonto,
                        Activo = existente.Activo,
                        FechaActualizacion = existente.FechaActualizacion,
                        UsuarioActualizacion = existente.UsuarioActualizacion
                    });
                }
                else
                {
                    items.Add(new ClienteCreditoLimiteItemViewModel
                    {
                        Puntaje = puntaje,
                        LimiteMonto = 0m,
                        Activo = true
                    });
                }
            }

            return new ClienteCreditoLimitesViewModel { Items = items };
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

        #endregion
    }
}
