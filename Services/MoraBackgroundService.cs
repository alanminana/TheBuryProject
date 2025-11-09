using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Servicio en background para ejecutar el procesamiento de mora automáticamente
    /// </summary>
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
            _logger.LogInformation("MoraBackgroundService iniciado");

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

                            // Verificar si estamos en la ventana de ejecución (±5 minutos)
                            if (diferencia >= -5 && diferencia <= 5)
                            {
                                // Verificar si ya se ejecutó hoy
                                var yaEjecutadoHoy = configuracion.UltimaEjecucion.HasValue &&
                                                    configuracion.UltimaEjecucion.Value.Date == DateTime.Today;

                                if (!yaEjecutadoHoy)
                                {
                                    _logger.LogInformation("Iniciando procesamiento automático de mora...");
                                    await moraService.ProcesarMoraAsync();
                                    _logger.LogInformation("Procesamiento de mora completado");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en MoraBackgroundService");
                }

                // Esperar 1 hora antes de la próxima verificación
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("MoraBackgroundService detenido");
        }
    }
}
