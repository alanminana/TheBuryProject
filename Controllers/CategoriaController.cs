using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    public class CategoriaController : Controller
    {
        private readonly ICategoriaService _categoriaService;
        private readonly ILogger<CategoriaController> _logger;

        public CategoriaController(ICategoriaService categoriaService, ILogger<CategoriaController> logger)
        {
            _categoriaService = categoriaService;
            _logger = logger;
        }

        private readonly IMapper _mapper;

        public async Task<IActionResult> Index()
        {
            var categorias = await _categoriaService.GetAllAsync();
            var viewModels = _mapper.Map<IEnumerable<CategoriaViewModel>>(categorias);
            return View(viewModels);
        }

        // GET: Categoria/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var categoria = await _categoriaService.GetByIdAsync(id.Value);
                if (categoria == null)
                {
                    return NotFound();
                }

                var viewModel = new CategoriaViewModel
                {
                    Id = categoria.Id,
                    Codigo = categoria.Codigo,
                    Nombre = categoria.Nombre,
                    Descripcion = categoria.Descripcion,
                    ParentId = categoria.ParentId,
                    ParentNombre = categoria.Parent?.Nombre,
                    ControlSerieDefault = categoria.ControlSerieDefault
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles de categor�a {Id}", id);
                TempData["Error"] = "Error al cargar los detalles. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Categoria/Create
        public async Task<IActionResult> Create()
        {
            await CargarCategoriasParaDropdown();
            return View();
        }

        // POST: Categoria/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoriaViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar que el c�digo no exista
                    if (await _categoriaService.ExistsCodigoAsync(viewModel.Codigo))
                    {
                        ModelState.AddModelError("Codigo", "Ya existe una categor�a con este c�digo");
                        await CargarCategoriasParaDropdown(viewModel.ParentId);
                        return View(viewModel);
                    }

                    var categoria = new Categoria
                    {
                        Codigo = viewModel.Codigo,
                        Nombre = viewModel.Nombre,
                        Descripcion = viewModel.Descripcion,
                        ParentId = viewModel.ParentId,
                        ControlSerieDefault = viewModel.ControlSerieDefault
                    };

                    await _categoriaService.CreateAsync(categoria);
                    TempData["Success"] = "Categor�a creada exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear categor�a");
                    ModelState.AddModelError("", "Error al crear la categor�a. Por favor, intente nuevamente.");
                }
            }

            await CargarCategoriasParaDropdown(viewModel.ParentId);
            return View(viewModel);
        }

        // GET: Categoria/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var categoria = await _categoriaService.GetByIdAsync(id.Value);
                if (categoria == null)
                {
                    return NotFound();
                }

                var viewModel = new CategoriaViewModel
                {
                    Id = categoria.Id,
                    Codigo = categoria.Codigo,
                    Nombre = categoria.Nombre,
                    Descripcion = categoria.Descripcion,
                    ParentId = categoria.ParentId,
                    ControlSerieDefault = categoria.ControlSerieDefault
                };

                await CargarCategoriasParaDropdown(viewModel.ParentId, id.Value);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar categor�a para editar {Id}", id);
                TempData["Error"] = "Error al cargar la categor�a. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categoria/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoriaViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar que el c�digo no exista (excluyendo el registro actual)
                    if (await _categoriaService.ExistsCodigoAsync(viewModel.Codigo, id))
                    {
                        ModelState.AddModelError("Codigo", "Ya existe otra categor�a con este c�digo");
                        await CargarCategoriasParaDropdown(viewModel.ParentId, id);
                        return View(viewModel);
                    }

                    var categoria = new Categoria
                    {
                        Id = viewModel.Id,
                        Codigo = viewModel.Codigo,
                        Nombre = viewModel.Nombre,
                        Descripcion = viewModel.Descripcion,
                        ParentId = viewModel.ParentId,
                        ControlSerieDefault = viewModel.ControlSerieDefault
                    };

                    await _categoriaService.UpdateAsync(categoria);
                    TempData["Success"] = "Categor�a actualizada exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Error de validaci�n al actualizar categor�a {Id}", id);
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar categor�a {Id}", id);
                    ModelState.AddModelError("", "Error al actualizar la categor�a. Por favor, intente nuevamente.");
                }
            }

            await CargarCategoriasParaDropdown(viewModel.ParentId, id);
            return View(viewModel);
        }

        // GET: Categoria/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var categoria = await _categoriaService.GetByIdAsync(id.Value);
                if (categoria == null)
                {
                    return NotFound();
                }

                var viewModel = new CategoriaViewModel
                {
                    Id = categoria.Id,
                    Codigo = categoria.Codigo,
                    Nombre = categoria.Nombre,
                    Descripcion = categoria.Descripcion,
                    ParentId = categoria.ParentId,
                    ParentNombre = categoria.Parent?.Nombre,
                    ControlSerieDefault = categoria.ControlSerieDefault
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar categor�a para eliminar {Id}", id);
                TempData["Error"] = "Error al cargar la categor�a. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categoria/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _categoriaService.DeleteAsync(id);
                if (result)
                {
                    TempData["Success"] = "Categor�a eliminada exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se encontr� la categor�a a eliminar";
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validaci�n al eliminar categor�a {Id}", id);
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categor�a {Id}", id);
                TempData["Error"] = "Error al eliminar la categor�a. Por favor, intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Carga las categor�as disponibles para el dropdown de categor�a padre
        /// </summary>
        private async Task CargarCategoriasParaDropdown(int? selectedId = null, int? excludeId = null)
        {
            var categorias = await _categoriaService.GetAllAsync();

            // Excluir la categor�a actual (para evitar ciclos)
            if (excludeId.HasValue)
            {
                categorias = categorias.Where(c => c.Id != excludeId.Value);
            }

            ViewBag.Categorias = new SelectList(
                categorias.OrderBy(c => c.Nombre),
                "Id",
                "Nombre",
                selectedId
            );
        }
    }
}