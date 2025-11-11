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
    public class DocumentoClienteController : Controller
    {
        private readonly IDocumentoClienteService _documentoService;
        private readonly AppDbContext _context;
        private readonly ILogger<DocumentoClienteController> _logger;

        public DocumentoClienteController(
            IDocumentoClienteService documentoService,
            AppDbContext context,
            ILogger<DocumentoClienteController> logger)
        {
            _documentoService = documentoService;
            _context = context;
            _logger = logger;
        }

        // GET: DocumentoCliente
        public async Task<IActionResult> Index(DocumentoClienteFilterViewModel? filtro)
        {
            try
            {
                if (filtro == null)
                    filtro = new DocumentoClienteFilterViewModel();

                filtro.Documentos = await _documentoService.BuscarAsync(filtro);

                await CargarViewBags(filtro.ClienteId);

                return View(filtro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar documentos");
                TempData["Error"] = $"Error al cargar los documentos: {ex.Message}";

                var emptyModel = new DocumentoClienteFilterViewModel();
                await CargarViewBags(null);
                return View(emptyModel);
            }
        }

        // GET: DocumentoCliente/Upload
        public async Task<IActionResult> Upload(int? clienteId)
        {
            await CargarViewBags(clienteId);

            var viewModel = new DocumentoClienteViewModel();
            if (clienteId.HasValue)
                viewModel.ClienteId = clienteId.Value;

            return View(viewModel);
        }

        // POST: DocumentoCliente/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(DocumentoClienteViewModel viewModel, bool returnToDetails = false)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarViewBags(viewModel.ClienteId);

                    // Si viene del inline upload, redirigir con error
                    if (returnToDetails)
                    {
                        TempData["Error"] = "Por favor corrija los errores en el formulario";
                        return RedirectToAction("Details", "Cliente", new { id = viewModel.ClienteId, tab = "documentacion" });
                    }

                    return View(viewModel);
                }

                var resultado = await _documentoService.UploadAsync(viewModel);

                TempData["Success"] = $"Documento '{resultado.TipoDocumentoNombre}' subido exitosamente";

                // Si viene del upload inline, redirigir a Cliente/Details con tab documentacion
                if (returnToDetails)
                {
                    return RedirectToAction("Details", "Cliente", new { id = viewModel.ClienteId, tab = "documentacion" });
                }

                // Redirigir al índice de documentos filtrado por el cliente
                return RedirectToAction(nameof(Index), new { clienteId = viewModel.ClienteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir documento");

                TempData["Error"] = "Error al subir documento: " + ex.Message;

                // Si viene del inline upload, redirigir al tab documentacion
                if (returnToDetails)
                {
                    return RedirectToAction("Details", "Cliente", new { id = viewModel.ClienteId, tab = "documentacion" });
                }

                ModelState.AddModelError("", "Error al subir documento: " + ex.Message);
                await CargarViewBags(viewModel.ClienteId);
                return View(viewModel);
            }
        }

        // GET: DocumentoCliente/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var documento = await _documentoService.GetByIdAsync(id);
                if (documento == null)
                {
                    TempData["Error"] = "Documento no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(documento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documento {Id}", id);
                TempData["Error"] = "Error al cargar el documento";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: DocumentoCliente/Verificar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verificar(int id, string? observaciones)
        {
            try
            {
                var resultado = await _documentoService.VerificarAsync(id, "System", observaciones);

                if (resultado)
                    TempData["Success"] = "Documento verificado exitosamente";
                else
                    TempData["Error"] = "No se pudo verificar el documento";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar documento {Id}", id);
                TempData["Error"] = "Error al verificar el documento";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: DocumentoCliente/Rechazar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["Error"] = "Debe especificar el motivo del rechazo";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var resultado = await _documentoService.RechazarAsync(id, motivo, "System");

                if (resultado)
                    TempData["Success"] = "Documento rechazado";
                else
                    TempData["Error"] = "No se pudo rechazar el documento";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar documento {Id}", id);
                TempData["Error"] = "Error al rechazar el documento";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: DocumentoCliente/Descargar/5
        public async Task<IActionResult> Descargar(int id)
        {
            try
            {
                var documento = await _documentoService.GetByIdAsync(id);
                if (documento == null)
                {
                    TempData["Error"] = "Documento no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var bytes = await _documentoService.DescargarArchivoAsync(id);
                return File(bytes, documento.TipoMIME ?? "application/octet-stream", documento.NombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar documento {Id}", id);
                TempData["Error"] = "Error al descargar el documento";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: DocumentoCliente/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var documento = await _documentoService.GetByIdAsync(id);
                if (documento == null)
                {
                    TempData["Error"] = "Documento no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var clienteId = documento.ClienteId;

                var resultado = await _documentoService.DeleteAsync(id);

                if (resultado)
                    TempData["Success"] = "Documento eliminado exitosamente";
                else
                    TempData["Error"] = "No se pudo eliminar el documento";

                return RedirectToAction(nameof(Index), new { clienteId = clienteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar documento {Id}", id);
                TempData["Error"] = "Error al eliminar el documento";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task CargarViewBags(int? clienteIdSeleccionado = null)
        {
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

            ViewBag.Clientes = new SelectList(clientes, "Id", "NombreCompleto", clienteIdSeleccionado);

            ViewBag.TiposDocumento = new SelectList(Enum.GetValues(typeof(TipoDocumentoCliente))
                .Cast<TipoDocumentoCliente>()
                .Select(t => new { Value = (int)t, Text = GetTipoDocumentoNombre(t) }), "Value", "Text");

            ViewBag.Estados = new SelectList(Enum.GetValues(typeof(EstadoDocumento))
                .Cast<EstadoDocumento>()
                .Select(e => new { Value = (int)e, Text = e.ToString() }), "Value", "Text");
        }
        // GET: API endpoint para obtener documentos por cliente
        [HttpGet]
        public async Task<IActionResult> GetDocumentosByCliente(int clienteId)
        {
            try
            {
                var documentos = await _documentoService.GetByClienteIdAsync(clienteId);
                return Json(documentos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documentos del cliente {ClienteId}", clienteId);
                return StatusCode(500, new { error = "Error al obtener documentos" });
            }
        }
        private string GetTipoDocumentoNombre(TipoDocumentoCliente tipo)
        {
            return tipo switch
            {
                TipoDocumentoCliente.DNI => "DNI",
                TipoDocumentoCliente.ReciboSueldo => "Recibo de Sueldo",
                TipoDocumentoCliente.ServicioLuz => "Servicio de Luz",
                TipoDocumentoCliente.ServicioGas => "Servicio de Gas",
                TipoDocumentoCliente.ServicioAgua => "Servicio de Agua",
                TipoDocumentoCliente.ConstanciaCUIL => "Constancia CUIL",
                TipoDocumentoCliente.DeclaracionJurada => "Declaración Jurada",
                TipoDocumentoCliente.Veraz => "Veraz",
                TipoDocumentoCliente.Otro => "Otro",
                _ => "Desconocido"
            };
        }
    }
}