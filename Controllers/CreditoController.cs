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
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [AllowAnonymous]

    //[Authorize(Roles = "Admin,Gerente")]
    public class CreditoController : Controller
    {
        private readonly ICreditoService _creditoService;
        private readonly IEvaluacionCreditoService _evaluacionService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CreditoController> _logger;

        public CreditoController(
            ICreditoService creditoService,
            IEvaluacionCreditoService evaluacionService,
            AppDbContext context,
            IMapper mapper,
            ILogger<CreditoController> logger)
        {
            _creditoService = creditoService;
            _evaluacionService = evaluacionService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
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
                _logger.LogError(ex, "Error al listar cr�ditos");
                TempData["Error"] = "Error al cargar los cr�ditos";
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
                    TempData["Error"] = "Cr�dito no encontrado";
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
                _logger.LogError(ex, "Error al obtener cr�dito {Id}", id);
                TempData["Error"] = "Error al cargar el cr�dito";
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

            if (ventaId.HasValue)
            {
                var venta = await _context.Ventas
                    .Include(v => v.Detalles)
                    .FirstOrDefaultAsync(v => v.Id == ventaId.Value);

                if (venta != null)
                {
                    // Prioriza el total guardado; si no existe, recalcula desde el detalle
                    montoVenta = venta.Total;

                    if (montoVenta <= 0 && venta.Detalles != null && venta.Detalles.Any())
                    {
                        montoVenta = venta.Detalles.Sum(d => d.Subtotal);
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
            {
                return View(modelo);
            }

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
            _logger.LogInformation("=== INICIANDO CREACI�N DE L�NEA DE CR�DITO ===");
            _logger.LogInformation("ClienteId: {ClienteId}", viewModel.ClienteId);
            _logger.LogInformation("MontoSolicitado: {Monto}", viewModel.MontoSolicitado);
            _logger.LogInformation("TasaInteres: {Tasa}", viewModel.TasaInteres);
            _logger.LogInformation("RequiereGarante: {RequiereGarante}", viewModel.RequiereGarante);

            try
            {
                _logger.LogInformation("Validando ModelState...");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState inv�lido. Errores:");
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key]?.Errors;
                        if (errors != null && errors.Count > 0)
                        {
                            foreach (var error in errors)
                            {
                                _logger.LogWarning("  - {Key}: {Error}", key, error.ErrorMessage);
                            }
                        }
                    }

                    await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                    return View(viewModel);
                }

                _logger.LogInformation("ModelState v�lido. Llamando a CreateAsync...");
                var credito = await _creditoService.CreateAsync(viewModel);

                _logger.LogInformation("L�nea de cr�dito creada. Id: {Id}, Numero: {Numero}",
                    credito.Id, credito.Numero);

                TempData["Success"] = $"L�nea de Cr�dito {credito.Numero} creada exitosamente";
                return RedirectToAction(nameof(Details), new { id = credito.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear l�nea de cr�dito. Mensaje: {Message}", ex.Message);
                _logger.LogError("StackTrace: {StackTrace}", ex.StackTrace);

                ModelState.AddModelError("", "Error al crear la l�nea de cr�dito: " + ex.Message);
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
                    TempData["Error"] = "Cr�dito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (credito.Estado != Models.Enums.EstadoCredito.Solicitado)
                {
                    TempData["Error"] = "Solo se pueden editar cr�ditos en estado Solicitado";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await CargarViewBags(credito.ClienteId, credito.GaranteId);
                return View(credito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cr�dito para editar: {Id}", id);
                TempData["Error"] = "Error al cargar el cr�dito";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Credito/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreditoViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                TempData["Error"] = "ID no coincide";
                return RedirectToAction(nameof(Index));
            }

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
                    TempData["Success"] = "Cr�dito actualizado exitosamente";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["Error"] = "No se pudo actualizar el cr�dito";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cr�dito: {Id}", id);
                ModelState.AddModelError("", "Error al actualizar el cr�dito: " + ex.Message);
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
                    TempData["Error"] = "Cr�dito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(credito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cr�dito para eliminar: {Id}", id);
                TempData["Error"] = "Error al cargar el cr�dito";
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
                {
                    TempData["Success"] = "Cr�dito eliminado exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el cr�dito";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cr�dito: {Id}", id);
                TempData["Error"] = "Error al eliminar el cr�dito: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Credito/Simular
        public IActionResult Simular()
        {
            return View(new SimularCreditoViewModel
            {
                TasaInteresMensual = 0.05m,
                CantidadCuotas = 12
            });
        }

        // POST: Credito/Simular
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Simular(SimularCreditoViewModel modelo)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(modelo);
                }

                var resultado = await _creditoService.SimularCreditoAsync(modelo);
                return View(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al simular cr�dito");
                ModelState.AddModelError("", "Error al simular el cr�dito: " + ex.Message);
                return View(modelo);
            }
        }

        // POST: Credito/Aprobar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            try
            {
                var usuario = User.Identity?.Name ?? "Sistema";
                var resultado = await _creditoService.AprobarCreditoAsync(id, usuario);

                if (resultado)
                {
                    TempData["Success"] = "Cr�dito aprobado exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo aprobar el cr�dito";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aprobar cr�dito: {Id}", id);
                TempData["Error"] = "Error al aprobar el cr�dito: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Credito/Rechazar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id)
        {
            try
            {
                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Cr�dito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (credito.Estado != Models.Enums.EstadoCredito.Solicitado)
                {
                    TempData["Error"] = "Solo se pueden rechazar cr�ditos en estado Solicitado";
                    return RedirectToAction(nameof(Details), new { id });
                }

                credito.Estado = Models.Enums.EstadoCredito.Rechazado;
                await _creditoService.UpdateAsync(credito);

                TempData["Success"] = "Cr�dito rechazado";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar cr�dito: {Id}", id);
                TempData["Error"] = "Error al rechazar el cr�dito: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Credito/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Cr�dito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                credito.Estado = Models.Enums.EstadoCredito.Cancelado;
                await _creditoService.UpdateAsync(credito);

                TempData["Success"] = "Cr�dito cancelado";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar cr�dito: {Id}", id);
                TempData["Error"] = "Error al cancelar el cr�dito: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Credito/PagarCuota/5
        public async Task<IActionResult> PagarCuota(int id)
        {
            try
            {
                var credito = await _creditoService.GetByIdAsync(id);
                if (credito == null)
                {
                    TempData["Error"] = "Cr�dito no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var cuotasPendientes = (credito.Cuotas ?? new List<CuotaViewModel>())
                    .Where(c => c.Estado == Models.Enums.EstadoCuota.Pendiente || c.Estado == Models.Enums.EstadoCuota.Vencida)
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
                        TempData["Error"] = "Cr�dito no encontrado";
                        return RedirectToAction(nameof(Index));
                    }

                    var cuotasPendientes = (credito.Cuotas ?? new List<CuotaViewModel>())
                        .Where(c => c.Estado == Models.Enums.EstadoCuota.Pendiente || c.Estado == Models.Enums.EstadoCuota.Vencida)
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
                else
                {
                    TempData["Error"] = "No se pudo registrar el pago";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al pagar cuota");
                ModelState.AddModelError("", "Error al registrar el pago: " + ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: API endpoint para evaluar cr�dito en tiempo real
        [HttpGet]
        public async Task<IActionResult> EvaluarCredito(int clienteId, decimal montoSolicitado, int? garanteId = null)
        {
            try
            {
                _logger.LogInformation("Evaluando cr�dito para cliente {ClienteId}, monto {Monto}", clienteId, montoSolicitado);

                var evaluacion = await _evaluacionService.EvaluarSolicitudAsync(clienteId, montoSolicitado, garanteId);

                return Json(evaluacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar cr�dito");
                return StatusCode(500, new { error = "Error al evaluar cr�dito: " + ex.Message });
            }
        }

        // GET: Credito/CuotasVencidas
        public async Task<IActionResult> CuotasVencidas()
        {
            try
            {
                var cuotas = await _context.Cuotas
                    .Include(c => c.Credito)
                        .ThenInclude(cr => cr.Cliente)
                    .Where(c => c.Estado == Models.Enums.EstadoCuota.Vencida ||
                               (c.Estado == Models.Enums.EstadoCuota.Pendiente && c.FechaVencimiento < DateTime.Today))
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

        #region M�todos Privados

        private async Task CargarViewBags(int? clienteIdSeleccionado = null, int? garanteIdSeleccionado = null)
        {
            _logger.LogInformation("Cargando ViewBags...");

            var clientes = await _context.Clientes
                .Where(c => !c.IsDeleted && c.Activo)
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    NombreCompleto = $"{c.Apellido}, {c.Nombre} - DNI: {c.NumeroDocumento}"
                })
                .ToListAsync();

            _logger.LogInformation("Clientes cargados: {Count}", clientes.Count);
            ViewBag.Clientes = new SelectList(clientes, "Id", "NombreCompleto", clienteIdSeleccionado);

            var garantes = await _context.Clientes
                .Where(c => !c.IsDeleted && c.Activo)
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    NombreCompleto = $"{c.Apellido}, {c.Nombre} - DNI: {c.NumeroDocumento}"
                })
                .ToListAsync();

            _logger.LogInformation("Garantes cargados: {Count}", garantes.Count);
            ViewBag.Garantes = new SelectList(garantes, "Id", "NombreCompleto", garanteIdSeleccionado);
        }
        #endregion
    }
}