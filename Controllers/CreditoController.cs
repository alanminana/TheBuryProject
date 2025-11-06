using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize]
    public class CreditoController : Controller
    {
        private readonly ICreditoService _creditoService;
        private readonly AppDbContext _context;
        private readonly ILogger<CreditoController> _logger;

        public CreditoController(ICreditoService creditoService, AppDbContext context, ILogger<CreditoController> logger)
        {
            _creditoService = creditoService;
            _context = context;
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

                return View(credito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del crédito: {Id}", id);
                TempData["Error"] = "Error al cargar el crédito";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Credito/Create
        public async Task<IActionResult> Create()
        {
            await CargarViewBags();
            return View(new CreditoViewModel
            {
                FechaSolicitud = DateTime.Now,
                TasaInteres = 5.0m, // Tasa por defecto 5%
                CantidadCuotas = 12 // 12 cuotas por defecto
            });
        }

        // POST: Credito/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditoViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarViewBags(viewModel.ClienteId, viewModel.GaranteId);
                    return View(viewModel);
                }

                var credito = await _creditoService.CreateAsync(viewModel);
                TempData["Success"] = $"Crédito {credito.Numero} creado exitosamente";
                return RedirectToAction(nameof(Details), new { id = credito.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear crédito");
                ModelState.AddModelError("", "Error al crear el crédito: " + ex.Message);
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

                // Solo se puede editar si está en Solicitado
                if (credito.Estado != Models.Enums.EstadoCredito.Solicitado)
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
                    TempData["Success"] = "Crédito actualizado exitosamente";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["Error"] = "No se pudo actualizar el crédito";
                    return RedirectToAction(nameof(Index));
                }
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
                {
                    TempData["Success"] = "Crédito eliminado exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el crédito";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar crédito: {Id}", id);
                TempData["Error"] = "Error al eliminar el crédito: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Credito/Simular
        public IActionResult Simular()
        {
            return View(new SimularCreditoViewModel
            {
                TasaInteres = 5.0m,
                CantidadCuotas = 12,
                FechaPrimeraCuota = DateTime.Now.AddMonths(1)
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
                _logger.LogError(ex, "Error al simular crédito");
                ModelState.AddModelError("", "Error al simular el crédito: " + ex.Message);
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
                    TempData["Success"] = "Crédito aprobado exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo aprobar el crédito";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aprobar crédito: {Id}", id);
                TempData["Error"] = "Error al aprobar el crédito: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Credito/Rechazar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["Error"] = "Debe indicar el motivo del rechazo";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var resultado = await _creditoService.RechazarCreditoAsync(id, motivo);

                if (resultado)
                {
                    TempData["Success"] = "Crédito rechazado";
                }
                else
                {
                    TempData["Error"] = "No se pudo rechazar el crédito";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar crédito: {Id}", id);
                TempData["Error"] = "Error al rechazar el crédito: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Credito/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["Error"] = "Debe indicar el motivo de la cancelación";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var resultado = await _creditoService.CancelarCreditoAsync(id, motivo);

                if (resultado)
                {
                    TempData["Success"] = "Crédito cancelado";
                }
                else
                {
                    TempData["Error"] = "No se pudo cancelar el crédito";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar crédito: {Id}", id);
                TempData["Error"] = "Error al cancelar el crédito: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Credito/PagarCuota/5
        public async Task<IActionResult> PagarCuota(int id)
        {
            try
            {
                var cuota = await _creditoService.GetCuotaByIdAsync(id);
                if (cuota == null)
                {
                    TempData["Error"] = "Cuota no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                var modelo = new PagarCuotaViewModel
                {
                    CuotaId = cuota.Id,
                    NumeroCuota = cuota.NumeroCuota,
                    MontoCuota = cuota.MontoTotal,
                    MontoPunitorio = cuota.MontoPunitorio,
                    TotalAPagar = cuota.SaldoPendiente,
                    MontoPagado = cuota.SaldoPendiente,
                    FechaPago = DateTime.Now,
                    FechaVencimiento = cuota.FechaVencimiento,
                    EstaVencida = cuota.EstaVencida,
                    DiasAtraso = cuota.DiasAtraso
                };

                return View(modelo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cuota para pagar: {Id}", id);
                TempData["Error"] = "Error al cargar la cuota";
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
                    return View(modelo);
                }

                var resultado = await _creditoService.PagarCuotaAsync(modelo);

                if (resultado)
                {
                    TempData["Success"] = "Pago registrado exitosamente";

                    // Obtener el crédito asociado para redirigir a detalles
                    var cuota = await _context.Cuotas.FindAsync(modelo.CuotaId);
                    if (cuota != null)
                    {
                        return RedirectToAction(nameof(Details), new { id = cuota.CreditoId });
                    }
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
                return View(modelo);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Credito/CuotasVencidas
        public async Task<IActionResult> CuotasVencidas()
        {
            try
            {
                await _creditoService.ActualizarEstadoCuotasAsync();
                var cuotas = await _creditoService.GetCuotasVencidasAsync();
                return View(cuotas);
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

            var garantes = await _context.Garantes
                .OrderBy(g => g.Apellido)
                .ThenBy(g => g.Nombre)
                .Select(g => new
                {
                    g.Id,
                    NombreCompleto = $"{g.Apellido}, {g.Nombre} - DNI: {g.NumeroDocumento}"
                })
                .ToListAsync();

            ViewBag.Garantes = new SelectList(garantes, "Id", "NombreCompleto", garanteIdSeleccionado);
        }

        #endregion
    }
}
