using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [AllowAnonymous]
    public class VentaController : Controller
    {
        private readonly IVentaService _ventaService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<VentaController> _logger;

        public VentaController(
            IVentaService ventaService,
            AppDbContext context,
            IMapper mapper,
            ILogger<VentaController> logger)
        {
            _ventaService = ventaService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Venta
        public async Task<IActionResult> Index(VentaFilterViewModel filter)
        {
            try
            {
                var ventas = await _ventaService.GetAllAsync(filter);

                // Cargar datos para filtros
                ViewBag.Clientes = await _context.Clientes
                    .Where(c => c.Activo)
                    .OrderBy(c => c.Apellido)
                    .Select(c => new SelectListItem
                    {
                        Value = c.NumeroDocumento,
                        Text = $"{c.Apellido}, {c.Nombre} - {c.NumeroDocumento}"
                    })
                    .ToListAsync();

                ViewBag.Estados = new SelectList(Enum.GetValues(typeof(EstadoVenta)));
                ViewBag.TiposPago = new SelectList(Enum.GetValues(typeof(TipoPago)));
                ViewBag.Filter = filter;

                return View(ventas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las ventas");
                TempData["Error"] = "Error al cargar las ventas";
                return View(new List<VentaViewModel>());
            }
        }

        // GET: Venta/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la venta {Id}", id);
                TempData["Error"] = "Error al cargar los detalles de la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Venta/Create
        public async Task<IActionResult> Create()
        {
            await CargarViewBags();
            return View(new VentaViewModel
            {
                FechaVenta = DateTime.Today,
                TipoPago = TipoPago.Efectivo
            });
        }

        // POST: Venta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VentaViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarViewBags(viewModel.ClienteId);
                    return View(viewModel);
                }

                // Validar que tenga al menos un detalle
                if (viewModel.Detalles == null || !viewModel.Detalles.Any())
                {
                    ModelState.AddModelError("", "Debe agregar al menos un producto a la venta");
                    await CargarViewBags(viewModel.ClienteId);
                    return View(viewModel);
                }

                var venta = await _ventaService.CreateAsync(viewModel);
                TempData["Success"] = $"Venta {venta.Numero} creada exitosamente";
                return RedirectToAction(nameof(Details), new { id = venta.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear venta");
                ModelState.AddModelError("", "Error al crear la venta: " + ex.Message);
                await CargarViewBags(viewModel.ClienteId);
                return View(viewModel);
            }
        }

        // GET: Venta/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (venta.Estado != EstadoVenta.Presupuesto)
                {
                    TempData["Error"] = "Solo se pueden editar ventas en estado Presupuesto";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await CargarViewBags(venta.ClienteId);
                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar venta para editar: {Id}", id);
                TempData["Error"] = "Error al cargar la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VentaViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                TempData["Error"] = "ID no coincide";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarViewBags(viewModel.ClienteId);
                    return View(viewModel);
                }

                if (viewModel.Detalles == null || !viewModel.Detalles.Any())
                {
                    ModelState.AddModelError("", "Debe agregar al menos un producto a la venta");
                    await CargarViewBags(viewModel.ClienteId);
                    return View(viewModel);
                }

                var resultado = await _ventaService.UpdateAsync(viewModel);
                if (resultado)
                {
                    TempData["Success"] = "Venta actualizada exitosamente";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["Error"] = "No se pudo actualizar la venta";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar venta: {Id}", id);
                ModelState.AddModelError("", "Error al actualizar la venta: " + ex.Message);
                await CargarViewBags(viewModel.ClienteId);
                return View(viewModel);
            }
        }

        // GET: Venta/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar venta para eliminar: {Id}", id);
                TempData["Error"] = "Error al cargar la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var resultado = await _ventaService.DeleteAsync(id);
                if (resultado)
                {
                    TempData["Success"] = "Venta eliminada exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar la venta";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar venta: {Id}", id);
                TempData["Error"] = "Error al eliminar la venta: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Venta/Confirmar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(int id)
        {
            try
            {
                var resultado = await _ventaService.ConfirmarVentaAsync(id);
                if (resultado)
                {
                    TempData["Success"] = "Venta confirmada exitosamente. El stock ha sido descontado.";
                }
                else
                {
                    TempData["Error"] = "No se pudo confirmar la venta";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar venta: {Id}", id);
                TempData["Error"] = "Error al confirmar la venta: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Venta/Cancelar/5
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                return View(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar venta para cancelar: {Id}", id);
                TempData["Error"] = "Error al cargar la venta";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Cancelar/5
        [HttpPost, ActionName("Cancelar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarConfirmed(int id, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["Error"] = "Debe indicar el motivo de la cancelación";
                    return RedirectToAction(nameof(Cancelar), new { id });
                }

                var resultado = await _ventaService.CancelarVentaAsync(id, motivo);
                if (resultado)
                {
                    TempData["Success"] = "Venta cancelada exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo cancelar la venta";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar venta: {Id}", id);
                TempData["Error"] = "Error al cancelar la venta: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Venta/Facturar/5
        public async Task<IActionResult> Facturar(int id)
        {
            try
            {
                var venta = await _ventaService.GetByIdAsync(id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (venta.Estado != EstadoVenta.Confirmada)
                {
                    TempData["Error"] = "Solo se pueden facturar ventas confirmadas";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var facturaViewModel = new FacturaViewModel
                {
                    VentaId = venta.Id,
                    FechaEmision = DateTime.Today,
                    Tipo = TipoFactura.B,
                    Subtotal = venta.Subtotal,
                    IVA = venta.IVA,
                    Total = venta.Total
                };

                ViewBag.Venta = venta;
                ViewBag.TiposFactura = new SelectList(Enum.GetValues(typeof(TipoFactura)));

                return View(facturaViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de facturación: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venta/Facturar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Facturar(FacturaViewModel facturaViewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var venta = await _ventaService.GetByIdAsync(facturaViewModel.VentaId);
                    ViewBag.Venta = venta;
                    ViewBag.TiposFactura = new SelectList(Enum.GetValues(typeof(TipoFactura)));
                    return View(facturaViewModel);
                }

                var resultado = await _ventaService.FacturarVentaAsync(facturaViewModel.VentaId, facturaViewModel);
                if (resultado)
                {
                    TempData["Success"] = "Factura generada exitosamente";
                    return RedirectToAction(nameof(Details), new { id = facturaViewModel.VentaId });
                }
                else
                {
                    TempData["Error"] = "No se pudo generar la factura";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al facturar venta");
                ModelState.AddModelError("", "Error al generar la factura: " + ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: API endpoint para obtener precio del producto
        [HttpGet]
        public async Task<IActionResult> GetProductoPrecio(int productoId)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(productoId);
                if (producto == null)
                    return NotFound();

                return Json(new
                {
                    precioVenta = producto.PrecioVenta,
                    stockActual = producto.StockActual,
                    codigo = producto.Codigo,
                    nombre = producto.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener precio del producto: {ProductoId}", productoId);
                return StatusCode(500, "Error al obtener el precio del producto");
            }
        }

        #region Métodos Privados

        private async Task CargarViewBags(int? clienteIdSeleccionado = null)
        {
            var clientes = await _context.Clientes
                .Where(c => c.Activo)
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    NombreCompleto = $"{c.Apellido}, {c.Nombre} - DNI: {c.NumeroDocumento}"
                })
                .ToListAsync();

            ViewBag.Clientes = new SelectList(clientes, "Id", "NombreCompleto", clienteIdSeleccionado);

            var productos = await _context.Productos
                .Where(p => p.Activo && p.StockActual > 0)
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    p.Id,
                    Detalle = $"{p.Codigo} - {p.Nombre} (Stock: {p.StockActual})"
                })
                .ToListAsync();

            ViewBag.Productos = new SelectList(productos, "Id", "Detalle");
            ViewBag.TiposPago = new SelectList(Enum.GetValues(typeof(TipoPago)));
        }

        #endregion
    }
}