using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Servicio en background para marcar documentos vencidos automáticamente
    /// Se ejecuta diariamente a las 2:00 AM
    /// </summary>
    public class DocumentoVencidoBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DocumentoVencidoBackgroundService> _logger;
        // Definir la hora de ejecución (2:00 AM)
        private readonly TimeSpan _horaEjecucion = new TimeSpan(2, 0, 0);

        public DocumentoVencidoBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DocumentoVencidoBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DocumentoVencidoBackgroundService iniciado. Se ejecutará diariamente a las {HoraEjecucion}", 
                _horaEjecucion.ToString(@"hh\:mm"));

            // Esperar hasta la próxima ejecución programada (máximo 30 segundos en desarrollo)
            await EsperarHastaProximaEjecucionAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Iniciando marcado de documentos vencidos...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var documentoService = scope.ServiceProvider
                            .GetRequiredService<IDocumentoClienteService>();
                        
                        await documentoService.MarcarVencidosAsync();
                    }

                    _logger.LogInformation("Marcado de documentos vencidos completado");

                    // Esperar hasta la próxima ejecución (mañana a las 2 AM)
                    await EsperarUnDiaAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en DocumentoVencidoBackgroundService");
                    // Continuar esperando en caso de error
                    await EsperarUnDiaAsync(stoppingToken);
                }
            }

            _logger.LogInformation("DocumentoVencidoBackgroundService detenido");
        }

        /// <summary>
        /// Calcula cuánto tiempo esperar hasta la próxima ejecución programada
        /// </summary>
        private TimeSpan CalcularTiempoEsperaHastaSiguienteEjecucion()
        {
            var ahora = DateTime.Now;
            var proximaEjecucion = ahora.Date.Add(_horaEjecucion);

            // Si ya pasó la hora hoy, programar para mañana
            if (ahora >= proximaEjecucion)
            {
                proximaEjecucion = proximaEjecucion.AddDays(1);
            }

            var tiempoEspera = proximaEjecucion - ahora;
            
            _logger.LogInformation(
                "Próxima ejecución programada para: {FechaHora} ({MinutosRestantes} minutos)",
                proximaEjecucion.ToString("dd/MM/yyyy HH:mm:ss"),
                (int)tiempoEspera.TotalMinutes);

            return tiempoEspera;
        }

        /// <summary>
        /// Espera hasta la próxima ejecución programada (mañana a las 2 AM)
        /// </summary>
        private async Task EsperarUnDiaAsync(CancellationToken stoppingToken)
        {
            var tiempoEspera = CalcularTiempoEsperaHastaSiguienteEjecucion();
            await Task.Delay(tiempoEspera, stoppingToken);
        }

        /// <summary>
        /// Espera hasta la próxima ejecución (solo para la primera vez)
        /// </summary>
        private async Task EsperarHastaProximaEjecucionAsync(CancellationToken stoppingToken)
        {
            var tiempoEspera = CalcularTiempoEsperaHastaSiguienteEjecucion();
            
            // En producción esperar hasta la hora exacta
            // En desarrollo, esperar máximo 30 segundos para testing
            #if DEBUG
            var tiempoEsperaMaximo = TimeSpan.FromSeconds(30);
            if (tiempoEspera > tiempoEsperaMaximo)
            {
                _logger.LogWarning("Modo DEBUG: Esperando máximo 30 segundos en lugar de {MinutosRestantes} minutos", 
                    (int)tiempoEspera.TotalMinutes);
                tiempoEspera = tiempoEsperaMaximo;
            }
            #endif

            await Task.Delay(tiempoEspera, stoppingToken);
        }
    }
}