using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    public class MoraService : IMoraService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MoraService> _logger;

        public MoraService(AppDbContext context, ILogger<MoraService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ConfiguracionMora> GetConfiguracionAsync()
        {
            var config = await _context.ConfiguracionesMora
                .Where(c => !c.IsDeleted)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new ConfiguracionMora
                {
                    DiasGracia = 3,
                    PorcentajeRecargo = 5.0m,
                    CalculoAutomatico = true,
                    NotificacionAutomatica = true,
                    JobActivo = true,
                    HoraEjecucion = new TimeSpan(8, 0, 0)
                };

                _context.ConfiguracionesMora.Add(config);
                await _context.SaveChangesAsync();
            }

            return config;
        }

        public async Task<ConfiguracionMora> UpdateConfiguracionAsync(ConfiguracionMoraViewModel viewModel)
        {
            var config = await _context.ConfiguracionesMora.FindAsync(viewModel.Id);

            if (config == null)
            {
                throw new InvalidOperationException("Configuración no encontrada");
            }

            config.DiasGracia = viewModel.DiasGracia;
            config.PorcentajeRecargo = viewModel.PorcentajeRecargo;
            config.CalculoAutomatico = viewModel.CalculoAutomatico;
            config.NotificacionAutomatica = viewModel.NotificacionAutomatica;
            config.JobActivo = viewModel.JobActivo;
            config.HoraEjecucion = viewModel.HoraEjecucion;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Configuración de mora actualizada");
            return config;
        }

        public async Task ProcesarMoraAsync()
        {
            var log = new LogMora
            {
                FechaEjecucion = DateTime.Now,
                Exitoso = false
            };

            try
            {
                var config = await GetConfiguracionAsync();
                var hoy = DateTime.Today;
                var fechaLimite = hoy.AddDays(-config.DiasGracia);

                var cuotasVencidas = await _context.Cuotas
                    .Include(c => c.Credito)
                        .ThenInclude(cr => cr!.Cliente)
                    .Where(c => !c.IsDeleted &&
                           c.Estado == EstadoCuota.Pendiente &&
                           c.FechaVencimiento < fechaLimite)
                    .ToListAsync();

                log.CuotasProcesadas = cuotasVencidas.Count;
                int alertasCreadas = 0;

                foreach (var cuota in cuotasVencidas)
                {
                    if (cuota.Credito == null) continue;

                    var alertaExistente = await _context.AlertasCobranza
                        .AnyAsync(a => a.CreditoId == cuota.CreditoId &&
                                  !a.Resuelta &&
                                  a.Tipo == TipoAlertaCobranza.CuotaVencida);

                    if (!alertaExistente)
                    {
                        var cliente = cuota.Credito.Cliente;
                        if (cliente == null) continue;

                        var diasMora = (hoy - cuota.FechaVencimiento).Days;
                        var prioridad = CalcularPrioridad(diasMora, cuota.MontoTotal);

                        var alerta = new AlertaCobranza
                        {
                            CreditoId = cuota.CreditoId,
                            ClienteId = cuota.Credito.ClienteId,
                            Tipo = TipoAlertaCobranza.CuotaVencida,
                            Prioridad = prioridad,
                            Mensaje = $"Cliente {cliente.Apellido}, {cliente.Nombre} tiene cuota vencida. Días de mora: {diasMora}",
                            MontoVencido = cuota.MontoTotal,
                            CuotasVencidas = await ContarCuotasVencidas(cuota.CreditoId),
                            FechaAlerta = DateTime.Now,
                            Resuelta = false
                        };

                        _context.AlertasCobranza.Add(alerta);
                        alertasCreadas++;
                    }
                }

                await GenerarAlertasProximosVencimientosAsync();

                log.AlertasGeneradas = alertasCreadas;
                log.Exitoso = true;
                log.Mensaje = $"Proceso completado. {cuotasVencidas.Count} cuotas procesadas, {alertasCreadas} alertas generadas.";

                var configuracion = await GetConfiguracionAsync();
                configuracion.UltimaEjecucion = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation(log.Mensaje);
            }
            catch (Exception ex)
            {
                log.Mensaje = "Error al procesar mora";
                log.DetalleError = ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : string.Empty);

                _logger.LogError(ex, "Error al procesar mora");
            }
            finally
            {
                _context.LogsMora.Add(log);
                await _context.SaveChangesAsync();
            }
        }

        private async Task GenerarAlertasProximosVencimientosAsync()
        {
            var hoy = DateTime.Today;
            var proximosDias = hoy.AddDays(5);

            var cuotasPorVencer = await _context.Cuotas
                .Include(c => c.Credito)
                    .ThenInclude(cr => cr!.Cliente)
                .Where(c => !c.IsDeleted &&
                       c.Estado == EstadoCuota.Pendiente &&
                       c.FechaVencimiento >= hoy &&
                       c.FechaVencimiento <= proximosDias)
                .ToListAsync();

            foreach (var cuota in cuotasPorVencer)
            {
                if (cuota.Credito?.Cliente == null) continue;

                var alertaExistente = await _context.AlertasCobranza
                    .AnyAsync(a => a.CreditoId == cuota.CreditoId &&
                              !a.Resuelta &&
                              a.Tipo == TipoAlertaCobranza.ProximoVencimiento);

                if (!alertaExistente)
                {
                    var diasRestantes = (cuota.FechaVencimiento - hoy).Days;
                    var cliente = cuota.Credito.Cliente;

                    var alerta = new AlertaCobranza
                    {
                        CreditoId = cuota.CreditoId,
                        ClienteId = cuota.Credito.ClienteId,
                        Tipo = TipoAlertaCobranza.ProximoVencimiento,
                        Prioridad = PrioridadAlerta.Baja,
                        Mensaje = $"Cliente {cliente.Apellido}, {cliente.Nombre} tiene cuota por vencer en {diasRestantes} días",
                        MontoVencido = cuota.MontoTotal,
                        CuotasVencidas = 0,
                        FechaAlerta = DateTime.Now,
                        Resuelta = false
                    };

                    _context.AlertasCobranza.Add(alerta);
                }
            }
        }

        private PrioridadAlerta CalcularPrioridad(int diasMora, decimal monto)
        {
            if (diasMora > 30 || monto > 50000)
                return PrioridadAlerta.Critica;
            if (diasMora > 15 || monto > 30000)
                return PrioridadAlerta.Alta;
            if (diasMora > 7 || monto > 15000)
                return PrioridadAlerta.Media;

            return PrioridadAlerta.Baja;
        }

        private async Task<int> ContarCuotasVencidas(int creditoId)
        {
            return await _context.Cuotas
                .CountAsync(c => !c.IsDeleted &&
                           c.CreditoId == creditoId &&
                           c.Estado == EstadoCuota.Pendiente &&
                           c.FechaVencimiento < DateTime.Today);
        }

        public async Task<List<AlertaCobranza>> GetAlertasActivasAsync()
        {
            return await _context.AlertasCobranza
                .Include(a => a.Cliente)
                .Include(a => a.Credito)
                .Where(a => !a.IsDeleted && !a.Resuelta)
                .OrderByDescending(a => a.Prioridad)
                .ThenBy(a => a.FechaAlerta)
                .ToListAsync();
        }

        public async Task<AlertaCobranza?> GetAlertaByIdAsync(int id)
        {
            return await _context.AlertasCobranza
                .Include(a => a.Cliente)
                .Include(a => a.Credito)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<bool> ResolverAlertaAsync(int id, string? observaciones)
        {
            var alerta = await _context.AlertasCobranza.FindAsync(id);

            if (alerta == null || alerta.IsDeleted)
                return false;

            alerta.Resuelta = true;
            alerta.FechaResolucion = DateTime.Now;
            alerta.Observaciones = observaciones;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Alerta {Id} resuelta", id);
            return true;
        }

        public async Task<List<LogMora>> GetLogsAsync(int cantidad = 50)
        {
            return await _context.LogsMora
                .Where(l => !l.IsDeleted)
                .OrderByDescending(l => l.FechaEjecucion)
                .Take(cantidad)
                .ToListAsync();
        }
    }
}
