using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    public class ProveedorController : Controller
    {
        private readonly IProveedorService _proveedorService;
        private readonly ILogger<ProveedorController> _logger;
        private readonly IMapper _mapper;

        public ProveedorController(
            IProveedorService proveedorService,
            ILogger<ProveedorController> logger,
            IMapper mapper)
        {
            _proveedorService = proveedorService;
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

        // GET: Proveedor/Create
        public IActionResult Create()
        {
            return View();
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
    }
}