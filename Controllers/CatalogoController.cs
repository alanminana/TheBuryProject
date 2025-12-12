using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Models.Constants;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente + "," + Roles.Vendedor)]
    public class CatalogoController : Controller
    {
        private readonly ICatalogLookupService _catalogLookupService;
        private readonly ILogger<CatalogoController> _logger;
        private readonly IMapper _mapper;

        public CatalogoController(
            ICatalogLookupService catalogLookupService,
            ILogger<CatalogoController> logger,
            IMapper mapper)
        {
            _catalogLookupService = catalogLookupService;
            _logger = logger;
            _mapper = mapper;
        }

        // GET: Catalogo
        public async Task<IActionResult> Index()
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
                _logger.LogError(ex, "Error al obtener catálogo");
                TempData["Error"] = "Error al cargar el catálogo";
                return View(new CatalogoViewModel());
            }
        }
    }
}