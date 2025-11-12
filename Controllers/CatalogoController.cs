using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize(Roles = "Admin,Gerente,Vendedor")]
    public class CatalogoController : Controller
    {
        private readonly ICategoriaService _categoriaService;
        private readonly IMarcaService _marcaService;
        private readonly ILogger<CatalogoController> _logger;
        private readonly IMapper _mapper;

        public CatalogoController(
            ICategoriaService categoriaService,
            IMarcaService marcaService,
            ILogger<CatalogoController> logger,
            IMapper mapper)
        {
            _categoriaService = categoriaService;
            _marcaService = marcaService;
            _logger = logger;
            _mapper = mapper;
        }

        // GET: Catalogo
        public async Task<IActionResult> Index()
        {
            try
            {
                var categorias = await _categoriaService.GetAllAsync();
                var marcas = await _marcaService.GetAllAsync();

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