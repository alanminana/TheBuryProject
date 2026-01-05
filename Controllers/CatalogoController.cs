using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Models.Constants;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
        /// <summary>
        /// Aplica un cambio directo de precio a productos seleccionados o filtrados desde el catálogo.
        /// Actualiza Producto.PrecioVenta, crea historial y permite revertir.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
        public async Task<IActionResult> AplicarCambioPrecioDirecto([FromBody] AplicarCambioPrecioDirectoViewModel model)
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
{
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente + "," + Roles.Vendedor)]
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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
        public async Task<IActionResult> SimularCambioPrecios([FromBody] SolicitudSimulacionPrecios solicitud)
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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
        public async Task<IActionResult> AplicarCambioPrecios([FromBody] SolicitudAplicarPrecios solicitud)
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
    }
}