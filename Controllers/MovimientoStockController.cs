using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize(Roles = "SuperAdmin,Gerente")]
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

                var productos = await _productoService.GetAllAsync();
                ViewBag.Productos = new SelectList(productos.OrderBy(p => p.Nombre), "Id", "Nombre", filter.ProductoId);
                ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoMovimiento))); // mantiene coherencia con la vista

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

        // =========================
        //  NUEVO/ACTUALIZADO: AJUSTES
        // =========================

        // GET: MovimientoStock/Create
        public async Task<IActionResult> Create(int? productoId)
        {
            try
            {
                var viewModel = new AjusteStockViewModel();

                if (productoId.HasValue)
                {
                    var producto = await _productoService.GetByIdAsync(productoId.Value);
                    if (producto != null)
                    {
                        viewModel.ProductoId = producto.Id;
                        viewModel.ProductoNombre = producto.Nombre;
                        viewModel.ProductoCodigo = producto.Codigo;
                        viewModel.StockActual = producto.StockActual;
                    }
                }

                var productos = await _productoService.GetAllAsync();
                ViewBag.Productos = new SelectList(
                    productos.Where(p => p.Activo).OrderBy(p => p.Nombre),
                    "Id",
                    "Nombre",
                    productoId);

                // NUEVO: llenar tipos para el <select> del formulario
                ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoMovimiento)));

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el formulario de ajuste de stock");
                TempData["Error"] = "Error al cargar el formulario";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: MovimientoStock/Create
        [HttpPost]
        [ValidateAntiForgeryToken] // protege el POST con anti-CSRF
        public async Task<IActionResult> Create(AjusteStockViewModel viewModel)
        {
            try
            {
                // Validación de modelo
                if (!ModelState.IsValid)
                {
                    var productosInvalid = await _productoService.GetAllAsync();
                    ViewBag.Productos = new SelectList(
                        productosInvalid.Where(p => p.Activo).OrderBy(p => p.Nombre),
                        "Id",
                        "Nombre",
                        viewModel.ProductoId);

                    // RE-llenar tipos al volver a la vista con errores
                    ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoMovimiento)));

                    return View(viewModel);
                }

                // Validación de existencia de producto antes de registrar
                var producto = await _productoService.GetByIdAsync(viewModel.ProductoId);
                if (producto == null)
                {
                    ModelState.AddModelError(nameof(viewModel.ProductoId), "Producto inexistente.");
                    var productosMissing = await _productoService.GetAllAsync();
                    ViewBag.Productos = new SelectList(
                        productosMissing.Where(p => p.Activo).OrderBy(p => p.Nombre),
                        "Id",
                        "Nombre",
                        viewModel.ProductoId);
                    ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoMovimiento)));
                    return View(viewModel);
                }

                // Registrar el ajuste en servicio de dominio
                await _movimientoStockService.RegistrarAjusteAsync(
                    viewModel.ProductoId,
                    viewModel.Tipo,
                    viewModel.Cantidad,
                    viewModel.Referencia,
                    viewModel.Motivo);

                TempData["Success"] = "Ajuste de stock registrado exitosamente";
                return RedirectToAction(nameof(Kardex), new { id = viewModel.ProductoId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var productos = await _productoService.GetAllAsync();
                ViewBag.Productos = new SelectList(
                    productos.Where(p => p.Activo).OrderBy(p => p.Nombre),
                    "Id",
                    "Nombre",
                    viewModel.ProductoId);
                ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoMovimiento)));
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar ajuste de stock");
                ModelState.AddModelError("", "Error al registrar el ajuste de stock");
                var productos = await _productoService.GetAllAsync();
                ViewBag.Productos = new SelectList(
                    productos.Where(p => p.Activo).OrderBy(p => p.Nombre),
                    "Id",
                    "Nombre",
                    viewModel.ProductoId);
                ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoMovimiento)));
                return View(viewModel);
            }
        }

        // GET API: MovimientoStock/GetProductoInfo/5
        [HttpGet]
        [Produces("application/json")] // negociación de contenido JSON
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // evita cachear stock
        public async Task<IActionResult> GetProductoInfo(int id)
        {
            try
            {
                var producto = await _productoService.GetByIdAsync(id);
                if (producto == null)
                {
                    return NotFound();
                }

                // Respuesta 200 OK con JSON tipado
                return Ok(new
                {
                    id = producto.Id,
                    nombre = producto.Nombre,
                    codigo = producto.Codigo,
                    stockActual = producto.StockActual,
                    stockMinimo = producto.StockMinimo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener info del producto {ProductoId}", id);
                return BadRequest("Error al obtener información del producto");
            }
        }
    }
}