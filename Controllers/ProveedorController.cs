using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [AllowAnonymous]
    public class ProveedorController : Controller
    {
        private readonly IProveedorService _proveedorService;
        private readonly ILogger<ProveedorController> _logger;
        private readonly IMapper _mapper;
        private readonly ICategoriaService _categoriaService;
        private readonly IMarcaService _marcaService;
        private readonly IProductoService _productoService;
        public ProveedorController(
        IProveedorService proveedorService,
        ICategoriaService categoriaService,
        IMarcaService marcaService,
        IProductoService productoService,
        ILogger<ProveedorController> logger,
        IMapper mapper)
        {
            _proveedorService = proveedorService;
            _categoriaService = categoriaService;
            _marcaService = marcaService;
            _productoService = productoService;
            _logger = logger;
            _mapper = mapper;
        }

        // GET: Proveedor
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            bool soloActivos = false,
            string? orderBy = null,
            string? orderDirection = "asc")
        {
            try
            {
                var proveedores = await _proveedorService.SearchAsync(
                    searchTerm,
                    soloActivos,
                    orderBy,
                    orderDirection
                );

                var viewModels = _mapper.Map<IEnumerable<ProveedorViewModel>>(proveedores);

                var filterViewModel = new ProveedorFilterViewModel
                {
                    SearchTerm = searchTerm,
                    SoloActivos = soloActivos,
                    OrderBy = orderBy,
                    OrderDirection = orderDirection,
                    Proveedores = viewModels,
                    TotalResultados = viewModels.Count()
                };

                return View(filterViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener listado de proveedores");
                TempData["Error"] = "Error al cargar los proveedores. Por favor, intente nuevamente.";
                return View(new ProveedorFilterViewModel());
            }
        }

        // GET: Proveedor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var proveedor = await _proveedorService.GetByIdAsync(id.Value);
                if (proveedor == null)
                {
                    return NotFound();
                }

                var viewModel = _mapper.Map<ProveedorViewModel>(proveedor);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del proveedor {Id}", id);
                TempData["Error"] = "Error al cargar los detalles del proveedor. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new ProveedorViewModel();
            await CargarAsociacionesAsync(viewModel);
            return View(viewModel);
        }
        // POST: Proveedor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProveedorViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar que el CUIT no exista
                    if (await _proveedorService.ExistsCuitAsync(viewModel.Cuit))
                    {
                        ModelState.AddModelError("Cuit", "Ya existe un proveedor con este CUIT");
                        await CargarAsociacionesAsync(viewModel);

                        return View(viewModel);
                    }

                    var proveedor = _mapper.Map<Proveedor>(viewModel);
                    await _proveedorService.CreateAsync(proveedor);

                    TempData["Success"] = "Proveedor creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Error de validación al crear proveedor");
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear proveedor");
                    ModelState.AddModelError("", "Error al crear el proveedor. Por favor, intente nuevamente.");
                }
            }
            await CargarAsociacionesAsync(viewModel);

            return View(viewModel);
        }

        // GET: Proveedor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var proveedor = await _proveedorService.GetByIdAsync(id.Value);
                if (proveedor == null)
                {
                    return NotFound();
                }

                var viewModel = _mapper.Map<ProveedorViewModel>(proveedor);
                await CargarAsociacionesAsync(viewModel);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar proveedor para editar {Id}", id);
                TempData["Error"] = "Error al cargar el proveedor. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Proveedor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProveedorViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar que el CUIT no exista en otro proveedor
                    if (await _proveedorService.ExistsCuitAsync(viewModel.Cuit, id))
                    {
                        ModelState.AddModelError("Cuit", "Ya existe otro proveedor con este CUIT");
                        return View(viewModel);
                    }

                    var proveedor = _mapper.Map<Proveedor>(viewModel);
                    await _proveedorService.UpdateAsync(proveedor);

                    TempData["Success"] = "Proveedor actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Error de validación al actualizar proveedor {Id}", id);
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar proveedor {Id}", id);
                    ModelState.AddModelError("", "Error al actualizar el proveedor. Por favor, intente nuevamente.");
                }
            }
            await CargarAsociacionesAsync(viewModel);

            return View(viewModel);
        }

        // GET: Proveedor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var proveedor = await _proveedorService.GetByIdAsync(id.Value);
                if (proveedor == null)
                {
                    return NotFound();
                }

                var viewModel = _mapper.Map<ProveedorViewModel>(proveedor);
                await CargarAsociacionesAsync(viewModel);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar proveedor para eliminar {Id}", id);
                TempData["Error"] = "Error al cargar el proveedor. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Proveedor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _proveedorService.DeleteAsync(id);
                if (result)
                {
                    TempData["Success"] = "Proveedor eliminado exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se encontró el proveedor a eliminar";
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación al eliminar proveedor {Id}", id);
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar proveedor {Id}", id);
                TempData["Error"] = "Error al eliminar el proveedor. Por favor, intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }
        // API: Obtener productos del proveedor
        [HttpGet]
        public async Task<IActionResult> GetProductos(int id)
        {
            try
            {
                var proveedor = await _proveedorService.GetByIdAsync(id);
                if (proveedor == null)
                {
                    return NotFound();
                }

                // Si el proveedor tiene productos asociados, devolver solo esos
                if (proveedor.ProveedorProductos.Any())
                {
                    var productos = proveedor.ProveedorProductos
                        .Where(pp => pp.Producto != null && pp.Producto.Activo)
                        .Select(pp => new
                        {
                            id = pp.ProductoId,
                            nombre = pp.Producto!.Codigo + " - " + pp.Producto.Nombre,
                            precio = pp.Producto.PrecioCompra
                        })
                        .OrderBy(p => p.nombre)
                        .ToList();

                    return Json(productos);
                }

                // Si no tiene productos asociados, devolver mensaje
                return Json(new { error = "Este proveedor no tiene productos asociados" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos del proveedor {Id}", id);
                return BadRequest("Error al obtener productos");
            }
        }
        private async Task CargarAsociacionesAsync(ProveedorViewModel viewModel)
        {
            var categorias = await _categoriaService.GetAllAsync();
            viewModel.CategoriasDisponibles = categorias
                .OrderBy(c => c.Nombre)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Nombre,
                    Selected = viewModel.CategoriasSeleccionadas.Contains(c.Id)
                })
                .ToList();

            var marcas = await _marcaService.GetAllAsync();
            viewModel.MarcasDisponibles = marcas
                .OrderBy(m => m.Nombre)
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = m.Nombre,
                    Selected = viewModel.MarcasSeleccionadas.Contains(m.Id)
                })
                .ToList();

            var productos = await _productoService.GetAllAsync();
            viewModel.ProductosDisponibles = productos
                .OrderBy(p => p.Nombre)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(p.Codigo) ? p.Nombre : $"{p.Codigo} - {p.Nombre}",
                    Selected = viewModel.ProductosSeleccionados.Contains(p.Id)
                })
                .ToList();
        }
    }
}