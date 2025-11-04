using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    public class MarcaController : Controller
    {
        private readonly IMarcaService _marcaService;
        private readonly ILogger<MarcaController> _logger;

        public MarcaController(IMarcaService marcaService, ILogger<MarcaController> logger)
        {
            _marcaService = marcaService;
            _logger = logger;
        }

        // GET: Marca
        // GET: Marca
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            bool soloActivos = false,
            string? orderBy = null,
            string? orderDirection = "asc")
        {
            try
            {
                // Ejecutar búsqueda con filtros
                var marcas = await _marcaService.SearchAsync(
                    searchTerm,
                    soloActivos,
                    orderBy,
                    orderDirection
                );

                var viewModels = marcas.Select(m => new MarcaViewModel
                {
                    Id = m.Id,
                    Codigo = m.Codigo,
                    Nombre = m.Nombre,
                    Descripcion = m.Descripcion,
                    ParentId = m.ParentId,
                    ParentNombre = m.Parent?.Nombre,
                    PaisOrigen = m.PaisOrigen,
                    Activo = m.Activo
                }).ToList();

                // Crear ViewModel de filtros
                var filterViewModel = new MarcaFilterViewModel
                {
                    SearchTerm = searchTerm,
                    SoloActivos = soloActivos,
                    OrderBy = orderBy,
                    OrderDirection = orderDirection,
                    Marcas = viewModels,
                    TotalResultados = viewModels.Count
                };

                return View(filterViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener listado de marcas");
                TempData["Error"] = "Error al cargar las marcas. Por favor, intente nuevamente.";
                return View(new MarcaFilterViewModel());
            }
        }

        // GET: Marca/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var marca = await _marcaService.GetByIdAsync(id.Value);
                if (marca == null)
                {
                    return NotFound();
                }

                var viewModel = new MarcaViewModel
                {
                    Id = marca.Id,
                    Codigo = marca.Codigo,
                    Nombre = marca.Nombre,
                    Descripcion = marca.Descripcion,
                    ParentId = marca.ParentId,
                    ParentNombre = marca.Parent?.Nombre,
                    PaisOrigen = marca.PaisOrigen
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles de marca {Id}", id);
                TempData["Error"] = "Error al cargar los detalles. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Marca/Create
        public async Task<IActionResult> Create()
        {
            await CargarMarcasParaDropdown();
            return View();
        }

        // POST: Marca/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarcaViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar que el código no exista
                    if (await _marcaService.ExistsCodigoAsync(viewModel.Codigo))
                    {
                        ModelState.AddModelError("Codigo", "Ya existe una marca con este código");
                        await CargarMarcasParaDropdown(viewModel.ParentId);
                        return View(viewModel);
                    }

                    var marca = new Marca
                    {
                        Codigo = viewModel.Codigo,
                        Nombre = viewModel.Nombre,
                        Descripcion = viewModel.Descripcion,
                        ParentId = viewModel.ParentId,
                        PaisOrigen = viewModel.PaisOrigen
                    };

                    await _marcaService.CreateAsync(marca);
                    TempData["Success"] = "Marca creada exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear marca");
                    ModelState.AddModelError("", "Error al crear la marca. Por favor, intente nuevamente.");
                }
            }

            await CargarMarcasParaDropdown(viewModel.ParentId);
            return View(viewModel);
        }

        // GET: Marca/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var marca = await _marcaService.GetByIdAsync(id.Value);
                if (marca == null)
                {
                    return NotFound();
                }

                var viewModel = new MarcaViewModel
                {
                    Id = marca.Id,
                    Codigo = marca.Codigo,
                    Nombre = marca.Nombre,
                    Descripcion = marca.Descripcion,
                    ParentId = marca.ParentId,
                    PaisOrigen = marca.PaisOrigen
                };

                await CargarMarcasParaDropdown(viewModel.ParentId, id.Value);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar marca para editar {Id}", id);
                TempData["Error"] = "Error al cargar la marca. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Marca/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MarcaViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar que el código no exista (excluyendo el registro actual)
                    if (await _marcaService.ExistsCodigoAsync(viewModel.Codigo, id))
                    {
                        ModelState.AddModelError("Codigo", "Ya existe otra marca con este código");
                        await CargarMarcasParaDropdown(viewModel.ParentId, id);
                        return View(viewModel);
                    }

                    var marca = new Marca
                    {
                        Id = viewModel.Id,
                        Codigo = viewModel.Codigo,
                        Nombre = viewModel.Nombre,
                        Descripcion = viewModel.Descripcion,
                        ParentId = viewModel.ParentId,
                        PaisOrigen = viewModel.PaisOrigen
                    };

                    await _marcaService.UpdateAsync(marca);
                    TempData["Success"] = "Marca actualizada exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Error de validación al actualizar marca {Id}", id);
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar marca {Id}", id);
                    ModelState.AddModelError("", "Error al actualizar la marca. Por favor, intente nuevamente.");
                }
            }

            await CargarMarcasParaDropdown(viewModel.ParentId, id);
            return View(viewModel);
        }

        // GET: Marca/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var marca = await _marcaService.GetByIdAsync(id.Value);
                if (marca == null)
                {
                    return NotFound();
                }

                var viewModel = new MarcaViewModel
                {
                    Id = marca.Id,
                    Codigo = marca.Codigo,
                    Nombre = marca.Nombre,
                    Descripcion = marca.Descripcion,
                    ParentId = marca.ParentId,
                    ParentNombre = marca.Parent?.Nombre,
                    PaisOrigen = marca.PaisOrigen
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar marca para eliminar {Id}", id);
                TempData["Error"] = "Error al cargar la marca. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Marca/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _marcaService.DeleteAsync(id);
                if (result)
                {
                    TempData["Success"] = "Marca eliminada exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se encontró la marca a eliminar";
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación al eliminar marca {Id}", id);
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar marca {Id}", id);
                TempData["Error"] = "Error al eliminar la marca. Por favor, intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Carga las marcas disponibles para el dropdown de marca padre
        /// </summary>
        private async Task CargarMarcasParaDropdown(int? selectedId = null, int? excludeId = null)
        {
            var marcas = await _marcaService.GetAllAsync();

            // Excluir la marca actual (para evitar ciclos)
            if (excludeId.HasValue)
            {
                marcas = marcas.Where(m => m.Id != excludeId.Value);
            }

            ViewBag.Marcas = new SelectList(
                marcas.OrderBy(m => m.Nombre),
                "Id",
                "Nombre",
                selectedId
            );
        }
    }
}