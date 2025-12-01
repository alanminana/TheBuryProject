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
    [Authorize(Roles = "SuperAdmin,Gerente")]
    public class DocumentoClienteController : Controller
    {
        private readonly IDocumentoClienteService _documentoService;
        private readonly AppDbContext _context;
        private readonly ILogger<DocumentoClienteController> _logger;
        private readonly IDocumentacionService _documentacionService;

        public DocumentoClienteController(
            IDocumentoClienteService documentoService,
            AppDbContext context,
            ILogger<DocumentoClienteController> logger,
            IDocumentacionService documentacionService)
        {
            _documentoService = documentoService;
            _context = context;
            _logger = logger;
            _documentacionService = documentacionService;
        }

        // GET: DocumentoCliente
        public async Task<IActionResult> Index(DocumentoClienteFilterViewModel? filtro, int? returnToVentaId)
        {
            try
            {
                if (filtro == null)
                    filtro = new DocumentoClienteFilterViewModel();

                if (returnToVentaId.HasValue)
                    filtro.ReturnToVentaId = returnToVentaId;

                var (documentos, total) = await _documentoService.BuscarAsync(filtro);
                filtro.Documentos = documentos;
                filtro.TotalResultados = total;

                if (filtro.ReturnToVentaId.HasValue)
                {
                    var venta = await _context.Ventas.FindAsync(filtro.ReturnToVentaId.Value);
                    if (venta != null)
                    {
                        ViewBag.DocumentacionPendiente =
                            await _documentoService.ValidarDocumentacionObligatoriaAsync(venta.ClienteId);
                    }
                }

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
        public async Task<IActionResult> Upload(int? clienteId, int? returnToVentaId, int? replaceId)
        {
            var viewModel = new DocumentoClienteViewModel();
            var bloquearCliente = false;

            if (clienteId.HasValue)
                viewModel.ClienteId = clienteId.Value;
            if (returnToVentaId.HasValue)
            {
                viewModel.ReturnToVentaId = returnToVentaId;

                var venta = await _context.Ventas
                    .Include(v => v.Cliente)
                    .FirstOrDefaultAsync(v => v.Id == returnToVentaId.Value);

                if (venta != null)
                {
                    viewModel.ClienteId = venta.ClienteId;
                    viewModel.ClienteNombre = $"{venta.Cliente.Apellido}, {venta.Cliente.Nombre} - DNI: {venta.Cliente.NumeroDocumento}";
                    bloquearCliente = true;
                }
            }

            if (replaceId.HasValue)
            {
                var documento = await _documentoService.GetByIdAsync(replaceId.Value);
                if (documento != null)
                {
                    viewModel.DocumentoAReemplazarId = documento.Id;
                    viewModel.ReemplazarExistente = true;
                    viewModel.DocumentoAReemplazarNombre = documento.NombreArchivo;
                    viewModel.ClienteId = documento.ClienteId;
                    viewModel.TipoDocumento = documento.TipoDocumento;
                    await CargarViewBags(documento.ClienteId, false);
                }
            }

            ViewBag.ClienteBloqueado = bloquearCliente;
            await CargarViewBags(viewModel.ClienteId, bloquearCliente);

            if (!string.IsNullOrWhiteSpace(viewModel.ClienteNombre))
            {
                return View(viewModel);
            }

            if (viewModel.ClienteId > 0)
            {
                var cliente = await _context.Clientes.FindAsync(viewModel.ClienteId);
                if (cliente != null)
                {
                    viewModel.ClienteNombre = $"{cliente.Apellido}, {cliente.Nombre} - DNI: {cliente.NumeroDocumento}";
                }
            }

            return View(viewModel);
        }

        // POST: DocumentoCliente/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(DocumentoClienteViewModel viewModel, bool returnToDetails = false)
        {
            try
            {
                if (viewModel.ReturnToVentaId.HasValue)
                {
                    var venta = await _context.Ventas
                        .Include(v => v.Cliente)
                        .FirstOrDefaultAsync(v => v.Id == viewModel.ReturnToVentaId.Value);

                    if (venta == null)
                    {
                        ModelState.AddModelError("", "No se encontró la venta asociada al crédito.");
                    }
                    else if (venta.ClienteId != viewModel.ClienteId)
                    {
                        ModelState.AddModelError("ClienteId", "Debe adjuntar documentación para el cliente seleccionado en la venta.");
                        viewModel.ClienteId = venta.ClienteId;
                        viewModel.ClienteNombre = $"{venta.Cliente.Apellido}, {venta.Cliente.Nombre} - DNI: {venta.Cliente.NumeroDocumento}";
                    }
                    else
                    {
                        viewModel.ClienteNombre = $"{venta.Cliente.Apellido}, {venta.Cliente.Nombre} - DNI: {venta.Cliente.NumeroDocumento}";
                    }
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ClienteBloqueado = viewModel.ReturnToVentaId.HasValue;
                    await CargarViewBags(viewModel.ClienteId, viewModel.ReturnToVentaId.HasValue);

                    // Si viene del inline upload, redirigir con error
                    if (returnToDetails)
                    {
                        TempData["Error"] = "Por favor corrija los errores en el formulario";
                        return RedirectToAction("Details", "Cliente", new { id = viewModel.ClienteId, tab = "documentos" });
                    }

                    return View(viewModel);
                }

                var resultado = await _documentoService.UploadAsync(viewModel);

                TempData["Success"] = $"Documento '{resultado.TipoDocumentoNombre}' subido exitosamente";

                if (viewModel.ReturnToVentaId.HasValue)
                {
                    var estado = await _documentacionService.ProcesarDocumentacionVentaAsync(viewModel.ReturnToVentaId.Value);

                    if (!estado.DocumentacionCompleta)
                    {
                        TempData["Warning"] =
                            $"Falta documentación obligatoria para otorgar crédito: {estado.MensajeFaltantes}";

                        return RedirectToAction(nameof(Index), new
                        {
                            clienteId = viewModel.ClienteId,
                            returnToVentaId = viewModel.ReturnToVentaId
                        });
                    }

                    TempData["Info"] = estado.CreditoCreado
                        ? "Documentación completa. Crédito generado para esta venta."
                        : "Documentación completa. Crédito listo para configurar.";

                    return RedirectToAction(
                        "ConfigurarVenta",
                        "Credito",
                        new { id = estado.CreditoId, ventaId = viewModel.ReturnToVentaId });
                }

                // Si viene del upload inline, redirigir a Cliente/Details con tab documentos
                if (returnToDetails)
                {
                    return RedirectToAction("Details", "Cliente", new { id = viewModel.ClienteId, tab = "documentos" });
                }

                // Redirigir al índice de documentos filtrado por el cliente
                return RedirectToAction(nameof(Index), new { clienteId = viewModel.ClienteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir documento");

                TempData["Error"] = "Error al subir documento: " + ex.Message;

                // Si viene del inline upload, redirigir al tab documentos
                if (returnToDetails)
                {
                    return RedirectToAction("Details", "Cliente", new { id = viewModel.ClienteId, tab = "documentos" });
                }

                ModelState.AddModelError("", "Error al subir documento: " + ex.Message);
                ViewBag.ClienteBloqueado = viewModel.ReturnToVentaId.HasValue;
                await CargarViewBags(viewModel.ClienteId, viewModel.ReturnToVentaId.HasValue);
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
                // CAMBIO: Capturar usuario actual en lugar de hardcodear "System"
                var usuario = User.Identity?.Name ?? "Sistema";
                var resultado = await _documentoService.VerificarAsync(id, usuario, observaciones);

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

                // CAMBIO: Capturar usuario actual en lugar de hardcodear "System"
                var usuario = User.Identity?.Name ?? "Sistema";
                var resultado = await _documentoService.RechazarAsync(id, motivo, usuario);

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

        private async Task CargarViewBags(int? clienteIdSeleccionado = null, bool limitarAClienteSeleccionado = false)
        {
            var clientesQuery = _context.Clientes
                .Where(c => !c.IsDeleted && c.Activo);

            if (limitarAClienteSeleccionado && clienteIdSeleccionado.HasValue)
            {
                clientesQuery = clientesQuery.Where(c => c.Id == clienteIdSeleccionado.Value);
            }

            var clientes = await clientesQuery
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