using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly ICategoriaService _categoriaService;
        private readonly IMarcaService _marcaService;
        private readonly ILogger<CatalogoController> _logger;  // ✅ AGREGAR ESTA LÍNEA

        public CatalogoController(
            ICategoriaService categoriaService,
            IMarcaService marcaService,
            ILogger<CatalogoController> logger)  // ✅ CAMBIAR BaseEntityController por CatalogoController
        {
            _categoriaService = categoriaService;
            _marcaService = marcaService;
            _logger = logger;  // ✅ Ahora sí puede asignar porque existe el campo
        }

        // GET: Catalogo
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            bool soloActivos = false,
            string? orderBy = null,
            string? orderDirection = "asc")
        {
            try
            {
                // Ejecutar búsqueda con filtros
                var categorias = await _categoriaService.SearchAsync(
                    searchTerm,
                    soloActivos,
                    orderBy,
                    orderDirection
                );

                var viewModels = _mapper.Map<IEnumerable<CategoriaViewModel>>(categorias);

                // Crear ViewModel de filtros
                var filterViewModel = new CategoriaFilterViewModel
                {
                    SearchTerm = searchTerm,
                    SoloActivos = soloActivos,
                    OrderBy = orderBy,
                    OrderDirection = orderDirection,
                    Categorias = viewModels,
                    TotalResultados = viewModels.Count()
                };

                return View(filterViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener listado de categorías");
                TempData["Error"] = "Error al cargar las categorías. Por favor, intente nuevamente.";
                return View(new CategoriaFilterViewModel());
            }
        }
    }
}