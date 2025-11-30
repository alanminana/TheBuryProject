using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [AllowAnonymous]
    public class ClienteController : Controller
    {
        private readonly IClienteService _clienteService;
        private readonly IDocumentoClienteService _documentoService;
        private readonly ICreditoService _creditoService;
        private readonly IEvaluacionCreditoService _evaluacionService;
        private readonly AppDbContext _context;
        private readonly IFinancialCalculationService _financialService;
        private readonly IMapper _mapper;
        private readonly ILogger<ClienteController> _logger;

        public ClienteController(
            IClienteService clienteService,
            IDocumentoClienteService documentoService,
            ICreditoService creditoService,
            IEvaluacionCreditoService evaluacionService,
            AppDbContext context,
            IFinancialCalculationService financialService,
            IMapper mapper,
            ILogger<ClienteController> logger)
        {
            _clienteService = clienteService;
            _documentoService = documentoService;
            _creditoService = creditoService;
            _evaluacionService = evaluacionService;
            _context = context;
            _financialService = financialService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Cliente
        public async Task<IActionResult> Index(ClienteFilterViewModel filter)
        {
            try
            {
                var clientes = await _clienteService.SearchAsync(
                    searchTerm: filter.SearchTerm,
                    tipoDocumento: filter.TipoDocumento,
                    soloActivos: filter.SoloActivos,
                    conCreditosActivos: filter.ConCreditosActivos,
                    puntajeMinimo: filter.PuntajeMinimo,
                    orderBy: filter.OrderBy,
                    orderDirection: filter.OrderDirection);

                var viewModels = _mapper.Map<IEnumerable<ClienteViewModel>>(clientes);
                ClienteHelper.AplicarEdadAMultiples(viewModels);

                filter.Clientes = viewModels;
                filter.TotalResultados = viewModels.Count();

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

        // GET: Cliente/Details/5
        public async Task<IActionResult> Details(int id, string? tab)
        {
            try
            {
                var cliente = await _clienteService.GetByIdAsync(id);
                if (!ClienteValidationHelper.ClienteExiste(cliente))
                    return RedirectToAction(nameof(Index));

                var detalleViewModel = await ConstructDetalleViewModel(cliente!, tab);
                return View(detalleViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cliente {Id}", id);
                TempData["Error"] = "Error al cargar el cliente";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Cliente/Create
        public IActionResult Create()
        {
            CargarDropdowns();
            return View(new ClienteViewModel());
        }

        // POST: Cliente/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClienteViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    CargarDropdowns();
                    return View(viewModel);
                }

                var cliente = _mapper.Map<Cliente>(viewModel);
                await _clienteService.CreateAsync(cliente);

                TempData["Success"] = $"Cliente {cliente.NombreCompleto} creado exitosamente";
                return RedirectToAction(nameof(Details), new { id = cliente.Id });
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

        // GET: Cliente/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var cliente = await _clienteService.GetByIdAsync(id);
                if (!ClienteValidationHelper.ClienteExiste(cliente))
                    return RedirectToAction(nameof(Index));

                var viewModel = _mapper.Map<ClienteViewModel>(cliente!);
                CargarDropdowns();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cliente {Id} para edición", id);
                TempData["Error"] = "Error al cargar el cliente";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Cliente/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClienteViewModel viewModel)
        {
            if (id != viewModel.Id)
                return NotFound();

            try
            {
                if (!ModelState.IsValid)
                {
                    CargarDropdowns();
                    return View(viewModel);
                }

                var cliente = _mapper.Map<Cliente>(viewModel);
                await _clienteService.UpdateAsync(cliente);

                TempData["Success"] = "Cliente actualizado exitosamente";
                return RedirectToAction(nameof(Details), new { id });
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

        // GET: Cliente/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var cliente = await _clienteService.GetByIdAsync(id);
                if (!ClienteValidationHelper.ClienteExiste(cliente))
                    return RedirectToAction(nameof(Index));

                var viewModel = _mapper.Map<ClienteViewModel>(cliente!);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cliente {Id} para eliminación", id);
                TempData["Error"] = "Error al cargar el cliente";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Cliente/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _clienteService.DeleteAsync(id);
                TempData["Success"] = "Cliente eliminado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cliente {Id}", id);
                TempData["Error"] = "Error al eliminar el cliente";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: Cliente/SolicitarCredito
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarCredito(SolicitudCreditoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Por favor complete todos los campos requeridos";
                    return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "evaluacion" });
                }

                var cliente = await _context.Clientes.FindAsync(model.ClienteId);
                if (!ClienteValidationHelper.ClienteExiste(cliente))
                    return RedirectToAction(nameof(Index));

                // Procesar garante
                int? garanteId = await ProcesarGarante(model);

                // Calcular parámetros del crédito
                var calculos = CalcularParametrosCredito(
                    model.MontoSolicitado, model.TasaInteres, model.CantidadCuotas);

                // Generar número de crédito
                var numeroCredito = await GenerarNumeroCreditoAsync(cliente!.NumeroDocumento);

                // ✅ USAR TRANSACCIÓN EXPLÍCITA
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Crear crédito
                        var credito = CrearCredito(model, cliente!, garanteId, calculos, numeroCredito);
                        _context.Creditos.Add(credito);
                        await _context.SaveChangesAsync();

                        // Generar cuotas
                        await GenerarCuotasAsync(credito, model, calculos.TasaMensualDecimal);

                        // Confirmar transacción
                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "Crédito {Numero} creado exitosamente para cliente {ClienteId}. Monto: {Monto}, Cuotas: {Cuotas}",
                            numeroCredito, model.ClienteId, model.MontoSolicitado, model.CantidadCuotas);

                        TempData["Success"] = $"Crédito {numeroCredito} solicitado exitosamente. " +
                            $"Monto: {model.MontoSolicitado:C}, {model.CantidadCuotas} cuotas de {calculos.CuotaMensual:C}";
                        return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "creditos" });
                    }
                    catch (Exception ex)
                    {
                        // Si algo falla, rollback automático
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error durante transacción de crédito para cliente {ClienteId}", model.ClienteId);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar crédito para cliente {ClienteId}", model.ClienteId);
                TempData["Error"] = $"Error al solicitar el crédito: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "evaluacion" });
            }
        }

        #region Métodos Privados

        private CreditoCalculos CalcularParametrosCredito(decimal montoSolicitado, decimal tasaInteres, int cantidadCuotas)
        {
            var tasaMensualDecimal = tasaInteres / 100;
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

        /// <summary>
        /// Construye el ViewModel de detalle con toda la información del cliente
        /// </summary>
        private async Task<ClienteDetalleViewModel> ConstructDetalleViewModel(Cliente cliente, string? tab)
        {
            var detalleViewModel = new ClienteDetalleViewModel
            {
                TabActivo = tab ?? "informacion",
                Cliente = _mapper.Map<ClienteViewModel>(cliente)
            };

            detalleViewModel.Cliente.Edad = ClienteHelper.CalcularEdad(detalleViewModel.Cliente.FechaNacimiento);
            detalleViewModel.Documentos = await _documentoService.GetByClienteIdAsync(cliente.Id);

            var creditos = await _creditoService.GetByClienteIdAsync(cliente.Id);
            detalleViewModel.CreditosActivos = creditos
                .Where(c => c.Estado == EstadoCredito.Activo)
                .ToList();

            detalleViewModel.EvaluacionCredito = await EvaluarCapacidadCrediticia(cliente.Id, detalleViewModel);

            return detalleViewModel;
        }

        /// <summary>
        /// Procesa la información del garante (crea o vincula)
        /// </summary>
        private async Task<int?> ProcesarGarante(SolicitudCreditoViewModel model)
        {
            int? garanteId = model.GaranteId;

            if (model.GaranteId == null && !string.IsNullOrEmpty(model.GaranteDocumento))
            {
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

                _context.Garantes.Add(garante);
                await _context.SaveChangesAsync();
                garanteId = garante.Id;

                var cliente = await _context.Clientes.FindAsync(model.ClienteId);
                if (cliente != null)
                {
                    cliente.GaranteId = garanteId;
                    _context.Clientes.Update(cliente);
                }
            }

            return garanteId;
        }

        /// <summary>
        /// Genera un número único para el crédito
        /// </summary>
        private async Task<string> GenerarNumeroCreditoAsync(string numeroDocumento)
        {
            var numeroCreditoBase = $"CRE-{DateTime.UtcNow:yyyyMMdd}-{numeroDocumento}";
            var creditosExistentes = await _context.Creditos
                .Where(c => c.Numero.StartsWith(numeroCreditoBase))
                .CountAsync();
            return $"{numeroCreditoBase}-{creditosExistentes + 1:D3}";
        }

        /// <summary>
        /// Crea una instancia de Credito con todos los parámetros calculados
        /// </summary>
        private Credito CrearCredito(
            SolicitudCreditoViewModel model,
            Cliente cliente,
            int? garanteId,
            CreditoCalculos calculos,
            string numeroCredito)
        {
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
                FechaSolicitud = DateTime.UtcNow,
                FechaAprobacion = DateTime.UtcNow,
                FechaPrimeraCuota = DateTime.UtcNow.AddMonths(1),
                GaranteId = garanteId,
                RequiereGarante = garanteId.HasValue,
                AprobadoPor = model.AprobarConExcepcion ? model.AutorizadoPor : "Sistema",
                Observaciones = model.AprobarConExcepcion
                    ? $"APROBADO CON EXCEPCIÓN: {model.MotivoExcepcion}\n{model.Observaciones}"
                    : model.Observaciones,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        /// <summary>
        /// Genera las cuotas para un crédito
        /// </summary>
        private async Task GenerarCuotasAsync(Credito credito, SolicitudCreditoViewModel model, decimal tasaMensualDecimal)
        {
            var fechaVencimiento = credito.FechaPrimeraCuota ?? DateTime.UtcNow.AddMonths(1);
            var cuotas = new List<Cuota>();

            for (int i = 1; i <= model.CantidadCuotas; i++)
            {
                decimal saldoPendienteAnterior = i == 1
                    ? model.MontoSolicitado
                    : model.MontoSolicitado - cuotas.Take(i - 1).Sum(c => c.MontoCapital);

                decimal montoInteres = saldoPendienteAnterior * tasaMensualDecimal;
                decimal montoCapital = credito.MontoCuota - montoInteres;

                var cuota = new Cuota
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
                };

                cuotas.Add(cuota);
                fechaVencimiento = fechaVencimiento.AddMonths(1);
            }

            _context.Cuotas.AddRange(cuotas);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Carga los dropdowns comunes en ViewBag
        /// </summary>
        private void CargarDropdowns()
        {
            ViewBag.TiposDocumento = new SelectList(DropdownConstants.TiposDocumento);
            ViewBag.EstadosCiviles = new SelectList(DropdownConstants.EstadosCiviles);
            ViewBag.TiposEmpleo = new SelectList(DropdownConstants.TiposEmpleo);
            ViewBag.Provincias = new SelectList(DropdownConstants.Provincias);
        }

        /// <summary>
        /// Evalúa la capacidad crediticia del cliente
        /// </summary>
        private Task<EvaluacionCreditoResult> EvaluarCapacidadCrediticia(int clienteId, ClienteDetalleViewModel modelo)
        {
            var evaluacion = new EvaluacionCreditoResult();

            try
            {
                // 1. Validar documentación
                var tiposDocumentosVerificados = modelo.Documentos
                    .Where(d => d.Estado == EstadoDocumento.Verificado)
                    .Select(d => d.TipoDocumentoNombre)
                    .ToList();

                // ✅ USAR HELPER - NO REPETIR LÓGICA
                evaluacion.TieneDocumentosCompletos = ClienteControllerHelper.VerificaDocumentosRequeridos(tiposDocumentosVerificados);
                evaluacion.DocumentosFaltantes = ClienteControllerHelper.ObtenerDocumentosFaltantes(tiposDocumentosVerificados);

                // 2. Calcular capacidad financiera
                EvaluacionCrediticiaHelper.CalcularCapacidadFinanciera(evaluacion, modelo);

                // 3. Score crediticio
                // ✅ USAR HELPER - NO REPETIR
                evaluacion.ScoreCrediticio = CreditoScoringHelper.CalcularScoreCrediticio(modelo);
                evaluacion.NivelRiesgo = ClienteControllerHelper.DeterminarNivelRiesgo(evaluacion.ScoreCrediticio);

                // 4-6. Requisitos y garante
                evaluacion.RequiereGarante = EvaluacionCrediticiaHelper.DeterminarRequiereGarante(evaluacion);
                evaluacion.TieneGarante = modelo.Cliente.CreditosActivos > 0;
                evaluacion.CumpleRequisitos = EvaluacionCrediticiaHelper.VerificaCumplimientoRequisitos(evaluacion);

                // 7-8. Alertas y excepciones
                evaluacion.PuedeAprobarConExcepcion = EvaluacionCrediticiaHelper.DeterminarPuedeAprobarConExcepcion(evaluacion);
                ClienteControllerHelper.GenerarAlertasYRecomendaciones(evaluacion);

                return Task.FromResult(evaluacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar capacidad crediticia del cliente {ClienteId}", clienteId);
                evaluacion.AlertasYRecomendaciones.Add("❌ Error al evaluar capacidad crediticia");
                return Task.FromResult(evaluacion);
            }
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