using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    public class ProductoController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly ICategoriaService _categoriaService;
        private readonly IMarcaService _marcaService;
        private readonly ILogger<ProductoController> _logger;
        private readonly IMapper _mapper;

        public ProductoController(
            IProductoService productoService,
            ICategoriaService categoriaService,
            IMarcaService marcaService,
            ILogger<ProductoController> logger,
            IMapper mapper)
        {
            _productoService = productoService;
            _categoriaService = categoriaService;
            _marcaService = marcaService;
            _logger = logger;
            _mapper = mapper;
        }

        // GET: Producto
        public async Task<IActionResult> Index()
        {
            try
            {
                var productos = await _productoService.GetAllAsync();
                var viewModels = _mapper.Map<IEnumerable<ProductoViewModel>>(productos);
                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener listado de productos");
                TempData["Error"] = "Error al cargar los productos. Por favor, intente nuevamente.";
                return View(new List<ProductoViewModel>());
            }
        }

        // GET: Producto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var producto = await _productoService.GetByIdAsync(id.Value);
                if (producto == null)
                {
                    return NotFound();
                }

                var viewModel = _mapper.Map<ProductoViewModel>(producto);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del producto {Id}", id);
                TempData["Error"] = "Error al cargar los detalles del producto. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Producto/Create
        public async Task<IActionResult> Create()
        {
            await CargarDropdownsAsync();
            return View();
        }

        // POST: Producto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductoViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar que el código no exista
                    if (await _productoService.ExistsCodigoAsync(viewModel.Codigo))
                    {
                        ModelState.AddModelError("Codigo", "Ya existe un producto con este código");
                        await CargarDropdownsAsync(viewModel.CategoriaId, viewModel.MarcaId);
                        return View(viewModel);
                    }

                    var producto = _mapper.Map<Producto>(viewModel);
                    await _productoService.CreateAsync(producto);

                    TempData["Success"] = "Producto creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Error de validación al crear producto");
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear producto");
                    ModelState.AddModelError("", "Error al crear el producto. Por favor, intente nuevamente.");
                }
            }

            await CargarDropdownsAsync(viewModel.CategoriaId, viewModel.MarcaId);
            return View(viewModel);
        }

        // GET: Producto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var producto = await _productoService.GetByIdAsync(id.Value);
                if (producto == null)
                {
                    return NotFound();
                }

                var viewModel = _mapper.Map<ProductoViewModel>(producto);
                await CargarDropdownsAsync(viewModel.CategoriaId, viewModel.MarcaId);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar producto para editar {Id}", id);
                TempData["Error"] = "Error al cargar el producto. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Producto/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductoViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar que el código no exista en otro producto
                    if (await _productoService.ExistsCodigoAsync(viewModel.Codigo, id))
                    {
                        ModelState.AddModelError("Codigo", "Ya existe otro producto con este código");
                        await CargarDropdownsAsync(viewModel.CategoriaId, viewModel.MarcaId);
                        return View(viewModel);
                    }

                    var producto = _mapper.Map<Producto>(viewModel);
                    await _productoService.UpdateAsync(producto);

                    TempData["Success"] = "Producto actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Error de validación al actualizar producto {Id}", id);
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar producto {Id}", id);
                    ModelState.AddModelError("", "Error al actualizar el producto. Por favor, intente nuevamente.");
                }
            }

            await CargarDropdownsAsync(viewModel.CategoriaId, viewModel.MarcaId);
            return View(viewModel);
        }

        // GET: Producto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var producto = await _productoService.GetByIdAsync(id.Value);
                if (producto == null)
                {
                    return NotFound();
                }

                var viewModel = _mapper.Map<ProductoViewModel>(producto);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar producto para eliminar {Id}", id);
                TempData["Error"] = "Error al cargar el producto. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Producto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _productoService.DeleteAsync(id);
                if (result)
                {
                    TempData["Success"] = "Producto eliminado exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se encontró el producto a eliminar";
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación al eliminar producto {Id}", id);
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto {Id}", id);
                TempData["Error"] = "Error al eliminar el producto. Por favor, intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Carga los dropdowns de Categorías y Marcas para los formularios
        /// </summary>
        private async Task CargarDropdownsAsync(int? categoriaSeleccionada = null, int? marcaSeleccionada = null)
        {
            var categorias = await _categoriaService.GetAllAsync();
            var marcas = await _marcaService.GetAllAsync();

            ViewBag.Categorias = new SelectList(
                categorias.OrderBy(c => c.Nombre),
                "Id",
                "Nombre",
                categoriaSeleccionada
            );

            ViewBag.Marcas = new SelectList(
                marcas.OrderBy(m => m.Nombre),
                "Id",
                "Nombre",
                marcaSeleccionada
            );
        }
    }
}