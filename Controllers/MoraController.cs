using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Controllers
{
    public class MoraController : Controller
    {
        private readonly IMoraService _moraService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public MoraController(IMoraService moraService, AppDbContext context, IMapper mapper)
        {
            _moraService = moraService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var alertasActivas = await _moraService.GetAlertasActivasAsync();
            var historialEjecuciones = await _moraService.GetLogsAsync(10);
            var configuracion = await _moraService.GetConfiguracionAsync();

            ViewBag.AlertasActivas = alertasActivas;
            ViewBag.HistorialEjecuciones = historialEjecuciones;
            ViewBag.AlertasCriticas = alertasActivas.Count(a => a.Prioridad == 3);
            ViewBag.AlertasNoLeidas = alertasActivas.Count(a => !a.Leida);

            return View(configuracion);
        }

        public async Task<IActionResult> Configuracion()
        {
            var config = await _moraService.GetConfiguracionAsync();
            var viewModel = _mapper.Map<ConfiguracionMoraViewModel>(config);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConfiguracion(ConfiguracionMoraViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Configuracion", model);
            }

            try
            {
                var config = _mapper.Map<ConfiguracionMora>(model);

                if (model.Id == 0)
                {
                    _context.ConfiguracionesMora.Add(config);
                }
                else
                {
                    var existing = await _context.ConfiguracionesMora.FindAsync(model.Id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    _mapper.Map(model, existing);
                    _context.Update(existing);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Configuración guardada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al guardar la configuración: {ex.Message}");
                return View("Configuracion", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> EjecutarJob()
        {
            try
            {
                var log = await _moraService.ProcesarMoraAsync();

                if (log.Exitoso)
                {
                    TempData["SuccessMessage"] = $"Job ejecutado exitosamente. " +
                        $"Cuotas procesadas: {log.CuotasProcesadas}, " +
                        $"Alertas generadas: {log.AlertasGeneradas}, " +
                        $"Total mora: ${log.TotalMora:N2}";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error en la ejecución: {log.Errores}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al ejecutar el job: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Alertas(string? tipo, int? prioridad, string? estado, string? cliente)
        {
            var query = _context.AlertasCobranza
                .Include(a => a.Cliente)
                .Include(a => a.Cuota)
                .Include(a => a.Credito)
                .Where(a => !a.IsDeleted);

            // Aplicar filtros
            if (!string.IsNullOrEmpty(tipo) && Enum.TryParse<TipoAlerta>(tipo, out var tipoEnum))
            {
                query = query.Where(a => a.Tipo == tipoEnum);
            }

            if (prioridad.HasValue)
            {
                query = query.Where(a => a.Prioridad == prioridad.Value);
            }

            if (!string.IsNullOrEmpty(estado))
            {
                query = estado switch
                {
                    "noLeidas" => query.Where(a => !a.Leida),
                    "leidas" => query.Where(a => a.Leida),
                    "noResueltas" => query.Where(a => !a.Resuelta),
                    "resueltas" => query.Where(a => a.Resuelta),
                    _ => query
                };
            }

            if (!string.IsNullOrEmpty(cliente))
            {
                query = query.Where(a => a.Cliente.NombreCompleto.Contains(cliente));
                ViewBag.ClienteFiltro = cliente;
            }

            var alertas = await query.ToListAsync();
            var viewModels = _mapper.Map<List<AlertaCobranzaViewModel>>(alertas);

            // Llenar nombres de clientes
            foreach (var vm in viewModels)
            {
                var alerta = alertas.First(a => a.Id == vm.Id);
                vm.ClienteNombre = alerta.Cliente.NombreCompleto;
            }

            return View(viewModels);
        }

        [HttpPost]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            await _moraService.MarcarAlertaLeidaAsync(id, User.Identity?.Name ?? "Sistema");
            TempData["SuccessMessage"] = "Alerta marcada como leída.";
            return RedirectToAction(nameof(Alertas));
        }

        [HttpPost]
        public async Task<IActionResult> Resolver(int id)
        {
            await _moraService.ResolverAlertaAsync(id, User.Identity?.Name ?? "Sistema", "Resuelta manualmente");
            TempData["SuccessMessage"] = "Alerta resuelta.";
            return RedirectToAction(nameof(Alertas));
        }
    }
}