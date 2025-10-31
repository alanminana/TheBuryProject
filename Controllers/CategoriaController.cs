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

        public CategoriaController(
            ICategoriaService categoriaService,
            ILogger<CategoriaController> logger,
            IMapper mapper)  // ✅ Agregado al constructor
        {
            _categoriaService = categoriaService;
            _logger = logger;
            _mapper = mapper;  // ✅ Inicializado
        }
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
                _logger.LogError(ex, "Error al obtener detalles de categoría {Id}", id);
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
                    // Verificar que el código no exista
                    if (await _categoriaService.ExistsCodigoAsync(viewModel.Codigo))
                    {
                        ModelState.AddModelError("Codigo", "Ya existe una categoría con este código");
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
                    TempData["Success"] = "Categoría creada exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear categoría");
                    ModelState.AddModelError("", "Error al crear la categoría. Por favor, intente nuevamente.");
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
                _logger.LogError(ex, "Error al cargar categoría para editar {Id}", id);
                TempData["Error"] = "Error al cargar la categoría. Por favor, intente nuevamente.";
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
                    // Verificar que el código no exista (excluyendo el registro actual)
                    if (await _categoriaService.ExistsCodigoAsync(viewModel.Codigo, id))
                    {
                        ModelState.AddModelError("Codigo", "Ya existe otra categoría con este código");
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
                    TempData["Success"] = "Categoría actualizada exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Error de validación al actualizar categoría {Id}", id);
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar categoría {Id}", id);
                    ModelState.AddModelError("", "Error al actualizar la categoría. Por favor, intente nuevamente.");
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
                _logger.LogError(ex, "Error al cargar categoría para eliminar {Id}", id);
                TempData["Error"] = "Error al cargar la categoría. Por favor, intente nuevamente.";
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
                    TempData["Success"] = "Categoría eliminada exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se encontró la categoría a eliminar";
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación al eliminar categoría {Id}", id);
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categoría {Id}", id);
                TempData["Error"] = "Error al eliminar la categoría. Por favor, intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Carga las categorías disponibles para el dropdown de categoría padre
        /// </summary>
        private async Task CargarCategoriasParaDropdown(int? selectedId = null, int? excludeId = null)
        {
            var categorias = await _categoriaService.GetAllAsync();

            // Excluir la categoría actual (para evitar ciclos)
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