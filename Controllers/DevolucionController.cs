using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Constants;
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
[Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
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
                viewModel.ClienteNombre = venta.ClienteNombre;
                viewModel.FechaVenta = venta.FechaVenta;
                viewModel.TotalVenta = venta.Total;
                viewModel.DiasDesdeVenta = await _devolucionService.ObtenerDiasDesdeVentaAsync(venta.Id);
                viewModel.PuedeDevolver = await _devolucionService.PuedeDevolverVentaAsync(venta.Id);

                // Cargar productos de la venta
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
        else
        {
            // Si no hay ventaId, cargar lista de ventas disponibles para devolución
            var ventasDisponibles = await _ventaService.GetAllAsync(new VentaFilterViewModel
            {
                Estado = Models.Enums.EstadoVenta.Confirmada
            });

            // También incluir facturadas y entregadas
            var ventasFacturadas = await _ventaService.GetAllAsync(new VentaFilterViewModel
            {
                Estado = Models.Enums.EstadoVenta.Facturada
            });

            var ventasEntregadas = await _ventaService.GetAllAsync(new VentaFilterViewModel
            {
                Estado = Models.Enums.EstadoVenta.Entregada
            });

            var todasVentas = ventasDisponibles
                .Concat(ventasFacturadas)
                .Concat(ventasEntregadas)
                .OrderByDescending(v => v.FechaVenta)
                .ToList();

            ViewBag.Ventas = todasVentas;
        }

        await CargarListasAsync();
        return View(viewModel);
    }

    /// <summary>
    /// Procesar creación de devolución
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
            // Validar que la venta existe y obtener el cliente correcto
            var venta = await _ventaService.GetByIdAsync(model.VentaId);
            if (venta == null)
            {
                ModelState.AddModelError("", "La venta especificada no existe");
                await CargarListasAsync();
                return View(model);
            }

            // Validar que el cliente de la devolución coincide con el cliente de la venta
            if (model.ClienteId != venta.ClienteId)
            {
                ModelState.AddModelError("", "El cliente de la devolución no coincide con el cliente de la venta");
                await CargarListasAsync();
                return View(model);
            }

            // Validar que puede devolver
            if (!await _devolucionService.PuedeDevolverVentaAsync(model.VentaId))
            {
                ModelState.AddModelError("", "Ha excedido el plazo para devolver esta venta (30 días)");
                await CargarListasAsync();
                return View(model);
            }

            // Crear devolución (el ClienteId ya está validado que coincide con la venta)
            var devolucion = new Devolucion
            {
                VentaId = model.VentaId,
                ClienteId = model.ClienteId,
                Motivo = model.Motivo,
                Descripcion = model.Descripcion,
                FechaDevolucion = DateTime.Now
            };

            // Crear detalles
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

    /// <summary>
    /// Aprobar devolución
    /// </summary>
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id, byte[]? rowVersion)
    {
        try
        {
            if (rowVersion is null || rowVersion.Length == 0)
            {
                TempData["Error"] = "Falta información de concurrencia (RowVersion). Recargá la página e intentá nuevamente.";
                return RedirectToAction(nameof(Detalles), new { id });
            }

            var usuario = await _userManager.GetUserAsync(User);
            await _devolucionService.AprobarDevolucionAsync(id, usuario?.UserName ?? Roles.Administrador, rowVersion);
            TempData["Success"] = "Devolución aprobada exitosamente. Se generó una nota de crédito.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al aprobar devolución: {ex.Message}";
        }

        return RedirectToAction(nameof(Detalles), new { id });
    }

    /// <summary>
    /// Rechazar devolución
    /// </summary>
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, string motivo, byte[]? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["Error"] = "Debe proporcionar un motivo para rechazar la devolución";
            return RedirectToAction(nameof(Detalles), new { id });
        }

        if (rowVersion is null || rowVersion.Length == 0)
        {
            TempData["Error"] = "Falta información de concurrencia (RowVersion). Recargá la página e intentá nuevamente.";
            return RedirectToAction(nameof(Detalles), new { id });
        }

        try
        {
            await _devolucionService.RechazarDevolucionAsync(id, motivo, rowVersion);
            TempData["Success"] = "Devolución rechazada";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al rechazar devolución: {ex.Message}";
        }

        return RedirectToAction(nameof(Detalles), new { id });
    }

    /// <summary>
    /// Completar devolución (procesar stock)
    /// </summary>
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Completar(int id, byte[]? rowVersion)
    {
        try
        {
            if (rowVersion is null || rowVersion.Length == 0)
            {
                TempData["Error"] = "Falta información de concurrencia (RowVersion). Recargá la página e intentá nuevamente.";
                return RedirectToAction(nameof(Detalles), new { id });
            }

            await _devolucionService.CompletarDevolucionAsync(id, rowVersion);
            TempData["Success"] = "Devolución completada. Stock actualizado según las acciones definidas.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al completar devolución: {ex.Message}";
        }

        return RedirectToAction(nameof(Detalles), new { id });
    }

    #endregion

    #region Garantías

    /// <summary>
    /// Lista de garantías
    /// </summary>
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
    public async Task<IActionResult> Garantias()
    {
        var todasGarantias = await _devolucionService.ObtenerTodasGarantiasAsync();
        var proximasVencer = await _devolucionService.ObtenerGarantiasProximasVencerAsync(30);

        var viewModel = new GarantiasListViewModel
        {
            Vigentes = todasGarantias.Where(g => g.Estado == EstadoGarantia.Vigente && g.FechaVencimiento >= DateTime.Now).ToList(),
            ProximasVencer = proximasVencer,
            Vencidas = todasGarantias.Where(g => g.FechaVencimiento < DateTime.Now || g.Estado == EstadoGarantia.Vencida).ToList(),
            EnUso = todasGarantias.Where(g => g.Estado == EstadoGarantia.EnUso).ToList(),
            TotalVigentes = todasGarantias.Count(g => g.Estado == EstadoGarantia.Vigente),
            TotalProximasVencer = proximasVencer.Count
        };

        return View(viewModel);
    }

    #endregion

    #region RMAs

    /// <summary>
    /// Estadísticas de RMAs y devoluciones
    /// </summary>
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
    public async Task<IActionResult> RMAs(DateTime? desde, DateTime? hasta)
    {
        var fechaDesde = desde ?? DateTime.Now.AddMonths(-1);
        var fechaHasta = hasta ?? DateTime.Now;

        var viewModel = new EstadisticasDevolucionViewModel
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            DevolucionesPorMotivo = await _devolucionService.ObtenerEstadisticasMotivoDevolucionAsync(fechaDesde, fechaHasta),
            ProductosMasDevueltos = await _devolucionService.ObtenerProductosMasDevueltosAsync(10),
            MontoTotalDevuelto = await _devolucionService.ObtenerTotalDevolucionesPeriodoAsync(fechaDesde, fechaHasta),
            RMAsPendientes = await _devolucionService.ObtenerCantidadRMAsPendientesAsync()
        };

        viewModel.TotalDevoluciones = viewModel.DevolucionesPorMotivo.Values.Sum();

        return View(viewModel);
    }

    /// <summary>
    /// Crear RMA para una devolución
    /// </summary>
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearRMA(int devolucionId, int proveedorId, string motivoSolicitud, byte[]? devolucionRowVersion)
    {
        try
        {
            if (devolucionRowVersion is null || devolucionRowVersion.Length == 0)
            {
                TempData["Error"] = "Falta información de concurrencia (RowVersion). Recargá la devolución e intentá nuevamente.";
                return RedirectToAction(nameof(Detalles), new { id = devolucionId });
            }

            var rma = new RMA
            {
                DevolucionId = devolucionId,
                ProveedorId = proveedorId,
                MotivoSolicitud = motivoSolicitud
            };

            await _devolucionService.CrearRMAAsync(rma, devolucionRowVersion);
            TempData["Success"] = $"RMA {rma.NumeroRMA} creado exitosamente";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear RMA: {ex.Message}";
        }

        return RedirectToAction(nameof(Detalles), new { id = devolucionId });
    }

    #endregion

    #region Notas de Crédito

    /// <summary>
    /// Ver notas de crédito de un cliente
    /// </summary>
    public async Task<IActionResult> NotasCredito(int clienteId)
    {
        var cliente = await _clienteService.GetByIdAsync(clienteId);
        if (cliente == null)
        {
            TempData["Error"] = "Cliente no encontrado";
            return RedirectToAction("Index", "Cliente");
        }

        var todasNotas = await _devolucionService.ObtenerNotasCreditoPorClienteAsync(clienteId);
        var creditoDisponible = await _devolucionService.ObtenerCreditoDisponibleClienteAsync(clienteId);

        var viewModel = new NotasCreditoClienteViewModel
        {
            ClienteId = clienteId,
            ClienteNombre = cliente.NombreCompleto ?? "Cliente",
            NotasVigentes = todasNotas.Where(nc => nc.MontoDisponible > 0 && nc.Estado == EstadoNotaCredito.Vigente).ToList(),
            NotasUtilizadas = todasNotas.Where(nc => nc.MontoDisponible == 0 || nc.Estado == EstadoNotaCredito.UtilizadaTotalmente).ToList(),
            CreditoTotalDisponible = creditoDisponible
        };

        return View(viewModel);
    }

    #endregion

    #region Estadísticas

    /// <summary>
    /// Estadísticas de devoluciones
    /// </summary>
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente)]
    public async Task<IActionResult> Estadisticas(DateTime? desde, DateTime? hasta)
    {
        var fechaDesde = desde ?? DateTime.Now.AddMonths(-1);
        var fechaHasta = hasta ?? DateTime.Now;

        var viewModel = new EstadisticasDevolucionViewModel
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            DevolucionesPorMotivo = await _devolucionService.ObtenerEstadisticasMotivoDevolucionAsync(fechaDesde, fechaHasta),
            ProductosMasDevueltos = await _devolucionService.ObtenerProductosMasDevueltosAsync(10),
            MontoTotalDevuelto = await _devolucionService.ObtenerTotalDevolucionesPeriodoAsync(fechaDesde, fechaHasta),
            RMAsPendientes = await _devolucionService.ObtenerCantidadRMAsPendientesAsync()
        };

        viewModel.TotalDevoluciones = viewModel.DevolucionesPorMotivo.Values.Sum();

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