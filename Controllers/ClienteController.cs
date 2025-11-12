using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize(Roles = "Admin,Gerente,Vendedor")]
    public class ClienteController : Controller
    {
        private readonly IClienteService _clienteService;
        private readonly IDocumentoClienteService _documentoService;
        private readonly ICreditoService _creditoService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ClienteController> _logger;

        public ClienteController(
            IClienteService clienteService,
            IDocumentoClienteService documentoService,
            ICreditoService creditoService,
            AppDbContext context,
            IMapper mapper,
            ILogger<ClienteController> logger)
        {
            _clienteService = clienteService;
            _documentoService = documentoService;
            _creditoService = creditoService;
            _context = context;
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

                // Calcular edad
                foreach (var vm in viewModels)
                {
                    if (vm.FechaNacimiento.HasValue)
                    {
                        var hoy = DateTime.Today;
                        var edad = hoy.Year - vm.FechaNacimiento.Value.Year;
                        if (vm.FechaNacimiento.Value.Date > hoy.AddYears(-edad)) edad--;
                        vm.Edad = edad;
                    }
                }

                filter.Clientes = viewModels;
                filter.TotalResultados = viewModels.Count();

                // Cargar tipos de documento
                ViewBag.TiposDocumento = new SelectList(new[] { "DNI", "CUIL", "CUIT" });

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
                if (cliente == null)
                {
                    TempData["Error"] = "Cliente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Crear ViewModel consolidado
                var detalleViewModel = new ClienteDetalleViewModel
                {
                    TabActivo = tab ?? "informacion"
                };

                // Cargar datos del cliente
                detalleViewModel.Cliente = _mapper.Map<ClienteViewModel>(cliente);

                // Calcular edad
                if (detalleViewModel.Cliente.FechaNacimiento.HasValue)
                {
                    var hoy = DateTime.Today;
                    var edad = hoy.Year - detalleViewModel.Cliente.FechaNacimiento.Value.Year;
                    if (detalleViewModel.Cliente.FechaNacimiento.Value.Date > hoy.AddYears(-edad)) edad--;
                    detalleViewModel.Cliente.Edad = edad;
                }

                // Cargar documentos del cliente
                detalleViewModel.Documentos = await _documentoService.GetByClienteIdAsync(id);

                // Cargar créditos activos
                var creditos = await _creditoService.GetByClienteIdAsync(id);
                detalleViewModel.CreditosActivos = creditos
                    .Where(c => c.Estado == Models.Enums.EstadoCredito.Activo)
                    .ToList();

                // Evaluar capacidad crediticia
                detalleViewModel.EvaluacionCredito = await EvaluarCapacidadCrediticia(id, detalleViewModel);

                return View(detalleViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cliente {Id}", id);
                TempData["Error"] = "Error al cargar el cliente";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Evalúa la capacidad crediticia del cliente y genera recomendaciones
        /// </summary>
        private Task<EvaluacionCreditoResult> EvaluarCapacidadCrediticia(int clienteId, ClienteDetalleViewModel modelo)
        {
            var evaluacion = new EvaluacionCreditoResult();

            try
            {
                // 1. Validar documentación
                var documentosRequeridos = new[] { "DNI", "Recibo de Sueldo", "Servicio (Luz/Gas/Agua)", "Veraz" };
                var tiposDocumentosVerificados = modelo.Documentos
                    .Where(d => d.Estado == Models.Enums.EstadoDocumento.Verificado)
                    .Select(d => d.TipoDocumentoNombre)
                    .ToList();

                evaluacion.TieneDocumentosCompletos = tiposDocumentosVerificados.Contains("DNI") &&
                                                      tiposDocumentosVerificados.Contains("Recibo de Sueldo") &&
                                                      tiposDocumentosVerificados.Contains("Veraz") &&
                                                      (tiposDocumentosVerificados.Contains("Servicio de Luz") ||
                                                       tiposDocumentosVerificados.Contains("Servicio de Gas") ||
                                                       tiposDocumentosVerificados.Contains("Servicio de Agua"));

                // Detectar documentos faltantes
                if (!tiposDocumentosVerificados.Contains("DNI"))
                    evaluacion.DocumentosFaltantes.Add("DNI");
                if (!tiposDocumentosVerificados.Contains("Recibo de Sueldo"))
                    evaluacion.DocumentosFaltantes.Add("Recibo de Sueldo");
                if (!tiposDocumentosVerificados.Contains("Veraz"))
                    evaluacion.DocumentosFaltantes.Add("Veraz");
                if (!tiposDocumentosVerificados.Contains("Servicio de Luz") &&
                    !tiposDocumentosVerificados.Contains("Servicio de Gas") &&
                    !tiposDocumentosVerificados.Contains("Servicio de Agua"))
                    evaluacion.DocumentosFaltantes.Add("Servicio (Luz/Gas/Agua)");

                // 2. Calcular capacidad financiera
                evaluacion.IngresosMensuales = modelo.Cliente.IngresoMensual ?? 0;
                evaluacion.DeudaActual = modelo.CreditosActivos.Sum(c => c.SaldoPendiente);

                // Capacidad de pago: 30% del ingreso mensual
                evaluacion.CapacidadPagoMensual = evaluacion.IngresosMensuales * 0.30m;

                // Porcentaje de endeudamiento actual
                if (evaluacion.IngresosMensuales > 0)
                {
                    var cuotaMensualActual = modelo.CreditosActivos.Sum(c => c.MontoTotal / c.CantidadCuotas);
                    evaluacion.PorcentajeEndeudamiento = (double)(cuotaMensualActual / evaluacion.IngresosMensuales * 100);
                }

                // Monto máximo disponible (asumiendo 12 cuotas)
                evaluacion.MontoMaximoDisponible = evaluacion.CapacidadPagoMensual * 12;

                // 3. Score crediticio (simplificado)
                evaluacion.ScoreCrediticio = CalcularScoreCrediticio(modelo);

                // 4. Nivel de riesgo
                if (evaluacion.ScoreCrediticio >= 700)
                    evaluacion.NivelRiesgo = "Bajo";
                else if (evaluacion.ScoreCrediticio >= 500)
                    evaluacion.NivelRiesgo = "Medio";
                else
                    evaluacion.NivelRiesgo = "Alto";

                // 5. Verificar si requiere garante
                evaluacion.RequiereGarante = evaluacion.PorcentajeEndeudamiento > 40 ||
                                            evaluacion.ScoreCrediticio < 500 ||
                                            !evaluacion.TieneDocumentosCompletos;

                // Verificar si tiene garante
                evaluacion.TieneGarante = modelo.Cliente.CreditosActivos > 0; // Simplificado, deberías verificar en la tabla Garantes

                // 6. Determinar si cumple requisitos
                evaluacion.CumpleRequisitos = evaluacion.TieneDocumentosCompletos &&
                                             evaluacion.PorcentajeEndeudamiento < 50 &&
                                             evaluacion.ScoreCrediticio >= 400 &&
                                             (!evaluacion.RequiereGarante || evaluacion.TieneGarante);

                // 7. Generar alertas y recomendaciones
                if (!evaluacion.TieneDocumentosCompletos)
                    evaluacion.AlertasYRecomendaciones.Add($"⚠️ Faltan documentos: {string.Join(", ", evaluacion.DocumentosFaltantes)}");

                if (evaluacion.PorcentajeEndeudamiento > 40)
                    evaluacion.AlertasYRecomendaciones.Add($"⚠️ Endeudamiento alto: {evaluacion.PorcentajeEndeudamiento:F1}%");

                if (evaluacion.RequiereGarante && !evaluacion.TieneGarante)
                    evaluacion.AlertasYRecomendaciones.Add("⚠️ Se requiere garante");

                if (evaluacion.ScoreCrediticio < 500)
                    evaluacion.AlertasYRecomendaciones.Add($"⚠️ Score crediticio bajo: {evaluacion.ScoreCrediticio}");

                if (evaluacion.MontoMaximoDisponible <= 0)
                    evaluacion.AlertasYRecomendaciones.Add("⚠️ Sin capacidad de pago disponible");

                // 8. Determinar si puede aprobar con excepción
                evaluacion.PuedeAprobarConExcepcion = !evaluacion.CumpleRequisitos &&
                                                      evaluacion.IngresosMensuales > 0 &&
                                                      evaluacion.PorcentajeEndeudamiento < 60;

                if (evaluacion.CumpleRequisitos)
                {
                    evaluacion.AlertasYRecomendaciones.Add("✅ El cliente cumple con todos los requisitos");
                }
                else if (evaluacion.PuedeAprobarConExcepcion)
                {
                    evaluacion.AlertasYRecomendaciones.Add("⚠️ Puede aprobarse con excepción autorizada");
                }

                return Task.FromResult(evaluacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar capacidad crediticia del cliente {ClienteId}", clienteId);
                evaluacion.AlertasYRecomendaciones.Add("❌ Error al evaluar capacidad crediticia");
                return Task.FromResult(evaluacion);
            }
        }

        /// <summary>
        /// Calcula un score crediticio simplificado
        /// </summary>
        private int CalcularScoreCrediticio(ClienteDetalleViewModel modelo)
        {
            int score = 500; // Base

            // Sumar puntos por documentación verificada
            score += modelo.Documentos.Count(d => d.Estado == Models.Enums.EstadoDocumento.Verificado) * 50;

            // Restar puntos por endeudamiento
            var endeudamiento = modelo.CreditosActivos.Any() ?
                modelo.CreditosActivos.Sum(c => c.MontoTotal / c.CantidadCuotas) / (modelo.Cliente.IngresoMensual ?? 1) * 100 : 0;
            if (endeudamiento > 40)
                score -= (int)((endeudamiento - 40) * 5);

            // Sumar puntos por antigüedad laboral
            if (!string.IsNullOrEmpty(modelo.Cliente.TiempoTrabajo))
            {
                if (modelo.Cliente.TiempoTrabajo.Contains("año"))
                    score += 100;
                else if (modelo.Cliente.TiempoTrabajo.Contains("mes"))
                    score += 50;
            }

            // Limitar entre 300 y 850
            return Math.Max(300, Math.Min(850, score));
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
                if (cliente == null)
                {
                    TempData["Error"] = "Cliente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = _mapper.Map<ClienteViewModel>(cliente);
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
            {
                return NotFound();
            }

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
                if (cliente == null)
                {
                    TempData["Error"] = "Cliente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = _mapper.Map<ClienteViewModel>(cliente);
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
                    return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "credito" });
                }

                // Validar que el cliente exista
                var cliente = await _context.Clientes.FindAsync(model.ClienteId);
                if (cliente == null)
                {
                    TempData["Error"] = "Cliente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Crear o vincular garante si es necesario
                int? garanteId = model.GaranteId;
                if (model.GaranteId == null && !string.IsNullOrEmpty(model.GaranteDocumento))
                {
                    // Crear nuevo garante
                    var garante = new Garante
                    {
                        ClienteId = model.ClienteId,
                        TipoDocumento = "DNI",
                        NumeroDocumento = model.GaranteDocumento,
                        Nombre = model.GaranteNombre,
                        Telefono = model.GaranteTelefono,
                        Relacion = "Garante",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsDeleted = false
                    };

                    _context.Garantes.Add(garante);
                    await _context.SaveChangesAsync();
                    garanteId = garante.Id;

                    // Actualizar el cliente con el garante
                    cliente.GaranteId = garanteId;
                    _context.Clientes.Update(cliente);
                }

                // Calcular cuota mensual usando sistema francés
                decimal tasaMensualDecimal = model.TasaInteres / 100;
                decimal cuotaMensual;

                if (tasaMensualDecimal > 0)
                {
                    // Fórmula del sistema francés: M = P * (r * (1+r)^n) / ((1+r)^n - 1)
                    var factor = (decimal)Math.Pow((double)(1 + tasaMensualDecimal), model.CantidadCuotas);
                    cuotaMensual = model.MontoSolicitado * (tasaMensualDecimal * factor) / (factor - 1);
                }
                else
                {
                    cuotaMensual = model.MontoSolicitado / model.CantidadCuotas;
                }

                decimal totalAPagar = cuotaMensual * model.CantidadCuotas;

                // Calcular CFTEA (Costo Financiero Total Efectivo Anual)
                // CFTEA = ((TotalAPagar / MontoSolicitado) ^ (12 / CantidadCuotas)) - 1
                decimal cftea = 0;
                if (model.CantidadCuotas > 0 && model.MontoSolicitado > 0)
                {
                    var base_cftea = (double)(totalAPagar / model.MontoSolicitado);
                    var exp_cftea = 12.0 / model.CantidadCuotas;
                    cftea = (decimal)(Math.Pow(base_cftea, exp_cftea) - 1) * 100;
                }

                // Generar número de crédito
                var numeroCreditoBase = $"CRE-{DateTime.Now:yyyyMMdd}-{cliente.NumeroDocumento}";
                var creditosExistentes = await _context.Creditos
                    .Where(c => c.Numero.StartsWith(numeroCreditoBase))
                    .CountAsync();
                var numeroCredito = $"{numeroCreditoBase}-{creditosExistentes + 1:D3}";

                // Crear el crédito
                var credito = new Credito
                {
                    ClienteId = model.ClienteId,
                    Numero = numeroCredito,
                    MontoSolicitado = model.MontoSolicitado,
                    MontoAprobado = model.MontoSolicitado,
                    TasaInteres = model.TasaInteres,
                    CantidadCuotas = model.CantidadCuotas,
                    MontoCuota = cuotaMensual,
                    CFTEA = cftea,
                    TotalAPagar = totalAPagar,
                    SaldoPendiente = totalAPagar,
                    Estado = model.AprobarConExcepcion ? EstadoCredito.Activo : EstadoCredito.Activo,
                    FechaSolicitud = DateTime.Now,
                    FechaAprobacion = DateTime.Now,
                    FechaPrimeraCuota = DateTime.Now.AddMonths(1),
                    GaranteId = garanteId,
                    RequiereGarante = garanteId.HasValue,
                    AprobadoPor = model.AprobarConExcepcion ? model.AutorizadoPor : "Sistema",
                    Observaciones = model.AprobarConExcepcion
                        ? $"APROBADO CON EXCEPCIÓN: {model.MotivoExcepcion}\n{model.Observaciones}"
                        : model.Observaciones,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.Creditos.Add(credito);
                await _context.SaveChangesAsync();

                // Generar cuotas
                var fechaVencimiento = credito.FechaPrimeraCuota ?? DateTime.Now.AddMonths(1);
                var cuotas = new List<Cuota>();

                for (int i = 1; i <= model.CantidadCuotas; i++)
                {
                    // Calcular interés y capital de esta cuota
                    decimal saldoPendienteAnterior = i == 1
                        ? model.MontoSolicitado
                        : model.MontoSolicitado - cuotas.Take(i - 1).Sum(c => c.MontoCapital);

                    decimal montoInteres = saldoPendienteAnterior * tasaMensualDecimal;
                    decimal montoCapital = cuotaMensual - montoInteres;

                    var cuota = new Cuota
                    {
                        CreditoId = credito.Id,
                        NumeroCuota = i,
                        MontoCapital = montoCapital,
                        MontoInteres = montoInteres,
                        MontoTotal = cuotaMensual,
                        FechaVencimiento = fechaVencimiento,
                        Estado = EstadoCuota.Pendiente,
                        MontoPagado = 0,
                        MontoPunitorio = 0,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsDeleted = false
                    };

                    cuotas.Add(cuota);
                    fechaVencimiento = fechaVencimiento.AddMonths(1);
                }

                _context.Cuotas.AddRange(cuotas);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Crédito {NumeroCredito} creado exitosamente para cliente {ClienteId}. Monto: {Monto}, Cuotas: {Cuotas}",
                    numeroCredito, model.ClienteId, model.MontoSolicitado, model.CantidadCuotas
                );

                TempData["Success"] = $"Crédito {numeroCredito} solicitado exitosamente. Monto: {model.MontoSolicitado:C}, {model.CantidadCuotas} cuotas de {cuotaMensual:C}";
                return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "informacion" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar crédito para cliente {ClienteId}", model.ClienteId);
                TempData["Error"] = $"Error al solicitar el crédito: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = model.ClienteId, tab = "credito" });
            }
        }

        private void CargarDropdowns()
        {
            ViewBag.TiposDocumento = new SelectList(new[] { "DNI", "CUIL", "CUIT" });
            ViewBag.EstadosCiviles = new SelectList(new[] {
                "Soltero/a", "Casado/a", "Divorciado/a", "Viudo/a", "Unión de hecho"
            });
            ViewBag.TiposEmpleo = new SelectList(new[] {
                "Relación de dependencia", "Autónomo", "Monotributista", "Informal"
            });
            ViewBag.Provincias = new SelectList(new[] {
                "Buenos Aires", "CABA", "Catamarca", "Chaco", "Chubut", "Córdoba",
                "Corrientes", "Entre Ríos", "Formosa", "Jujuy", "La Pampa", "La Rioja",
                "Mendoza", "Misiones", "Neuquén", "Río Negro", "Salta", "San Juan",
                "San Luis", "Santa Cruz", "Santa Fe", "Santiago del Estero",
                "Tierra del Fuego", "Tucumán"
            });
        }
    }
}