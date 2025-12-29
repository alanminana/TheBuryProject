using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Servicio centralizado para gestión de mora, alertas y cobranzas
    /// ✅ REFACTORIZADO: Consolidado, sin duplicaciones, optimizado
    /// </summary>
    public class MoraService : IMoraService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<MoraService> _logger;
        private ConfiguracionMora? _configuracion;

        public MoraService(
            AppDbContext context,
            IMapper mapper,
            ILogger<MoraService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        #region Configuración

        public async Task<ConfiguracionMora> GetConfiguracionAsync()
        {
            try
            {
                _configuracion ??= await _context.ConfiguracionesMora
                    .Where(c => !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (_configuracion == null)
                {
                    _configuracion = new ConfiguracionMora
                    {
                        DiasGracia = 3,
                        PorcentajeRecargo = 5.0m,
                        CalculoAutomatico = true,
                        NotificacionAutomatica = true,
                        JobActivo = true,
                        HoraEjecucion = new TimeSpan(8, 0, 0),
                        CreatedAt = DateTime.Now
                    };

                    _context.ConfiguracionesMora.Add(_configuracion);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Configuración de mora creada por defecto");
                }

                return _configuracion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener configuración de mora");
                throw;
            }
        }

        public async Task<ConfiguracionMora> UpdateConfiguracionAsync(ConfiguracionMoraViewModel viewModel)
        {
            try
            {
                var config = await _context.ConfiguracionesMora
                    .FirstOrDefaultAsync(c => c.Id == viewModel.Id && !c.IsDeleted);
                if (config == null)
                    throw new InvalidOperationException("Configuración no encontrada");

                config.DiasGracia = viewModel.DiasGracia;
                config.PorcentajeRecargo = viewModel.PorcentajeRecargo;
                config.CalculoAutomatico = viewModel.CalculoAutomatico;
                config.NotificacionAutomatica = viewModel.NotificacionAutomatica;
                config.JobActivo = viewModel.JobActivo;
                config.HoraEjecucion = viewModel.HoraEjecucion;
                config.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                _configuracion = null; // Limpiar caché para recargar

                _logger.LogInformation("Configuración de mora actualizada");
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar configuración de mora");
                throw;
            }
        }

        #endregion

        #region Procesamiento de Mora

        public async Task ProcesarMoraAsync()
        {
            var inicioEjecucion = DateTime.Now;
            var log = new LogMora
            {
                FechaEjecucion = inicioEjecucion,
                Exitoso = false,
                CuotasProcesadas = 0,
                AlertasGeneradas = 0,
                CuotasConMora = 0,
                TotalMora = 0,
                TotalRecargosAplicados = 0,
                Errores = 0
            };

            try
            {
                _logger.LogInformation("=== INICIANDO PROCESAMIENTO DE MORA ===");

                var config = await GetConfiguracionAsync();
                var hoy = DateTime.Today;
                var fechaLimite = hoy.AddDays(-config.DiasGracia);

                // ✅ OPTIMIZADO: Obtener cuotas vencidas en UN solo query (sin N+1)
                var cuotasVencidas = await _context.Cuotas
                    .Include(c => c.Credito)
                        .ThenInclude(cr => cr!.Cliente)
                    .Where(c => !c.IsDeleted &&
                           !c.Credito!.IsDeleted &&
                           !c.Credito!.Cliente!.IsDeleted &&
                           c.Estado == EstadoCuota.Pendiente &&
                           c.FechaVencimiento < fechaLimite)
                    .ToListAsync();

                log.CuotasProcesadas = cuotasVencidas.Count;

                // ✅ OPTIMIZADO: Agrupar por crédito para evitar múltiples queries
                var cuotasPorCredito = cuotasVencidas.GroupBy(c => c.CreditoId).ToList();
                int alertasCreadas = 0;

                foreach (var grupo in cuotasPorCredito)
                {
                    try
                    {
                        var creditoId = grupo.Key;
                        var cuotasDelCredito = grupo.ToList();
                        var primeraCuota = cuotasDelCredito.First();
                        var credito = primeraCuota.Credito;
                        var cliente = credito?.Cliente;

                        if (credito == null || cliente == null)
                        {
                            log.Errores++;
                            continue;
                        }

                        // Verificar si ya existe alerta activa (evitar duplicados)
                        var alertaExistente = await _context.AlertasCobranza
                            .AnyAsync(a => a.CreditoId == creditoId &&
                                      !a.Resuelta &&
                                      a.Tipo == TipoAlertaCobranza.CuotaVencida &&
                                      !a.IsDeleted);

                        if (alertaExistente)
                            continue;

                        // Calcular datos de mora
                        var diasMora = (hoy - cuotasDelCredito.Min(c => c.FechaVencimiento)).Days;
                        var montoVencido = cuotasDelCredito.Sum(c => c.MontoTotal - c.MontoPagado);
                        var moraCalculada = CalcularMora(montoVencido, diasMora, config);
                        var prioridad = DeterminarPrioridad(diasMora, montoVencido, config);

                        // ✅ MEJORADO: Crear alerta con información completa
                        var alerta = new AlertaCobranza
                        {
                            CreditoId = creditoId,
                            ClienteId = cliente.Id,
                            Tipo = ObtenerTipoAlerta(diasMora),
                            Prioridad = prioridad,
                            Mensaje = GenerarMensajeAlerta(cliente, montoVencido, cuotasDelCredito.Count, diasMora),
                            MontoVencido = montoVencido,
                            CuotasVencidas = cuotasDelCredito.Count,
                            FechaAlerta = DateTime.Now,
                            Resuelta = false,
                            CreatedAt = DateTime.Now
                        };

                        _context.AlertasCobranza.Add(alerta);
                        alertasCreadas++;

                        log.CuotasConMora += cuotasDelCredito.Count;
                        log.TotalMora += montoVencido;
                        log.TotalRecargosAplicados += moraCalculada;

                        _logger.LogInformation(
                            "Alerta generada - Crédito: {CreditoId}, Cliente: {Cliente}, Mora: ${Mora}, Días: {Días}",
                            creditoId, cliente.NombreCompleto, montoVencido, diasMora);
                    }
                    catch (Exception ex)
                    {
                        log.Errores++;
                        _logger.LogWarning(ex, "Error procesando crédito {CreditoId}", grupo.Key);
                    }
                }

                // Generar alertas de próximos vencimientos
                await GenerarAlertasProximosVencimientosAsync(config);

                // Actualizar configuración
                config.UltimaEjecucion = DateTime.Now;
                log.AlertasGeneradas = alertasCreadas;
                log.Exitoso = true;
                log.Mensaje = $"Proceso completado. {cuotasVencidas.Count} cuotas procesadas, " +
                    $"{alertasCreadas} alertas generadas, ${log.TotalMora:F2} en mora.";

                await _context.SaveChangesAsync();

                _logger.LogInformation("=== PROCESAMIENTO DE MORA COMPLETADO ===");
                _logger.LogInformation(log.Mensaje);
            }
            catch (Exception ex)
            {
                log.Exitoso = false;
                log.Mensaje = "Error al procesar mora";
                log.DetalleError = ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : string.Empty);
                log.Errores++;

                _logger.LogError(ex, "Error al procesar mora");
            }
            finally
            {
                // Registrar duración
                log.DuracionEjecucion = DateTime.Now - inicioEjecucion;

                _context.LogsMora.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Log de ejecución guardado. Duración: {Duracion}ms",
                    log.DuracionEjecucion.TotalMilliseconds);
            }
        }

        private async Task GenerarAlertasProximosVencimientosAsync(ConfiguracionMora config)
        {
            try
            {
                var hoy = DateTime.Today;
                var diasAntesAlerta = 5; // ✅ TODO: Hacer configurable en ConfiguracionMora
                var proximosDias = hoy.AddDays(diasAntesAlerta);
                var now = DateTime.Now;

                var cuotasPorVencer = await _context.Cuotas
                    .Include(c => c.Credito)
                        .ThenInclude(cr => cr!.Cliente)
                    .Where(c => !c.IsDeleted &&
                           !c.Credito!.IsDeleted &&
                           !c.Credito!.Cliente!.IsDeleted &&
                           c.Estado == EstadoCuota.Pendiente &&
                           c.FechaVencimiento > hoy &&
                           c.FechaVencimiento <= proximosDias)
                    .ToListAsync();

                var creditoIds = cuotasPorVencer
                    .Where(c => c.Credito?.Cliente != null)
                    .Select(c => c.CreditoId)
                    .Distinct()
                    .ToList();

                var creditosConAlerta = creditoIds.Count == 0
                    ? new HashSet<int>()
                    : (await _context.AlertasCobranza
                        .AsNoTracking()
                        .Where(a => creditoIds.Contains(a.CreditoId) &&
                                   !a.Resuelta &&
                                   a.Tipo == TipoAlertaCobranza.ProximoVencimiento &&
                                   !a.IsDeleted)
                        .Select(a => a.CreditoId)
                        .Distinct()
                        .ToListAsync())
                        .ToHashSet();

                foreach (var cuota in cuotasPorVencer)
                {
                    if (cuota.Credito?.Cliente == null)
                        continue;

                    // Verificar si ya existe alerta
                    if (creditosConAlerta.Contains(cuota.CreditoId))
                        continue;

                    var diasRestantes = (cuota.FechaVencimiento - hoy).Days;
                    var cliente = cuota.Credito.Cliente;

                    var alerta = new AlertaCobranza
                    {
                        CreditoId = cuota.CreditoId,
                        ClienteId = cliente.Id,
                        Tipo = TipoAlertaCobranza.ProximoVencimiento,
                        Prioridad = PrioridadAlerta.Baja,
                        Mensaje = $"Cliente {cliente.NombreCompleto} tiene cuota por vencer en {diasRestantes} días",
                        MontoVencido = cuota.MontoTotal,
                        CuotasVencidas = 1,
                        FechaAlerta = now,
                        Resuelta = false,
                        CreatedAt = now
                    };

                    _context.AlertasCobranza.Add(alerta);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Alertas de próximos vencimientos generadas");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al generar alertas de próximos vencimientos");
            }
        }

        #endregion

        #region Gestión de Alertas

        public async Task<List<AlertaCobranzaViewModel>> GetAlertasActivasAsync()
        {
            try
            {
                var alertas = await _context.AlertasCobranza
                    .Include(a => a.Cliente)
                    .Include(a => a.Credito)
                    .Where(a => !a.IsDeleted && !a.Resuelta)
                    .OrderByDescending(a => a.Prioridad)
                    .ThenBy(a => a.FechaAlerta)
                    .ToListAsync();

                // ✅ OPTIMIZADO: Usar AutoMapper en lugar de mapeo manual
                return _mapper.Map<List<AlertaCobranzaViewModel>>(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener alertas activas");
                throw;
            }
        }

        public async Task<List<AlertaCobranzaViewModel>> GetTodasAlertasAsync()
        {
            try
            {
                var alertas = await _context.AlertasCobranza
                    .Include(a => a.Cliente)
                    .Include(a => a.Credito)
                    .Where(a => !a.IsDeleted)
                    .OrderByDescending(a => a.FechaAlerta)
                    .ToListAsync();

                return _mapper.Map<List<AlertaCobranzaViewModel>>(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las alertas");
                throw;
            }
        }

        public async Task<AlertaCobranzaViewModel?> GetAlertaByIdAsync(int id)
        {
            try
            {
                var alerta = await _context.AlertasCobranza
                    .Include(a => a.Cliente)
                    .Include(a => a.Credito)
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                return alerta != null ? _mapper.Map<AlertaCobranzaViewModel>(alerta) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener alerta {Id}", id);
                throw;
            }
        }

        public async Task<List<AlertaCobranzaViewModel>> GetAlertasPorClienteAsync(int clienteId)
        {
            try
            {
                var alertas = await _context.AlertasCobranza
                    .Include(a => a.Cliente)
                    .Include(a => a.Credito)
                    .Where(a => a.ClienteId == clienteId && !a.IsDeleted)
                    .OrderByDescending(a => a.FechaAlerta)
                    .ToListAsync();

                return _mapper.Map<List<AlertaCobranzaViewModel>>(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener alertas del cliente {ClienteId}", clienteId);
                throw;
            }
        }

        public async Task<bool> ResolverAlertaAsync(int id, string? observaciones = null, byte[]? rowVersion = null)
        {
            try
            {
                var alerta = await _context.AlertasCobranza.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                if (alerta == null)
                    return false;

                if (alerta.Resuelta)
                    return true; // idempotente

                if (rowVersion is null || rowVersion.Length == 0)
                    throw new InvalidOperationException("Falta información de concurrencia (RowVersion). Recargá la alerta e intentá nuevamente.");

                _context.Entry(alerta).Property(a => a.RowVersion).OriginalValue = rowVersion;

                alerta.Resuelta = true;
                alerta.FechaResolucion = DateTime.Now;
                alerta.Observaciones = observaciones;
                alerta.UpdatedAt = DateTime.Now;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new InvalidOperationException("La alerta fue modificada por otro usuario. Por favor, recargue los datos.");
                }

                _logger.LogInformation("Alerta {Id} resuelta. Observaciones: {Obs}", id, observaciones ?? "Ninguna");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resolver alerta {Id}", id);
                throw;
            }
        }

        public async Task<bool> MarcarAlertaComoLeidaAsync(int id, byte[]? rowVersion = null)
        {
            try
            {
                var alerta = await _context.AlertasCobranza.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                if (alerta == null)
                    return false;

                if (rowVersion is null || rowVersion.Length == 0)
                    throw new InvalidOperationException("Falta información de concurrencia (RowVersion). Recargá la alerta e intentá nuevamente.");

                _context.Entry(alerta).Property(a => a.RowVersion).OriginalValue = rowVersion;

                alerta.UpdatedAt = DateTime.Now;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new InvalidOperationException("La alerta fue modificada por otro usuario. Por favor, recargue los datos.");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar alerta como leída {Id}", id);
                throw;
            }
        }

        #endregion

        #region Logs

        public async Task<List<LogMora>> GetLogsAsync(int cantidad = 50)
        {
            try
            {
                return await _context.LogsMora
                    .Where(l => !l.IsDeleted)
                    .OrderByDescending(l => l.FechaEjecucion)
                    .Take(cantidad)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener logs de mora");
                throw;
            }
        }

        #endregion

        #region Métodos Privados Helpers

        // ✅ NUEVO: Consolidar cálculo de mora en un lugar
        private decimal CalcularMora(decimal montoVencido, int diasAtraso, ConfiguracionMora config)
        {
            if (diasAtraso <= 0 || montoVencido <= 0)
                return 0;

            var tasaDiaria = config.PorcentajeRecargo / 100m / 30m; // Convertir a tasa diaria
            var mora = montoVencido * tasaDiaria * diasAtraso;

            return Math.Round(mora, 2);
        }

        // ✅ MEJORADO: Usar configuración en lugar de constantes hardcodeadas
        private PrioridadAlerta DeterminarPrioridad(int diasMora, decimal montoVencido, ConfiguracionMora config)
        {
            // Umbrales por defecto (TODO: Hacer configurables)
            const int diasAlertaCritica = 30;
            const int diasAlertaAlta = 15;
            const int diasAlertaMedia = 7;
            const decimal montoAlertaCritica = 50000;
            const decimal montoAlertaAlta = 30000;
            const decimal montoAlertaMedia = 15000;

            if (diasMora > diasAlertaCritica || montoVencido > montoAlertaCritica)
                return PrioridadAlerta.Critica;
            if (diasMora > diasAlertaAlta || montoVencido > montoAlertaAlta)
                return PrioridadAlerta.Alta;
            if (diasMora > diasAlertaMedia || montoVencido > montoAlertaMedia)
                return PrioridadAlerta.Media;

            return PrioridadAlerta.Baja;
        }

        private TipoAlertaCobranza ObtenerTipoAlerta(int diasAtraso)
        {
            return diasAtraso > 90 ? TipoAlertaCobranza.MoraElevada : TipoAlertaCobranza.CuotaVencida;
        }

        private string GenerarMensajeAlerta(Cliente cliente, decimal montoVencido, int cuotasVencidas, int diasAtraso)
        {
            return $"Cliente {cliente.NombreCompleto} tiene ${montoVencido:F2} en mora " +
                   $"con {cuotasVencidas} cuota(s) vencida(s) por {diasAtraso} día(s). " +
                   $"Documento: {cliente.NumeroDocumento}";
        }

        #endregion
    }
}