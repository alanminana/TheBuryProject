using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    public class MoraBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MoraBackgroundService> _logger;

        public MoraBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MoraBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Mora Background Service iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var moraService = scope.ServiceProvider.GetRequiredService<IMoraService>();
                        var configuracion = await moraService.GetConfiguracionAsync();

                        if (configuracion.JobActivo)
                        {
                            var ahora = DateTime.Now.TimeOfDay;
                            var horaEjecucion = configuracion.HoraEjecucion;
                            var diferencia = (horaEjecucion - ahora).TotalMinutes;

                            // Ejecutar si estamos dentro de la ventana de 5 minutos
                            if (diferencia >= -5 && diferencia <= 5)
                            {
                                if (!configuracion.UltimaEjecucion.HasValue ||
                                    configuracion.UltimaEjecucion.Value.Date < DateTime.Today)
                                {
                                    _logger.LogInformation("Ejecutando job de mora automática...");
                                    await moraService.ProcesarMoraAsync();
                                    _logger.LogInformation("Job de mora ejecutado exitosamente.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el job de mora automática.");
                }

                // Esperar 1 hora antes de la próxima verificación
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Mora Background Service detenido.");
        }
    }
}