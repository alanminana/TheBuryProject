using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
    public class ProductoController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly ICatalogLookupService _catalogLookupService;
        private readonly ILogger<ProductoController> _logger;
        private readonly IMapper _mapper;

        public ProductoController(
            IProductoService productoService,
            ICatalogLookupService catalogLookupService,
            ILogger<ProductoController> logger,
            IMapper mapper)
        {
            _productoService = productoService;
            _catalogLookupService = catalogLookupService;
            _logger = logger;
            _mapper = mapper;
        }

        // GET: Producto
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            int? categoriaId = null,
            int? marcaId = null,
            bool stockBajo = false,
            bool soloActivos = false,
            string? orderBy = null,
            string? orderDirection = "asc")
        {
            try
            {
                // Ejecutar b�squeda con filtros
                var productos = await _productoService.SearchAsync(
                    searchTerm,
                    categoriaId,
                    marcaId,
                    stockBajo,
                    soloActivos,
                    orderBy,
                    orderDirection
                );

                var viewModels = _mapper.Map<List<ProductoViewModel>>(productos);

                // Crear ViewModel de filtros
                var filterViewModel = new ProductoFilterViewModel
                {
                    SearchTerm = searchTerm,
                    CategoriaId = categoriaId,
                    MarcaId = marcaId,
                    StockBajo = stockBajo,
                    SoloActivos = soloActivos,
                    OrderBy = orderBy,
                    OrderDirection = orderDirection,
                    Productos = viewModels,
                    TotalResultados = viewModels.Count
                };

                // Cargar dropdowns para filtros
                await CargarDropdownsFiltrosAsync(categoriaId, marcaId);

                return View(filterViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener listado de productos");
                TempData["Error"] = "Error al cargar los productos. Por favor, intente nuevamente.";

                // Cargar dropdowns incluso en caso de error
                await CargarDropdownsFiltrosAsync();

                return View(new ProductoFilterViewModel());
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
        /// <summary>
        /// Carga los dropdowns de Categor�as y Marcas para los formularios
        /// </summary>
        private async Task CargarDropdownsAsync(int? categoriaSeleccionada = null, int? marcaSeleccionada = null)
        {
            var (categorias, marcas) = await _catalogLookupService.GetCategoriasYMarcasAsync();

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
                return NotFound();

            try
            {
                var producto = await _productoService.GetByIdAsync(id.Value);
                if (producto == null)
                    return NotFound();

                // ✅ USAR AUTOMAPPER en lugar de mapeo manual
                var viewModel = _mapper.Map<ProductoViewModel>(producto);

                await CargarDropdownsAsync(viewModel.CategoriaId, viewModel.MarcaId);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar producto para editar {Id}", id);
                TempData["Error"] = "Error al cargar el producto";
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
                    var rowVersion = viewModel.RowVersion;
                    if (rowVersion is null || rowVersion.Length == 0)
                    {
                        ModelState.AddModelError("", "No se recibió la versión de fila (RowVersion). Recargue la página e intente nuevamente.");
                        await CargarDropdownsAsync(viewModel.CategoriaId, viewModel.MarcaId);
                        return View(viewModel);
                    }

                    // Verificar que el código no exista en otro producto
                    if (await _productoService.ExistsCodigoAsync(viewModel.Codigo, id))
                    {
                        ModelState.AddModelError("Codigo", "Ya existe otro producto con este código");
                        await CargarDropdownsAsync(viewModel.CategoriaId, viewModel.MarcaId);
                        return View(viewModel);
                    }

                    var producto = _mapper.Map<Producto>(viewModel);
                    producto.RowVersion = rowVersion;
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
                return NotFound();

            try
            {
                var producto = await _productoService.GetByIdAsync(id.Value);
                if (producto == null)
                    return NotFound();

                // ✅ USAR AUTOMAPPER
                var viewModel = _mapper.Map<ProductoViewModel>(producto);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar producto para eliminar {Id}", id);
                TempData["Error"] = "Error al cargar el producto";
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
                    TempData["Error"] = "No se encontr� el producto a eliminar";
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
        /// Carga los dropdowns de Categor�as y Marcas para los formularios
        /// </summary>
        /// <summary>
        /// Carga los dropdowns para los filtros
        /// </summary>
        private async Task CargarDropdownsFiltrosAsync(int? categoriaSeleccionada = null, int? marcaSeleccionada = null)
        {
            var (categorias, marcas) = await _catalogLookupService.GetCategoriasYMarcasAsync();

            ViewBag.CategoriasFiltro = new SelectList(
                categorias.OrderBy(c => c.Nombre),
                "Id",
                "Nombre",
                categoriaSeleccionada
            );

            ViewBag.MarcasFiltro = new SelectList(
                marcas.OrderBy(m => m.Nombre),
                "Id",
                "Nombre",
                marcaSeleccionada
            );
        }

    }
}