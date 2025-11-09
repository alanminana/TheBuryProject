using AutoMapper;
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
        private readonly IMapper _mapper;

        public MoraService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ConfiguracionMoraViewModel> GetConfiguracionAsync()
        {
            var config = await _context.ConfiguracionesMora
                .FirstOrDefaultAsync(c => !c.IsDeleted);

            if (config == null)
            {
                config = new ConfiguracionMora
                {
                    TasaMoraDiaria = 0.05m,
                    DiasGraciaMora = 3,
                    PorcentajeRecargoPrimerMes = 5.0m,
                    PorcentajeRecargoSegundoMes = 10.0m,
                    PorcentajeRecargoTercerMes = 15.0m,
                    DiasAntesAlertaVencimiento = 7,
                    JobActivo = true,
                    HoraEjecucion = new TimeSpan(2, 0, 0)
                };
                _context.ConfiguracionesMora.Add(config);
                await _context.SaveChangesAsync();
            }

            return _mapper.Map<ConfiguracionMoraViewModel>(config);
        }

        public async Task SaveConfiguracionAsync(ConfiguracionMoraViewModel model)
        {
            var config = await _context.ConfiguracionesMora
                .FirstOrDefaultAsync(c => !c.IsDeleted && c.Id == model.Id);

            if (config == null)
            {
                config = _mapper.Map<ConfiguracionMora>(model);
                _context.ConfiguracionesMora.Add(config);
            }
            else
            {
                _mapper.Map(model, config);
                config.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<LogMora> ProcesarMoraAsync()
        {
            var inicio = DateTime.Now;
            var log = new LogMora
            {
                FechaEjecucion = inicio,
                Exitoso = true
            };

            try
            {
                var configVm = await GetConfiguracionAsync();
                var config = await _context.ConfiguracionesMora.FindAsync(configVm.Id);

                // Obtener cuotas pendientes y vencidas
                var cuotasPendientes = await _context.Cuotas
                    .Include(c => c.Credito)
                        .ThenInclude(cr => cr.Cliente)
                    .Where(c => !c.IsDeleted &&
                               c.Estado != EstadoCuota.Pagada &&
                               c.Estado != EstadoCuota.Cancelada &&
                               c.FechaVencimiento < DateTime.Today)
                    .ToListAsync();

                decimal totalMora = 0;
                decimal totalRecargos = 0;
                int alertasGeneradas = 0;
                int cuotasConMora = 0;

                foreach (var cuota in cuotasPendientes)
                {
                    var diasVencidos = (DateTime.Today - cuota.FechaVencimiento).Days;

                    // Aplicar período de gracia
                    if (diasVencidos <= config.DiasGraciaMora)
                    {
                        continue;
                    }

                    var diasMoraReal = diasVencidos - config.DiasGraciaMora;

                    // Calcular mora diaria
                    var moraDiaria = cuota.MontoTotal * (config.TasaMoraDiaria / 100) * diasMoraReal;

                    // Determinar recargo mensual según días de mora
                    decimal porcentajeRecargo = diasMoraReal > 60
                        ? config.PorcentajeRecargoTercerMes
                        : diasMoraReal > 30
                            ? config.PorcentajeRecargoSegundoMes
                            : config.PorcentajeRecargoPrimerMes;

                    var recargoMensual = cuota.MontoTotal * (porcentajeRecargo / 100);

                    // Actualizar monto punitorio
                    cuota.MontoPunitorio = moraDiaria + recargoMensual;
                    cuota.Estado = EstadoCuota.Vencida;

                    totalMora += cuota.MontoPunitorio;
                    totalRecargos += recargoMensual;
                    cuotasConMora++;

                    // Generar alertas según nivel de mora
                    var tipoAlerta = DeterminarTipoAlerta(diasMoraReal);
                    var prioridad = DeterminarPrioridad(diasMoraReal);

                    // Verificar si ya existe una alerta activa para esta cuota
                    var alertaExistente = await _context.AlertasCobranza
                        .AnyAsync(a => !a.IsDeleted &&
                                      a.CuotaId == cuota.Id &&
                                      !a.Resuelta);

                    if (!alertaExistente)
                    {
                        var alerta = new AlertaCobranza
                        {
                            CuotaId = cuota.Id,
                            CreditoId = cuota.CreditoId,
                            ClienteId = cuota.Credito.ClienteId,
                            Tipo = tipoAlerta,
                            Titulo = $"Cuota {cuota.NumeroCuota} - {ObtenerDescripcionTipo(tipoAlerta)}",
                            Mensaje = $"Cliente: {cuota.Credito.Cliente.NombreCompleto}. " +
                                     $"Cuota #{cuota.NumeroCuota} vencida hace {diasVencidos} días. " +
                                     $"Monto adeudado: ${cuota.MontoTotal:N2} + Mora: ${cuota.MontoPunitorio:N2}",
                            Prioridad = prioridad,
                            Leida = false,
                            Resuelta = false
                        };

                        _context.AlertasCobranza.Add(alerta);
                        alertasGeneradas++;
                    }
                }

                // Generar alertas de próximo vencimiento
                await GenerarAlertasVencimientoAsync();

                log.CuotasProcesadas = cuotasPendientes.Count;
                log.CuotasConMora = cuotasConMora;
                log.AlertasGeneradas = alertasGeneradas;
                log.TotalMora = totalMora;
                log.TotalRecargosAplicados = totalRecargos;

                // Actualizar última ejecución en configuración
                config.UltimaEjecucion = DateTime.Now;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                log.Exitoso = false;
                log.Errores = ex.Message;
            }

            var duracion = (DateTime.Now - inicio).TotalSeconds;
            log.DuracionSegundos = (int)duracion;

            _context.LogsMora.Add(log);
            await _context.SaveChangesAsync();

            return log;
        }

        public async Task GenerarAlertasVencimientoAsync()
        {
            var configVm = await GetConfiguracionAsync();
            var config = await _context.ConfiguracionesMora.FindAsync(configVm.Id);

            var cuotasProximasAVencer = await _context.Cuotas
                .Include(c => c.Credito)
                    .ThenInclude(cr => cr.Cliente)
                .Where(c => !c.IsDeleted &&
                           c.Estado == EstadoCuota.Pendiente &&
                           c.FechaVencimiento > DateTime.Today &&
                           c.FechaVencimiento <= DateTime.Today.AddDays(config.DiasAntesAlertaVencimiento))
                .ToListAsync();

            foreach (var cuota in cuotasProximasAVencer)
            {
                var diasHastaVencimiento = (cuota.FechaVencimiento - DateTime.Today).Days;

                var alertaExistente = await _context.AlertasCobranza
                    .AnyAsync(a => !a.IsDeleted &&
                                  a.CuotaId == cuota.Id &&
                                  a.Tipo == TipoAlerta.ProximoVencimiento &&
                                  !a.Resuelta);

                if (!alertaExistente)
                {
                    var alerta = new AlertaCobranza
                    {
                        CuotaId = cuota.Id,
                        CreditoId = cuota.CreditoId,
                        ClienteId = cuota.Credito.ClienteId,
                        Tipo = TipoAlerta.ProximoVencimiento,
                        Titulo = $"Cuota {cuota.NumeroCuota} - Próximo Vencimiento",
                        Mensaje = $"Cliente: {cuota.Credito.Cliente.NombreCompleto}. " +
                                 $"Cuota #{cuota.NumeroCuota} vence en {diasHastaVencimiento} días ({cuota.FechaVencimiento:dd/MM/yyyy}). " +
                                 $"Monto: ${cuota.MontoTotal:N2}",
                        Prioridad = 1,
                        Leida = false,
                        Resuelta = false
                    };

                    _context.AlertasCobranza.Add(alerta);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<AlertaCobranzaViewModel>> GetAlertasActivasAsync()
        {
            var alertas = await _context.AlertasCobranza
                .Include(a => a.Cliente)
                .Include(a => a.Cuota)
                .Include(a => a.Credito)
                .Where(a => !a.IsDeleted && !a.Resuelta)
                .OrderByDescending(a => a.Prioridad)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            var viewModels = _mapper.Map<List<AlertaCobranzaViewModel>>(alertas);

            for (int i = 0; i < viewModels.Count; i++)
            {
                viewModels[i].ClienteNombre = alertas[i].Cliente.NombreCompleto;
            }

            return viewModels;
        }

        public async Task<List<LogMora>> GetLogsAsync(int cantidad = 20)
        {
            return await _context.LogsMora
                .Where(l => !l.IsDeleted)
                .OrderByDescending(l => l.FechaEjecucion)
                .Take(cantidad)
                .ToListAsync();
        }

        public async Task MarcarAlertaLeidaAsync(int alertaId, string usuario)
        {
            var alerta = await _context.AlertasCobranza.FindAsync(alertaId);
            if (alerta != null && !alerta.IsDeleted)
            {
                alerta.Leida = true;
                alerta.UpdatedAt = DateTime.UtcNow;
                alerta.UpdatedBy = usuario;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ResolverAlertaAsync(int alertaId, string usuario, string nota)
        {
            var alerta = await _context.AlertasCobranza.FindAsync(alertaId);
            if (alerta != null && !alerta.IsDeleted)
            {
                alerta.Resuelta = true;
                alerta.Leida = true;
                alerta.UpdatedAt = DateTime.UtcNow;
                alerta.UpdatedBy = usuario;
                if (!string.IsNullOrEmpty(nota))
                {
                    alerta.Mensaje += $" | Resolución: {nota}";
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task ActualizarMoraCuotaAsync(int cuotaId)
        {
            var cuota = await _context.Cuotas
                .Include(c => c.Credito)
                .FirstOrDefaultAsync(c => c.Id == cuotaId && !c.IsDeleted);

            if (cuota == null || cuota.Estado == EstadoCuota.Pagada || cuota.Estado == EstadoCuota.Cancelada)
                return;

            var configVm = await GetConfiguracionAsync();
            var config = await _context.ConfiguracionesMora.FindAsync(configVm.Id);

            var diasVencidos = (DateTime.Today - cuota.FechaVencimiento).Days;

            if (diasVencidos <= config.DiasGraciaMora)
            {
                cuota.MontoPunitorio = 0;
                return;
            }

            var diasMoraReal = diasVencidos - config.DiasGraciaMora;
            var moraDiaria = cuota.MontoTotal * (config.TasaMoraDiaria / 100) * diasMoraReal;

            decimal porcentajeRecargo = diasMoraReal > 60
                ? config.PorcentajeRecargoTercerMes
                : diasMoraReal > 30
                    ? config.PorcentajeRecargoSegundoMes
                    : config.PorcentajeRecargoPrimerMes;

            var recargoMensual = cuota.MontoTotal * (porcentajeRecargo / 100);
            cuota.MontoPunitorio = moraDiaria + recargoMensual;
            cuota.Estado = EstadoCuota.Vencida;

            await _context.SaveChangesAsync();
        }

        public async Task<decimal> CalcularRecargoMoraAsync(int cuotaId)
        {
            var cuota = await _context.Cuotas.FindAsync(cuotaId);
            if (cuota == null || cuota.Estado == EstadoCuota.Pagada)
                return 0;

            var configVm = await GetConfiguracionAsync();
            var config = await _context.ConfiguracionesMora.FindAsync(configVm.Id);

            var diasVencidos = (DateTime.Today - cuota.FechaVencimiento).Days;

            if (diasVencidos <= config.DiasGraciaMora)
                return 0;

            var diasMoraReal = diasVencidos - config.DiasGraciaMora;
            var moraDiaria = cuota.MontoTotal * (config.TasaMoraDiaria / 100) * diasMoraReal;

            decimal porcentajeRecargo = diasMoraReal > 60
                ? config.PorcentajeRecargoTercerMes
                : diasMoraReal > 30
                    ? config.PorcentajeRecargoSegundoMes
                    : config.PorcentajeRecargoPrimerMes;

            var recargoMensual = cuota.MontoTotal * (porcentajeRecargo / 100);

            return moraDiaria + recargoMensual;
        }

        private TipoAlerta DeterminarTipoAlerta(int diasMora)
        {
            if (diasMora <= 15)
                return TipoAlerta.MoraLeve;
            else if (diasMora <= 30)
                return TipoAlerta.MoraModerada;
            else if (diasMora <= 60)
                return TipoAlerta.MoraGrave;
            else
                return TipoAlerta.MoraCritica;
        }

        private int DeterminarPrioridad(int diasMora)
        {
            if (diasMora <= 15)
                return 1; // Baja
            else if (diasMora <= 30)
                return 2; // Media
            else
                return 3; // Alta
        }

        private string ObtenerDescripcionTipo(TipoAlerta tipo)
        {
            return tipo switch
            {
                TipoAlerta.ProximoVencimiento => "Próximo Vencimiento",
                TipoAlerta.MoraLeve => "Mora Leve (1-15 días)",
                TipoAlerta.MoraModerada => "Mora Moderada (16-30 días)",
                TipoAlerta.MoraGrave => "Mora Grave (31-60 días)",
                TipoAlerta.MoraCritica => "Mora Crítica (+60 días)",
                _ => "Desconocido"
            };
        }
    }
}