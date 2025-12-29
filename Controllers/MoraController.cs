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

        [HttpGet]
        public async Task<IActionResult> Configuracion()
        {
            try
            {
                var config = await _moraService.GetConfiguracionAsync();
                var vm = _mapper.Map<ConfiguracionMoraViewModel>(config);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar configuración de mora");
                TempData["Error"] = "Error al cargar configuración: " + ex.Message;
                return View(new ConfiguracionMoraViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> GuardarConfiguracion(ConfiguracionMoraViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Datos inválidos";
                    return RedirectToAction(nameof(Configuracion));
                }

                await _moraService.UpdateConfiguracionAsync(viewModel);
                TempData["Success"] = "Configuración actualizada correctamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar configuración");
                TempData["Error"] = "Error al guardar configuración: " + ex.Message;
            }

            return RedirectToAction(nameof(Configuracion));
        }

        [HttpPost]
        public async Task<IActionResult> EjecutarJob()
        {
            try
            {
                await _moraService.ProcesarMoraAsync();
                TempData["Success"] = "Proceso de mora ejecutado correctamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar job de mora");
                TempData["Error"] = "Error al ejecutar mora: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Alertas(int? tipo = null, int? prioridad = null, string? estado = null, string? cliente = null)
        {
            try
            {
                var alertas = await _moraService.GetTodasAlertasAsync();

                if (tipo.HasValue)
                    alertas = alertas.Where(a => (int)a.Tipo == tipo.Value).ToList();

                if (prioridad.HasValue)
                    alertas = alertas.Where(a => (int)a.Prioridad == prioridad.Value).ToList();

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    alertas = estado switch
                    {
                        "noLeidas" => alertas.Where(a => !a.Leida).ToList(),
                        "leidas" => alertas.Where(a => a.Leida).ToList(),
                        "noResueltas" => alertas.Where(a => !a.Resuelta).ToList(),
                        "resueltas" => alertas.Where(a => a.Resuelta).ToList(),
                        _ => alertas
                    };
                }

                if (!string.IsNullOrWhiteSpace(cliente))
                {
                    alertas = alertas.Where(a =>
                        (a.ClienteNombre != null && a.ClienteNombre.Contains(cliente, StringComparison.OrdinalIgnoreCase)) ||
                        (a.ClienteDocumento != null && a.ClienteDocumento.Contains(cliente, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }

                ViewBag.ClienteFiltro = cliente;
                return View(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar alertas de mora");
                TempData["Error"] = "Error al cargar alertas: " + ex.Message;
                return View(Enumerable.Empty<AlertaCobranzaViewModel>());
            }
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> MarcarLeida(int id, string? rowVersion = null)
        {
            return await MarcarLeidaPost(id, rowVersion);
        }

        private async Task<IActionResult> MarcarLeidaPost(int id, string? rowVersion)
        {
            try
            {
                var bytes = string.IsNullOrWhiteSpace(rowVersion) ? null : Convert.FromBase64String(rowVersion);
                var ok = await _moraService.MarcarAlertaComoLeidaAsync(id, bytes);
                TempData[ok ? "Success" : "Error"] = ok ? "Alerta marcada como leída" : "No se pudo marcar la alerta";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (FormatException)
            {
                TempData["Error"] = "RowVersion inválida. Recargue la página e intente nuevamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar alerta como leída");
                TempData["Error"] = "Error al marcar alerta como leída: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Resolver(int id, string? rowVersion = null, string? observaciones = null)
        {
            return await ResolverPost(id, rowVersion, observaciones);
        }

        private async Task<IActionResult> ResolverPost(int id, string? rowVersion, string? observaciones)
        {
            try
            {
                var bytes = string.IsNullOrWhiteSpace(rowVersion) ? null : Convert.FromBase64String(rowVersion);
                var resultado = await _moraService.ResolverAlertaAsync(id, observaciones, bytes);

                TempData[resultado ? "Success" : "Error"] = resultado
                    ? "Alerta resuelta correctamente"
                    : "No se pudo resolver la alerta";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (FormatException)
            {
                TempData["Error"] = "RowVersion inválida. Recargue la página e intente nuevamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resolver alerta");
                TempData["Error"] = "Error al resolver alerta: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public Task<IActionResult> ProcesarMora()
        {
            return EjecutarJob();
        }

        [HttpPost]
        public Task<IActionResult> ResolverAlerta(int id, string? observaciones, [FromForm] string? rowVersion)
        {
            return ResolverPost(id, rowVersion, observaciones);
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