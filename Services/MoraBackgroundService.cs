using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Servicio en background que ejecuta el job de mora automáticamente
    /// </summary>
    public class MoraBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MoraBackgroundService> _logger;
        private readonly Timer? _timer;

        public MoraBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MoraBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de Mora iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Obtener configuración
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var moraService = scope.ServiceProvider.GetRequiredService<IMoraService>();
                        var config = await moraService.GetConfiguracionAsync();

                        if (config.JobActivo)
                        {
                            var ahora = DateTime.Now.TimeOfDay;
                            var horaEjecucion = config.HoraEjecucion;

                            // Verificar si es la hora de ejecutar
                            var diferencia = (horaEjecucion - ahora).TotalMinutes;

                            // Ejecutar si estamos dentro de la ventana de 5 minutos
                            if (diferencia >= -5 && diferencia <= 5)
                            {
                                // Verificar si ya se ejecutó hoy
                                if (!config.UltimaEjecucion.HasValue ||
                                    config.UltimaEjecucion.Value.Date < DateTime.Today)
                                {
                                    _logger.LogInformation("Ejecutando job de mora automático");
                                    await moraService.ProcesarMoraAsync();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el servicio de mora");
                }

                // Esperar 1 hora antes de la siguiente verificación
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}