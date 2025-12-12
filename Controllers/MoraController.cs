using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Models.Constants;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Gerente + "," + Roles.Contador)]
    public class MoraController : Controller
    {
        private readonly IMoraService _moraService;
        private readonly IMapper _mapper;
        private readonly ILogger<MoraController> _logger;

        public MoraController(
            IMoraService moraService,
            IMapper mapper,
            ILogger<MoraController> logger)
        {
            _moraService = moraService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var config = await _moraService.GetConfiguracionAsync();
                var alertas = await _moraService.GetAlertasActivasAsync();

                var viewModel = new MoraIndexViewModel
                {
                    Configuracion = _mapper.Map<ConfiguracionMoraViewModel>(config), // ✅ AutoMapper
                    Alertas = alertas,
                    TotalAlertas = alertas.Count,
                    AlertasPendientes = alertas.Count(a => !a.Resuelta),
                    AlertasResueltas = alertas.Count(a => a.Resuelta),
                    MontoTotalVencido = alertas.Sum(a => a.MontoVencido)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar mora");
                TempData["Error"] = "Error al cargar mora: " + ex.Message;
                return View(new MoraIndexViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarConfiguracion(ConfiguracionMoraViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Datos inválidos";
                    return RedirectToAction(nameof(Index));
                }

                await _moraService.UpdateConfiguracionAsync(viewModel);
                TempData["Success"] = "Configuración actualizada correctamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar configuración");
                TempData["Error"] = "Error al actualizar configuración: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarMora()
        {
            try
            {
                await _moraService.ProcesarMoraAsync();
                TempData["Success"] = "Proceso de mora ejecutado correctamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar mora");
                TempData["Error"] = "Error al procesar mora: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        // ✅ OPTIMIZADO: Sin query innecesaria
        public async Task<IActionResult> ResolverAlerta(int id, string? observaciones)
        {
            try
            {
                var resultado = await _moraService.ResolverAlertaAsync(id, observaciones);

                if (resultado)
                    TempData["Success"] = "Alerta resuelta correctamente";
                else
                    TempData["Error"] = "No se pudo resolver la alerta";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resolver alerta");
                TempData["Error"] = "Error al resolver alerta: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Logs()
        {
            try
            {
                var logs = await _moraService.GetLogsAsync(100);
                return View(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar logs");
                TempData["Error"] = "Error al cargar logs: " + ex.Message;
                return View(new List<Models.Entities.LogMora>());
            }
        }
    }
}