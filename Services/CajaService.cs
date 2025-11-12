using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    public class CajaService : ICajaService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CajaService> _logger;
        private readonly INotificacionService _notificacionService;

        public CajaService(
            AppDbContext context,
            ILogger<CajaService> logger,
            INotificacionService notificacionService)
        {
            _context = context;
            _logger = logger;
            _notificacionService = notificacionService;
        }

        #region CRUD de Cajas

        public async Task<List<Caja>> ObtenerTodasCajasAsync()
        {
            return await _context.Cajas
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Codigo)
                .ToListAsync();
        }

        public async Task<Caja?> ObtenerCajaPorIdAsync(int id)
        {
            return await _context.Cajas
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<Caja> CrearCajaAsync(CajaViewModel model)
        {
            // Validar código único
            if (await ExisteCodigoCajaAsync(model.Codigo))
            {
                throw new InvalidOperationException($"Ya existe una caja con el código '{model.Codigo}'");
            }

            var caja = new Caja
            {
                Codigo = model.Codigo,
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Sucursal = model.Sucursal,
                Ubicacion = model.Ubicacion,
                Activa = model.Activa,
                Estado = EstadoCaja.Cerrada
            };

            _context.Cajas.Add(caja);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Caja creada: {caja.Codigo} - {caja.Nombre}");

            return caja;
        }

        public async Task<Caja> ActualizarCajaAsync(int id, CajaViewModel model)
        {
            var caja = await ObtenerCajaPorIdAsync(id);
            if (caja == null)
            {
                throw new InvalidOperationException("Caja no encontrada");
            }

            // Validar código único (excluyendo la caja actual)
            if (await ExisteCodigoCajaAsync(model.Codigo, id))
            {
                throw new InvalidOperationException($"Ya existe otra caja con el código '{model.Codigo}'");
            }

            // No permitir desactivar si está abierta
            if (!model.Activa && caja.Estado == EstadoCaja.Abierta)
            {
                throw new InvalidOperationException("No se puede desactivar una caja que está abierta");
            }

            caja.Codigo = model.Codigo;
            caja.Nombre = model.Nombre;
            caja.Descripcion = model.Descripcion;
            caja.Sucursal = model.Sucursal;
            caja.Ubicacion = model.Ubicacion;
            caja.Activa = model.Activa;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Caja actualizada: {caja.Codigo}");

            return caja;
        }

        public async Task EliminarCajaAsync(int id)
        {
            var caja = await ObtenerCajaPorIdAsync(id);
            if (caja == null)
            {
                throw new InvalidOperationException("Caja no encontrada");
            }

            // No permitir eliminar si está abierta
            if (caja.Estado == EstadoCaja.Abierta)
            {
                throw new InvalidOperationException("No se puede eliminar una caja que está abierta");
            }

            // Soft delete
            caja.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Caja eliminada: {caja.Codigo}");
        }

        public async Task<bool> ExisteCodigoCajaAsync(string codigo, int? cajaIdExcluir = null)
        {
            var query = _context.Cajas.Where(c => c.Codigo == codigo && !c.IsDeleted);

            if (cajaIdExcluir.HasValue)
            {
                query = query.Where(c => c.Id != cajaIdExcluir.Value);
            }

            return await query.AnyAsync();
        }

        #endregion

        #region Apertura de Caja

        public async Task<AperturaCaja> AbrirCajaAsync(AbrirCajaViewModel model, string usuario)
        {
            var caja = await ObtenerCajaPorIdAsync(model.CajaId);
            if (caja == null)
            {
                throw new InvalidOperationException("Caja no encontrada");
            }

            if (!caja.Activa)
            {
                throw new InvalidOperationException("La caja no está activa");
            }

            // Verificar que no esté ya abierta
            if (await TieneCajaAbiertaAsync(model.CajaId))
            {
                throw new InvalidOperationException("La caja ya tiene una apertura activa");
            }

            var apertura = new AperturaCaja
            {
                CajaId = model.CajaId,
                FechaApertura = DateTime.Now,
                MontoInicial = model.MontoInicial,
                UsuarioApertura = usuario,
                ObservacionesApertura = model.ObservacionesApertura,
                Cerrada = false
            };

            _context.AperturasCaja.Add(apertura);

            // Actualizar estado de la caja
            caja.Estado = EstadoCaja.Abierta;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Caja abierta: {caja.Codigo} por {usuario} con monto inicial ${model.MontoInicial}");

            // Crear notificación para supervisores
            await _notificacionService.CrearNotificacionParaRolAsync(
                "Supervisor",
                TipoNotificacion.CajaAbierta,
                "Caja Abierta",
                $"Caja {caja.Codigo} abierta por {usuario} con monto inicial ${model.MontoInicial:N2}",
                $"/Caja/DetallesApertura/{apertura.Id}",
                PrioridadNotificacion.Baja
            );

            return apertura;
        }

        public async Task<AperturaCaja?> ObtenerAperturaActivaAsync(int cajaId)
        {
            return await _context.AperturasCaja
                .Include(a => a.Caja)
                .Include(a => a.Movimientos)
                .FirstOrDefaultAsync(a => a.CajaId == cajaId && !a.Cerrada && !a.IsDeleted);
        }

        public async Task<AperturaCaja?> ObtenerAperturaPorIdAsync(int id)
        {
            return await _context.AperturasCaja
                .Include(a => a.Caja)
                .Include(a => a.Movimientos)
                .Include(a => a.Cierre)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<List<AperturaCaja>> ObtenerAperturasAbiertasAsync()
        {
            return await _context.AperturasCaja
                .Include(a => a.Caja)
                .Where(a => !a.Cerrada && !a.IsDeleted)
                .OrderByDescending(a => a.FechaApertura)
                .ToListAsync();
        }

        public async Task<bool> TieneCajaAbiertaAsync(int cajaId)
        {
            return await _context.AperturasCaja
                .AnyAsync(a => a.CajaId == cajaId && !a.Cerrada && !a.IsDeleted);
        }

        #endregion

        #region Movimientos de Caja

        public async Task<MovimientoCaja> RegistrarMovimientoAsync(MovimientoCajaViewModel model, string usuario)
        {
            var apertura = await ObtenerAperturaPorIdAsync(model.AperturaCajaId);
            if (apertura == null)
            {
                throw new InvalidOperationException("Apertura de caja no encontrada");
            }

            if (apertura.Cerrada)
            {
                throw new InvalidOperationException("No se pueden registrar movimientos en una caja cerrada");
            }

            var movimiento = new MovimientoCaja
            {
                AperturaCajaId = model.AperturaCajaId,
                FechaMovimiento = DateTime.Now,
                Tipo = model.Tipo,
                Concepto = model.Concepto,
                Monto = model.Monto,
                Descripcion = model.Descripcion,
                Referencia = model.Referencia,
                Usuario = usuario,
                Observaciones = model.Observaciones
            };

            _context.MovimientosCaja.Add(movimiento);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Movimiento registrado en caja: {movimiento.Tipo} - ${movimiento.Monto}");

            return movimiento;
        }

        public async Task<List<MovimientoCaja>> ObtenerMovimientosDeAperturaAsync(int aperturaId)
        {
            return await _context.MovimientosCaja
                .Where(m => m.AperturaCajaId == aperturaId && !m.IsDeleted)
                .OrderBy(m => m.FechaMovimiento)
                .ToListAsync();
        }

        public async Task<decimal> CalcularSaldoActualAsync(int aperturaId)
        {
            var apertura = await ObtenerAperturaPorIdAsync(aperturaId);
            if (apertura == null)
            {
                return 0;
            }

            var ingresos = await _context.MovimientosCaja
                .Where(m => m.AperturaCajaId == aperturaId &&
                           m.Tipo == TipoMovimientoCaja.Ingreso &&
                           !m.IsDeleted)
                .SumAsync(m => (decimal?)m.Monto) ?? 0;

            var egresos = await _context.MovimientosCaja
                .Where(m => m.AperturaCajaId == aperturaId &&
                           m.Tipo == TipoMovimientoCaja.Egreso &&
                           !m.IsDeleted)
                .SumAsync(m => (decimal?)m.Monto) ?? 0;

            return apertura.MontoInicial + ingresos - egresos;
        }

        #endregion

        #region Cierre de Caja

        public async Task<CierreCaja> CerrarCajaAsync(CerrarCajaViewModel model, string usuario)
        {
            var apertura = await ObtenerAperturaPorIdAsync(model.AperturaCajaId);
            if (apertura == null)
            {
                throw new InvalidOperationException("Apertura de caja no encontrada");
            }

            if (apertura.Cerrada)
            {
                throw new InvalidOperationException("La caja ya está cerrada");
            }

            // Calcular totales del sistema
            var ingresos = await _context.MovimientosCaja
                .Where(m => m.AperturaCajaId == model.AperturaCajaId &&
                           m.Tipo == TipoMovimientoCaja.Ingreso &&
                           !m.IsDeleted)
                .SumAsync(m => (decimal?)m.Monto) ?? 0;

            var egresos = await _context.MovimientosCaja
                .Where(m => m.AperturaCajaId == model.AperturaCajaId &&
                           m.Tipo == TipoMovimientoCaja.Egreso &&
                           !m.IsDeleted)
                .SumAsync(m => (decimal?)m.Monto) ?? 0;

            var montoEsperado = apertura.MontoInicial + ingresos - egresos;
            var montoReal = model.EfectivoContado + model.ChequesContados + model.ValesContados;
            var diferencia = montoReal - montoEsperado;

            // Validar justificación si hay diferencia
            if (Math.Abs(diferencia) > 0.01m && string.IsNullOrWhiteSpace(model.JustificacionDiferencia))
            {
                throw new InvalidOperationException("Debe proporcionar una justificación para la diferencia encontrada");
            }

            var cierre = new CierreCaja
            {
                AperturaCajaId = model.AperturaCajaId,
                FechaCierre = DateTime.Now,
                MontoInicialSistema = apertura.MontoInicial,
                TotalIngresosSistema = ingresos,
                TotalEgresosSistema = egresos,
                MontoEsperadoSistema = montoEsperado,
                EfectivoContado = model.EfectivoContado,
                ChequesContados = model.ChequesContados,
                ValesContados = model.ValesContados,
                MontoTotalReal = montoReal,
                Diferencia = diferencia,
                TieneDiferencia = Math.Abs(diferencia) > 0.01m,
                JustificacionDiferencia = model.JustificacionDiferencia,
                UsuarioCierre = usuario,
                ObservacionesCierre = model.ObservacionesCierre,
                DetalleArqueo = model.DetalleArqueo
            };

            _context.CierresCaja.Add(cierre);

            // Marcar apertura como cerrada
            apertura.Cerrada = true;

            // Actualizar estado de la caja
            apertura.Caja.Estado = EstadoCaja.Cerrada;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Caja cerrada: {apertura.Caja.Codigo} por {usuario}. Diferencia: ${diferencia}");

            // Crear notificaciones según el resultado del cierre
            if (cierre.TieneDiferencia)
            {
                // Notificación de alta prioridad si hay diferencia
                var tipoDiferencia = diferencia > 0 ? "sobrante" : "faltante";
                await _notificacionService.CrearNotificacionParaRolAsync(
                    "Supervisor",
                    TipoNotificacion.CierreConDiferencia,
                    "Cierre de Caja con Diferencia",
                    $"Caja {apertura.Caja.Codigo} cerrada con ${Math.Abs(diferencia):N2} {tipoDiferencia}. Usuario: {usuario}",
                    $"/Caja/DetallesCierre/{cierre.Id}",
                    PrioridadNotificacion.Alta
                );
            }
            else
            {
                // Notificación normal si el cierre es exacto
                await _notificacionService.CrearNotificacionParaRolAsync(
                    "Supervisor",
                    TipoNotificacion.CajaCerrada,
                    "Caja Cerrada",
                    $"Caja {apertura.Caja.Codigo} cerrada sin diferencias por {usuario}",
                    $"/Caja/DetallesCierre/{cierre.Id}",
                    PrioridadNotificacion.Baja
                );
            }

            return cierre;
        }

        public async Task<CierreCaja?> ObtenerCierrePorIdAsync(int id)
        {
            return await _context.CierresCaja
                .Include(c => c.AperturaCaja)
                    .ThenInclude(a => a.Caja)
                .Include(c => c.AperturaCaja)
                    .ThenInclude(a => a.Movimientos)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<List<CierreCaja>> ObtenerHistorialCierresAsync(
            int? cajaId = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            var query = _context.CierresCaja
                .Include(c => c.AperturaCaja)
                    .ThenInclude(a => a.Caja)
                .Where(c => !c.IsDeleted);

            if (cajaId.HasValue)
            {
                query = query.Where(c => c.AperturaCaja.CajaId == cajaId.Value);
            }

            if (fechaDesde.HasValue)
            {
                query = query.Where(c => c.FechaCierre >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                var fechaHastaFin = fechaHasta.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(c => c.FechaCierre <= fechaHastaFin);
            }

            return await query
                .OrderByDescending(c => c.FechaCierre)
                .ToListAsync();
        }

        #endregion

        #region Reportes y Estadísticas

        public async Task<DetallesAperturaViewModel> ObtenerDetallesAperturaAsync(int aperturaId)
        {
            var apertura = await ObtenerAperturaPorIdAsync(aperturaId);
            if (apertura == null)
            {
                throw new InvalidOperationException("Apertura no encontrada");
            }

            var movimientos = await ObtenerMovimientosDeAperturaAsync(aperturaId);

            var totalIngresos = movimientos
                .Where(m => m.Tipo == TipoMovimientoCaja.Ingreso)
                .Sum(m => m.Monto);

            var totalEgresos = movimientos
                .Where(m => m.Tipo == TipoMovimientoCaja.Egreso)
                .Sum(m => m.Monto);

            var saldoActual = apertura.MontoInicial + totalIngresos - totalEgresos;

            return new DetallesAperturaViewModel
            {
                Apertura = apertura,
                Movimientos = movimientos,
                SaldoActual = saldoActual,
                TotalIngresos = totalIngresos,
                TotalEgresos = totalEgresos,
                CantidadMovimientos = movimientos.Count
            };
        }

        public async Task<ReporteCajaViewModel> GenerarReporteCajaAsync(
            DateTime fechaDesde,
            DateTime fechaHasta,
            int? cajaId = null)
        {
            var query = _context.AperturasCaja
                .Include(a => a.Caja)
                .Include(a => a.Movimientos)
                .Include(a => a.Cierre)
                .Where(a => !a.IsDeleted &&
                           a.FechaApertura >= fechaDesde &&
                           a.FechaApertura <= fechaHasta);

            if (cajaId.HasValue)
            {
                query = query.Where(a => a.CajaId == cajaId.Value);
            }

            var aperturas = await query.ToListAsync();

            var totalIngresos = aperturas
                .SelectMany(a => a.Movimientos)
                .Where(m => m.Tipo == TipoMovimientoCaja.Ingreso && !m.IsDeleted)
                .Sum(m => m.Monto);

            var totalEgresos = aperturas
                .SelectMany(a => a.Movimientos)
                .Where(m => m.Tipo == TipoMovimientoCaja.Egreso && !m.IsDeleted)
                .Sum(m => m.Monto);

            var totalDiferencias = aperturas
                .Where(a => a.Cierre != null)
                .Sum(a => a.Cierre!.Diferencia);

            return new ReporteCajaViewModel
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                CajaId = cajaId,
                Aperturas = aperturas,
                TotalIngresos = totalIngresos,
                TotalEgresos = totalEgresos,
                TotalDiferencias = totalDiferencias,
                TotalAperturas = aperturas.Count
            };
        }

        public async Task<HistorialCierresViewModel> ObtenerEstadisticasCierresAsync(
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            var query = _context.CierresCaja
                .Include(c => c.AperturaCaja)
                    .ThenInclude(a => a.Caja)
                .Where(c => !c.IsDeleted);

            if (fechaDesde.HasValue)
            {
                query = query.Where(c => c.FechaCierre >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                var fechaHastaFin = fechaHasta.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(c => c.FechaCierre <= fechaHastaFin);
            }

            var cierres = await query
                .OrderByDescending(c => c.FechaCierre)
                .ToListAsync();

            var totalCierres = cierres.Count;
            var cierresConDiferencia = cierres.Count(c => c.TieneDiferencia);
            var porcentajeCierresExactos = totalCierres > 0
                ? ((totalCierres - cierresConDiferencia) / (decimal)totalCierres) * 100
                : 0;

            var totalDiferenciasPositivas = cierres
                .Where(c => c.Diferencia > 0)
                .Sum(c => c.Diferencia);

            var totalDiferenciasNegativas = cierres
                .Where(c => c.Diferencia < 0)
                .Sum(c => c.Diferencia);

            return new HistorialCierresViewModel
            {
                Cierres = cierres,
                TotalDiferenciasPositivas = totalDiferenciasPositivas,
                TotalDiferenciasNegativas = totalDiferenciasNegativas,
                CierresConDiferencia = cierresConDiferencia,
                TotalCierres = totalCierres,
                PorcentajeCierresExactos = porcentajeCierresExactos
            };
        }

        #endregion
    }
}