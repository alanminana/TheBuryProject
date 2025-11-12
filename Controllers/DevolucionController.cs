using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers;

/// <summary>
/// Controlador para gestión de devoluciones, garantías y RMAs
/// </summary>
[Authorize]
public class DevolucionController : Controller
{
    private readonly IDevolucionService _devolucionService;
    private readonly IClienteService _clienteService;
    private readonly IVentaService _ventaService;
    private readonly IProveedorService _proveedorService;
    private readonly UserManager<IdentityUser> _userManager;

    public DevolucionController(
        IDevolucionService devolucionService,
        IClienteService clienteService,
        IVentaService ventaService,
        IProveedorService proveedorService,
        UserManager<IdentityUser> userManager)
    {
        _devolucionService = devolucionService;
        _clienteService = clienteService;
        _ventaService = ventaService;
        _proveedorService = proveedorService;
        _userManager = userManager;
    }

    #region Devoluciones

    /// <summary>
    /// Lista de todas las devoluciones
    /// </summary>
    [Authorize(Roles = "Admin,Gerente")]
    public async Task<IActionResult> Index()
    {
        var todasDevoluciones = await _devolucionService.ObtenerTodasDevolucionesAsync();

        var viewModel = new DevolucionesListViewModel
        {
            Pendientes = todasDevoluciones.Where(d => d.Estado == EstadoDevolucion.Pendiente).ToList(),
            EnRevision = todasDevoluciones.Where(d => d.Estado == EstadoDevolucion.EnRevision).ToList(),
            Aprobadas = todasDevoluciones.Where(d => d.Estado == EstadoDevolucion.Aprobada).ToList(),
            Completadas = todasDevoluciones.Where(d => d.Estado == EstadoDevolucion.Completada).ToList(),
            TotalPendientes = todasDevoluciones.Count(d => d.Estado == EstadoDevolucion.Pendiente),
            TotalAprobadas = todasDevoluciones.Count(d => d.Estado == EstadoDevolucion.Aprobada),
            TotalRechazadas = todasDevoluciones.Count(d => d.Estado == EstadoDevolucion.Rechazada),
            MontoTotalMes = todasDevoluciones
                .Where(d => d.FechaDevolucion >= DateTime.Now.AddMonths(-1) && d.Estado == EstadoDevolucion.Completada)
                .Sum(d => d.TotalDevolucion)
        };

        return View(viewModel);
    }

    /// <summary>
    /// Formulario para crear nueva devolución
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create(int? ventaId)
    {
        var viewModel = new CrearDevolucionViewModel();

        if (ventaId.HasValue)
        {
            var venta = await _ventaService.GetByIdAsync(ventaId.Value);
            if (venta != null)
            {
                viewModel.VentaId = venta.Id;
                viewModel.ClienteId = venta.ClienteId;
                viewModel.NumeroVenta = venta.Numero;
                viewModel.FechaVenta = venta.FechaVenta;
                viewModel.TotalVenta = venta.Total;
                viewModel.DiasDesdeVenta = await _devolucionService.ObtenerDiasDesdeVentaAsync(venta.Id);
                viewModel.PuedeDevolver = await _devolucionService.PuedeDevolverVentaAsync(venta.Id);

                // ✅ Agrega el nombre completo del cliente aquí
                var cliente = await _clienteService.GetByIdAsync(venta.ClienteId);
                if (cliente != null)
                {
                    viewModel.ClienteNombre = $"{cliente.Apellido}, {cliente.Nombre}";
                }

                foreach (var detalle in venta.Detalles)
                {
                    viewModel.Productos.Add(new ProductoDevolucionViewModel
                    {
                        ProductoId = detalle.ProductoId,
                        ProductoNombre = detalle.ProductoNombre ?? "Producto",
                        CantidadComprada = detalle.Cantidad,
                        PrecioUnitario = detalle.PrecioUnitario,
                        CantidadDevolver = 0
                    });
                }
            }
        }

        await CargarListasAsync();
        return View(viewModel);
    }


    /// <summary>
    /// Procesar creación de devolución (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearDevolucionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(model);
        }

        try
        {
            if (!await _devolucionService.PuedeDevolverVentaAsync(model.VentaId))
            {
                ModelState.AddModelError("", "Ha excedido el plazo para devolver esta venta (30 días)");
                await CargarListasAsync();
                return View(model);
            }

            var devolucion = new Devolucion
            {
                VentaId = model.VentaId,
                ClienteId = model.ClienteId,
                Motivo = model.Motivo,
                Descripcion = model.Descripcion,
                FechaDevolucion = DateTime.Now
            };

            var detalles = model.Productos
                .Where(p => p.CantidadDevolver > 0)
                .Select(p => new DevolucionDetalle
                {
                    ProductoId = p.ProductoId,
                    Cantidad = p.CantidadDevolver,
                    PrecioUnitario = p.PrecioUnitario,
                    EstadoProducto = p.EstadoProducto,
                    AccesoriosCompletos = p.AccesoriosCompletos,
                    AccesoriosFaltantes = p.AccesoriosFaltantes,
                    TieneGarantia = p.TieneGarantia,
                    ObservacionesTecnicas = p.ObservacionesTecnicas,
                    AccionRecomendada = DeterminarAccionRecomendada(p.EstadoProducto)
                })
                .ToList();

            if (!detalles.Any())
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un producto para devolver");
                await CargarListasAsync();
                return View(model);
            }

            await _devolucionService.CrearDevolucionAsync(devolucion, detalles);

            TempData["Success"] = $"Devolución {devolucion.NumeroDevolucion} creada exitosamente. Aguarde aprobación.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al crear devolución: {ex.Message}");
            await CargarListasAsync();
            return View(model);
        }
    }

    /// <summary>
    /// Ver detalles de una devolución
    /// </summary>
    public async Task<IActionResult> Detalles(int id)
    {
        var devolucion = await _devolucionService.ObtenerDevolucionAsync(id);
        if (devolucion == null)
        {
            TempData["Error"] = "Devolución no encontrada";
            return RedirectToAction(nameof(Index));
        }

        var detalles = await _devolucionService.ObtenerDetallesDevolucionAsync(id);

        var viewModel = new DevolucionDetallesViewModel
        {
            Devolucion = devolucion,
            Detalles = detalles,
            NotaCredito = devolucion.NotaCredito,
            RMA = devolucion.RMA
        };

        await CargarListasAsync();
        return View(viewModel);
    }

    #endregion

    #region Métodos Privados

    private async Task CargarListasAsync()
    {
        ViewBag.Clientes = new SelectList(await _clienteService.GetAllAsync(), "Id", "NombreCompleto");
        ViewBag.Proveedores = new SelectList(await _proveedorService.GetAllAsync(), "Id", "RazonSocial");
    }

    private AccionProducto DeterminarAccionRecomendada(EstadoProductoDevuelto estado)
    {
        return estado switch
        {
            EstadoProductoDevuelto.Nuevo => AccionProducto.ReintegrarStock,
            EstadoProductoDevuelto.UsadoBuenEstado => AccionProducto.ReintegrarStock,
            EstadoProductoDevuelto.UsadoConDetalles => AccionProducto.Cuarentena,
            EstadoProductoDevuelto.Defectuoso => AccionProducto.DevolverProveedor,
            EstadoProductoDevuelto.Danado => AccionProducto.Descarte,
            _ => AccionProducto.Cuarentena
        };
    }

    #endregion
}
