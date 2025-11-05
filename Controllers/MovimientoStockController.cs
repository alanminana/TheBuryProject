using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [AllowAnonymous]
    public class MovimientoStockController : Controller
    {
        private readonly IMovimientoStockService _movimientoStockService;
        private readonly IProductoService _productoService;
        private readonly IMapper _mapper;
        private readonly ILogger<MovimientoStockController> _logger;

        public MovimientoStockController(
            IMovimientoStockService movimientoStockService,
            IProductoService productoService,
            IMapper mapper,
            ILogger<MovimientoStockController> logger)
        {
            _movimientoStockService = movimientoStockService;
            _productoService = productoService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: MovimientoStock
        public async Task<IActionResult> Index(MovimientoStockFilterViewModel filter)
        {
            try
            {
                var movimientos = await _movimientoStockService.SearchAsync(
                    productoId: filter.ProductoId,
                    tipo: filter.Tipo,
                    fechaDesde: filter.FechaDesde,
                    fechaHasta: filter.FechaHasta,
                    orderBy: filter.OrderBy,
                    orderDirection: filter.OrderDirection);

                var viewModels = _mapper.Map<IEnumerable<MovimientoStockViewModel>>(movimientos);

                filter.Movimientos = viewModels;
                filter.TotalResultados = viewModels.Count();

                // Cargar productos para el filtro
                var productos = await _productoService.GetAllAsync();
                ViewBag.Productos = new SelectList(productos.OrderBy(p => p.Nombre), "Id", "Nombre", filter.ProductoId);
                ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoMovimiento)));

                return View(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener movimientos de stock");
                TempData["Error"] = "Error al cargar los movimientos de stock";
                return View(new MovimientoStockFilterViewModel());
            }
        }

        // GET: MovimientoStock/Kardex/5
        public async Task<IActionResult> Kardex(int id)
        {
            try
            {
                var producto = await _productoService.GetByIdAsync(id);
                if (producto == null)
                {
                    TempData["Error"] = "Producto no encontrado";
                    return RedirectToAction("Index", "Producto");
                }

                var movimientos = await _movimientoStockService.GetByProductoIdAsync(id);
                var viewModels = _mapper.Map<IEnumerable<MovimientoStockViewModel>>(movimientos);

                ViewBag.Producto = producto;
                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener kardex del producto {ProductoId}", id);
                TempData["Error"] = "Error al cargar el kardex";
                return RedirectToAction("Index", "Producto");
            }
        }
    }
}