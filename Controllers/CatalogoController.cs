using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    public class BaseEntityController : Controller
    {
        private readonly ICategoriaService _categoriaService;
        private readonly IMarcaService _marcaService;
        private readonly ILogger<BaseEntityController> _logger;

        public BaseEntityController(
            ICategoriaService categoriaService,
            IMarcaService marcaService,
            ILogger<BaseEntityController> logger)
        {
            _categoriaService = categoriaService;
            _marcaService = marcaService;
            _logger = logger;
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
                    Categorias = categorias.Select(c => new CategoriaViewModel
                    {
                        Id = c.Id,
                        Codigo = c.Codigo,
                        Nombre = c.Nombre,
                        Descripcion = c.Descripcion,
                        ParentId = c.ParentId,
                        ParentNombre = c.Parent?.Nombre,
                        ControlSerieDefault = c.ControlSerieDefault
                    }),
                    Marcas = marcas.Select(m => new MarcaViewModel
                    {
                        Id = m.Id,
                        Codigo = m.Codigo,
                        Nombre = m.Nombre,
                        Descripcion = m.Descripcion,
                        ParentId = m.ParentId,
                        ParentNombre = m.Parent?.Nombre,
                        PaisOrigen = m.PaisOrigen
                    })
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
