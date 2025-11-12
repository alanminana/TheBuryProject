using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers;

/// <summary>
/// Controlador para gestión de cajas y arqueos
/// </summary>
[Authorize]
public class CajaController : Controller
{
    private readonly ICajaService _cajaService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<CajaController> _logger;

    public CajaController(
        ICajaService cajaService,
        UserManager<IdentityUser> userManager,
        ILogger<CajaController> logger)
    {
        _cajaService = cajaService;
        _userManager = userManager;
        _logger = logger;
    }

    #region CRUD de Cajas

    [Authorize(Roles = "Admin,Gerente")]
    public async Task<IActionResult> Index()
    {
        var cajas = await _cajaService.ObtenerTodasCajasAsync();
        var aperturas = await _cajaService.ObtenerAperturasAbiertasAsync();

        var viewModel = new CajasListViewModel
        {
            CajasActivas = cajas.Where(c => c.Activa).ToList(),
            CajasInactivas = cajas.Where(c => !c.Activa).ToList(),
            AperturasAbiertas = aperturas
        };

        return View(viewModel);
    }

    [Authorize(Roles = "Admin,Gerente")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Gerente")]
    public async Task<IActionResult> Create(CajaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _cajaService.CrearCajaAsync(model);
            TempData["Success"] = "Caja creada exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear caja");
            TempData["Error"] = ex.Message;
            return View(model);
        }
    }

    [Authorize(Roles = "Admin,Gerente")]
    public async Task<IActionResult> Edit(int id)
    {
        var caja = await _cajaService.ObtenerCajaPorIdAsync(id);
        if (caja == null)
        {
            TempData["Error"] = "Caja no encontrada";
            return RedirectToAction(nameof(Index));
        }

        var model = new CajaViewModel
        {
            Id = caja.Id,
            Codigo = caja.Codigo,
            Nombre = caja.Nombre,
            Descripcion = caja.Descripcion,
            Sucursal = caja.Sucursal,
            Ubicacion = caja.Ubicacion,
            Activa = caja.Activa,
            Estado = caja.Estado
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Gerente")]
    public async Task<IActionResult> Edit(int id, CajaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _cajaService.ActualizarCajaAsync(id, model);
            TempData["Success"] = "Caja actualizada exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar caja");
            TempData["Error"] = ex.Message;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _cajaService.EliminarCajaAsync(id);
            TempData["Success"] = "Caja eliminada exitosamente";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar caja");
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Apertura de Caja

    [Authorize(Roles = "Admin,Gerente,Vendedor")]
    public async Task<IActionResult> Abrir(int? cajaId)
    {
        var cajas = await _cajaService.ObtenerTodasCajasAsync();
        ViewBag.Cajas = new SelectList(cajas.Where(c => c.Activa), "Id", "Nombre", cajaId);

        var model = new AbrirCajaViewModel();
        if (cajaId.HasValue)
        {
            model.CajaId = cajaId.Value;
            var caja = cajas.FirstOrDefault(c => c.Id == cajaId.Value);
            if (caja != null)
            {
                model.CajaNombre = caja.Nombre;
                model.CajaCodigo = caja.Codigo;
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Gerente,Vendedor")]
    public async Task<IActionResult> Abrir(AbrirCajaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var cajas = await _cajaService.ObtenerTodasCajasAsync();
            ViewBag.Cajas = new SelectList(cajas.Where(c => c.Activa), "Id", "Nombre", model.CajaId);
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            var apertura = await _cajaService.AbrirCajaAsync(model, user?.UserName ?? "Unknown");
            TempData["Success"] = $"Caja abierta exitosamente con ${model.MontoInicial:N2}";
            return RedirectToAction(nameof(DetallesApertura), new { id = apertura.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al abrir caja");
            var cajas = await _cajaService.ObtenerTodasCajasAsync();
            ViewBag.Cajas = new SelectList(cajas.Where(c => c.Activa), "Id", "Nombre", model.CajaId);
            TempData["Error"] = ex.Message;
            return View(model);
        }
    }

    #endregion

    #region Movimientos

    [Authorize(Roles = "Admin,Gerente,Vendedor")]
    public async Task<IActionResult> RegistrarMovimiento(int aperturaId)
    {
        var apertura = await _cajaService.ObtenerAperturaPorIdAsync(aperturaId);
        if (apertura == null)
        {
            TempData["Error"] = "Apertura no encontrada";
            return RedirectToAction(nameof(Index));
        }

        var saldo = await _cajaService.CalcularSaldoActualAsync(aperturaId);

        var model = new MovimientoCajaViewModel
        {
            AperturaCajaId = aperturaId,
            CajaNombre = apertura.Caja.Nombre,
            SaldoActual = saldo
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Gerente,Vendedor")]
    public async Task<IActionResult> RegistrarMovimiento(MovimientoCajaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var apertura = await _cajaService.ObtenerAperturaPorIdAsync(model.AperturaCajaId);
            if (apertura != null)
            {
                model.CajaNombre = apertura.Caja.Nombre;
                model.SaldoActual = await _cajaService.CalcularSaldoActualAsync(model.AperturaCajaId);
            }
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            await _cajaService.RegistrarMovimientoAsync(model, user?.UserName ?? "Unknown");
            TempData["Success"] = "Movimiento registrado exitosamente";
            return RedirectToAction(nameof(DetallesApertura), new { id = model.AperturaCajaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar movimiento");
            var apertura = await _cajaService.ObtenerAperturaPorIdAsync(model.AperturaCajaId);
            if (apertura != null)
            {
                model.CajaNombre = apertura.Caja.Nombre;
                model.SaldoActual = await _cajaService.CalcularSaldoActualAsync(model.AperturaCajaId);
            }
            TempData["Error"] = ex.Message;
            return View(model);
        }
    }

    #endregion

    #region Cierre de Caja

    [Authorize(Roles = "Admin,Gerente")]
    public async Task<IActionResult> Cerrar(int aperturaId)
    {
        try
        {
            var detalles = await _cajaService.ObtenerDetallesAperturaAsync(aperturaId);

            var model = new CerrarCajaViewModel
            {
                AperturaCajaId = aperturaId,
                MontoInicialSistema = detalles.Apertura.MontoInicial,
                TotalIngresosSistema = detalles.TotalIngresos,
                TotalEgresosSistema = detalles.TotalEgresos,
                MontoEsperadoSistema = detalles.SaldoActual,
                CajaNombre = detalles.Apertura.Caja.Nombre,
                FechaApertura = detalles.Apertura.FechaApertura,
                UsuarioApertura = detalles.Apertura.UsuarioApertura,
                Movimientos = detalles.Movimientos
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar formulario de cierre");
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Gerente")]
    public async Task<IActionResult> Cerrar(CerrarCajaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var detalles = await _cajaService.ObtenerDetallesAperturaAsync(model.AperturaCajaId);
            model.CajaNombre = detalles.Apertura.Caja.Nombre;
            model.FechaApertura = detalles.Apertura.FechaApertura;
            model.UsuarioApertura = detalles.Apertura.UsuarioApertura;
            model.Movimientos = detalles.Movimientos;
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            var cierre = await _cajaService.CerrarCajaAsync(model, user?.UserName ?? "Unknown");

            if (cierre.TieneDiferencia)
            {
                TempData["Warning"] = $"Caja cerrada con diferencia de ${cierre.Diferencia:N2}";
            }
            else
            {
                TempData["Success"] = "Caja cerrada exitosamente sin diferencias";
            }

            return RedirectToAction(nameof(DetallesCierre), new { id = cierre.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cerrar caja");
            var detalles = await _cajaService.ObtenerDetallesAperturaAsync(model.AperturaCajaId);
            model.CajaNombre = detalles.Apertura.Caja.Nombre;
            model.FechaApertura = detalles.Apertura.FechaApertura;
            model.UsuarioApertura = detalles.Apertura.UsuarioApertura;
            model.Movimientos = detalles.Movimientos;
            TempData["Error"] = ex.Message;
            return View(model);
        }
    }

    #endregion

    #region Detalles y Reportes

    [Authorize(Roles = "Admin,Gerente,Vendedor")]
    public async Task<IActionResult> DetallesApertura(int id)
    {
        try
        {
            var detalles = await _cajaService.ObtenerDetallesAperturaAsync(id);
            return View(detalles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar detalles de apertura");
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,Gerente")]
    public async Task<IActionResult> DetallesCierre(int id)
    {
        var cierre = await _cajaService.ObtenerCierrePorIdAsync(id);
        if (cierre == null)
        {
            TempData["Error"] = "Cierre no encontrado";
            return RedirectToAction(nameof(Index));
        }

        return View(cierre);
    }

    [Authorize(Roles = "Admin,Gerente,Contador")]
    public async Task<IActionResult> Historial(int? cajaId, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var cierres = await _cajaService.ObtenerHistorialCierresAsync(cajaId, fechaDesde, fechaHasta);

        var totalDiferenciasPositivas = cierres.Where(c => c.Diferencia > 0).Sum(c => c.Diferencia);
        var totalDiferenciasNegativas = cierres.Where(c => c.Diferencia < 0).Sum(c => c.Diferencia);
        var cierresConDiferencia = cierres.Count(c => c.TieneDiferencia);
        var porcentajeCierresExactos = cierres.Count > 0
            ? ((cierres.Count - cierresConDiferencia) / (decimal)cierres.Count) * 100
            : 0;

        var viewModel = new HistorialCierresViewModel
        {
            Cierres = cierres,
            TotalDiferenciasPositivas = totalDiferenciasPositivas,
            TotalDiferenciasNegativas = totalDiferenciasNegativas,
            CierresConDiferencia = cierresConDiferencia,
            TotalCierres = cierres.Count,
            PorcentajeCierresExactos = porcentajeCierresExactos
        };

        var cajas = await _cajaService.ObtenerTodasCajasAsync();
        ViewBag.Cajas = new SelectList(cajas, "Id", "Nombre", cajaId);
        ViewBag.FechaDesde = fechaDesde;
        ViewBag.FechaHasta = fechaHasta;

        return View(viewModel);
    }

    #endregion
}