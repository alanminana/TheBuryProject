using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Filters;
using TheBuryProject.Models.Constants;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize]
    [PermisoRequerido(Modulo = "cotizaciones", Accion = "view")]
    public class CatalogoController : Controller
    {
        private readonly ICatalogoService _catalogoService;
        private readonly ICatalogLookupService _catalogLookupService;
        private readonly ILogger<CatalogoController> _logger;
        private readonly IMapper _mapper;

        public CatalogoController(
            ICatalogoService catalogoService,
            ICatalogLookupService catalogLookupService,
            ILogger<CatalogoController> logger,
            IMapper mapper)
        {
            _catalogoService = catalogoService;
            _catalogLookupService = catalogLookupService;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Vista unificada del catálogo con productos, filtros y precios.
        /// Usa ICatalogoService.ObtenerCatalogoAsync como único punto de acceso a datos.
        /// </summary>
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            int? categoriaId = null,
            int? marcaId = null,
            bool stockBajo = false,
            bool soloActivos = false,
            string? orderBy = null,
            string? orderDirection = "asc",
            int? listaPrecioId = null)
        {
            try
            {
                // Construir filtros desde parámetros de query
                var filtros = new FiltrosCatalogo
                {
                    TextoBusqueda = searchTerm,
                    CategoriaId = categoriaId,
                    MarcaId = marcaId,
                    SoloStockBajo = stockBajo,
                    SoloActivos = soloActivos,
                    OrdenarPor = orderBy,
                    DireccionOrden = orderDirection,
                    ListaPrecioId = listaPrecioId
                };

                // Obtener datos del servicio (único punto de acceso)
                var resultado = await _catalogoService.ObtenerCatalogoAsync(filtros);

                // Convertir a ViewModel para la vista
                var viewModel = CatalogoUnificadoViewModel.Desde(resultado, filtros);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener catálogo unificado");
                TempData["Error"] = "Error al cargar el catálogo. Por favor, intente nuevamente.";
                return View(new CatalogoUnificadoViewModel());
            }
        }

        /// <summary>
        /// Vista legacy de resumen (categorías y marcas)
        /// </summary>
        [Route("Catalogo/Resumen")]
        public async Task<IActionResult> Resumen()
        {
            try
            {
                var (categorias, marcas) = await _catalogLookupService.GetCategoriasYMarcasAsync();

                var viewModel = new CatalogoViewModel
                {
                    Categorias = _mapper.Map<IEnumerable<CategoriaViewModel>>(categorias),
                    Marcas = _mapper.Map<IEnumerable<MarcaViewModel>>(marcas)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen del catálogo");
                TempData["Error"] = "Error al cargar el catálogo";
                return View(new CatalogoViewModel());
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Acciones masivas de precios
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Simula un cambio masivo de precios sin persistir.
        /// Devuelve preview con Actual/Nuevo/Diferencia para confirmación.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]        public async Task<IActionResult> SimularCambioPrecios([FromBody] SolicitudSimulacionPrecios solicitud)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Datos de solicitud inválidos", detalles = ModelState });
                }

                var resultado = await _catalogoService.SimularCambioPreciosAsync(solicitud);

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al simular cambio de precios");
                return StatusCode(500, new { error = "Error al simular el cambio de precios", mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Aplica el cambio masivo de precios previamente simulado.
        /// Persiste los cambios con auditoría e historial.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]        public async Task<IActionResult> AplicarCambioPrecios([FromBody] SolicitudAplicarPrecios solicitud)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Datos de solicitud inválidos", detalles = ModelState });
                }

                var resultado = await _catalogoService.AplicarCambioPreciosAsync(solicitud);

                if (!resultado.Exitoso)
                {
                    return BadRequest(new { error = resultado.Mensaje });
                }

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar cambio de precios");
                return StatusCode(500, new { error = "Error al aplicar el cambio de precios", mensaje = ex.Message });
            }
        }
        /// <summary>
        /// Aplica un cambio directo de precio a productos seleccionados o filtrados desde el catálogo.
        /// Actualiza Producto.PrecioVenta, crea historial y permite revertir.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]        [Consumes("application/json")]
        [ActionName("AplicarCambioPrecioDirecto")]
        public async Task<IActionResult> AplicarCambioPrecioDirectoJson([FromBody] AplicarCambioPrecioDirectoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Datos de solicitud inválidos", detalles = ModelState });
                }

                // Llama al servicio para aplicar el cambio directo
                var resultado = await _catalogoService.AplicarCambioPrecioDirectoAsync(model);

                if (!resultado.Exitoso)
                {
                    return BadRequest(new { error = resultado.Mensaje });
                }

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar cambio directo de precio");
                return StatusCode(500, new { error = "Error al aplicar el cambio de precio", mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Aplica un cambio directo de precio desde formulario (redirect).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]        [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
        public async Task<IActionResult> AplicarCambioPrecioDirecto(AplicarCambioPrecioDirectoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Datos de solicitud inválidos.";
                    return RedirectToAction(nameof(Index));
                }

                var resultado = await _catalogoService.AplicarCambioPrecioDirectoAsync(model);
                if (!resultado.Exitoso)
                {
                    TempData["Error"] = resultado.Mensaje;
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = resultado.Mensaje;
                if (resultado.CambioPrecioEventoId.HasValue)
                {
                    TempData["SuccessLinkUrl"] = Url.Action(nameof(DetalleCambioPrecio), new { id = resultado.CambioPrecioEventoId.Value });
                    TempData["SuccessLinkText"] = "Ver detalle";
                }
                else
                {
                    TempData["SuccessLinkUrl"] = Url.Action(nameof(HistorialCambiosPrecio));
                    TempData["SuccessLinkText"] = "Ver historial";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar cambio directo de precio (form)");
                TempData["Error"] = "Error al aplicar el cambio de precio.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]        public async Task<IActionResult> HistorialCambiosPrecio()
        {
            try
            {
                var viewModel = await _catalogoService.GetHistorialCambiosPrecioAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar historial de cambios de precio");
                TempData["Error"] = "Error al cargar el historial.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]        public async Task<IActionResult> DetalleCambioPrecio(int id)
        {
            try
            {
                var viewModel = await _catalogoService.GetCambioPrecioDetalleAsync(id);
                if (viewModel == null)
                {
                    TempData["Error"] = "Evento no encontrado.";
                    return RedirectToAction(nameof(HistorialCambiosPrecio));
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalle de cambio de precio {EventoId}", id);
                TempData["Error"] = "Error al cargar el detalle.";
                return RedirectToAction(nameof(HistorialCambiosPrecio));
            }
        }

        [HttpPost("Catalogo/RevertirCambioPrecio/{eventoId:int}")]
        [ValidateAntiForgeryToken]        public async Task<IActionResult> RevertirCambioPrecio(int eventoId)
        {
            try
            {
                var resultado = await _catalogoService.RevertirCambioPrecioAsync(eventoId);
                if (!resultado.Exitoso)
                {
                    TempData["Error"] = resultado.Mensaje;
                    return RedirectToAction(nameof(HistorialCambiosPrecio));
                }

                TempData["Success"] = resultado.Mensaje;
                if (resultado.EventoReversionId.HasValue)
                {
                    TempData["SuccessLinkUrl"] = Url.Action(nameof(DetalleCambioPrecio), new { id = resultado.EventoReversionId.Value });
                    TempData["SuccessLinkText"] = "Ver reversion";
                }

                return RedirectToAction(nameof(HistorialCambiosPrecio));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revertir cambio de precio {EventoId}", eventoId);
                TempData["Error"] = "Error al revertir el cambio.";
                return RedirectToAction(nameof(HistorialCambiosPrecio));
            }
        }
    }
}



