using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services;

/// <summary>
/// Implementación del servicio de devoluciones, garantías y RMAs
/// </summary>
public class DevolucionService : IDevolucionService
{
    private readonly AppDbContext _context;
    private readonly IMovimientoStockService _movimientoStockService;

    public DevolucionService(AppDbContext context, IMovimientoStockService movimientoStockService)
    {
        _context = context;
        _movimientoStockService = movimientoStockService;
    }

    #region Devoluciones

    public async Task<List<Devolucion>> ObtenerTodasDevolucionesAsync()
    {
        return await _context.Devoluciones
            .Include(d => d.Cliente)
            .Include(d => d.Venta)
            .Include(d => d.Detalles).ThenInclude(dd => dd.Producto)
            .Where(d => !d.IsDeleted)
            .OrderByDescending(d => d.FechaDevolucion)
            .ToListAsync();
    }

    public async Task<List<Devolucion>> ObtenerDevolucionesPorClienteAsync(int clienteId)
    {
        return await _context.Devoluciones
            .Include(d => d.Venta)
            .Include(d => d.Detalles).ThenInclude(dd => dd.Producto)
            .Where(d => d.ClienteId == clienteId && !d.IsDeleted)
            .OrderByDescending(d => d.FechaDevolucion)
            .ToListAsync();
    }

    public async Task<List<Devolucion>> ObtenerDevolucionesPorEstadoAsync(EstadoDevolucion estado)
    {
        return await _context.Devoluciones
            .Include(d => d.Cliente)
            .Include(d => d.Venta)
            .Include(d => d.Detalles).ThenInclude(dd => dd.Producto)
            .Where(d => d.Estado == estado && !d.IsDeleted)
            .OrderByDescending(d => d.FechaDevolucion)
            .ToListAsync();
    }

    public async Task<Devolucion?> ObtenerDevolucionAsync(int id)
    {
        return await _context.Devoluciones
            .Include(d => d.Cliente)
            .Include(d => d.Venta)
            .Include(d => d.Detalles).ThenInclude(dd => dd.Producto)
            .Include(d => d.NotaCredito)
            .Include(d => d.RMA).ThenInclude(r => r!.Proveedor)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<Devolucion?> ObtenerDevolucionPorNumeroAsync(string numeroDevolucion)
    {
        return await _context.Devoluciones
            .Include(d => d.Cliente)
            .Include(d => d.Venta)
            .Include(d => d.Detalles).ThenInclude(dd => dd.Producto)
            .FirstOrDefaultAsync(d => d.NumeroDevolucion == numeroDevolucion && !d.IsDeleted);
    }

    public async Task<Devolucion> CrearDevolucionAsync(Devolucion devolucion, List<DevolucionDetalle> detalles)
    {
        // Validar que la venta existe
        var venta = await _context.Ventas.FindAsync(devolucion.VentaId);
        if (venta == null)
        {
            throw new InvalidOperationException("La venta no existe");
        }

        // Generar número de devolución
        devolucion.NumeroDevolucion = await GenerarNumeroDevolucionAsync();
        devolucion.Estado = EstadoDevolucion.Pendiente;

        // Calcular total
        devolucion.TotalDevolucion = detalles.Sum(d => d.Subtotal);

        // Agregar devolución
        _context.Devoluciones.Add(devolucion);
        await _context.SaveChangesAsync();

        // Agregar detalles
        foreach (var detalle in detalles)
        {
            detalle.DevolucionId = devolucion.Id;
            detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
            _context.DevolucionDetalles.Add(detalle);
        }

        await _context.SaveChangesAsync();
        return devolucion;
    }

    public async Task<Devolucion> ActualizarDevolucionAsync(Devolucion devolucion)
    {
        var existente = await ObtenerDevolucionAsync(devolucion.Id);
        if (existente == null)
        {
            throw new KeyNotFoundException($"Devolución con ID {devolucion.Id} no encontrada");
        }

        existente.Descripcion = devolucion.Descripcion;
        existente.ObservacionesInternas = devolucion.ObservacionesInternas;
        existente.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return existente;
    }

    public async Task<Devolucion> AprobarDevolucionAsync(int id, string aprobadoPor)
    {
        var devolucion = await ObtenerDevolucionAsync(id);
        if (devolucion == null)
        {
            throw new KeyNotFoundException($"Devolución con ID {id} no encontrada");
        }

        if (devolucion.Estado != EstadoDevolucion.Pendiente && devolucion.Estado != EstadoDevolucion.EnRevision)
        {
            throw new InvalidOperationException($"No se puede aprobar una devolución en estado {devolucion.Estado}");
        }

        devolucion.Estado = EstadoDevolucion.Aprobada;
        devolucion.AprobadoPor = aprobadoPor;
        devolucion.FechaAprobacion = DateTime.Now;
        devolucion.UpdatedAt = DateTime.Now;

        // Generar nota de crédito automáticamente
        var notaCredito = new NotaCredito
        {
            DevolucionId = devolucion.Id,
            ClienteId = devolucion.ClienteId,
            NumeroNotaCredito = await GenerarNumeroNotaCreditoAsync(),
            FechaEmision = DateTime.Now,
            MontoTotal = devolucion.TotalDevolucion,
            Estado = EstadoNotaCredito.Vigente,
            FechaVencimiento = DateTime.Now.AddYears(1)
        };

        _context.NotasCredito.Add(notaCredito);
        devolucion.NotaCreditoGenerada = true;

        await _context.SaveChangesAsync();
        return devolucion;
    }

    public async Task<Devolucion> RechazarDevolucionAsync(int id, string motivo)
    {
        var devolucion = await ObtenerDevolucionAsync(id);
        if (devolucion == null)
        {
            throw new KeyNotFoundException($"Devolución con ID {id} no encontrada");
        }

        devolucion.Estado = EstadoDevolucion.Rechazada;
        devolucion.ObservacionesInternas = motivo;
        devolucion.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return devolucion;
    }

    public async Task<Devolucion> CompletarDevolucionAsync(int id)
    {
        var devolucion = await ObtenerDevolucionAsync(id);
        if (devolucion == null)
        {
            throw new KeyNotFoundException($"Devolución con ID {id} no encontrada");
        }

        if (devolucion.Estado != EstadoDevolucion.Aprobada)
        {
            throw new InvalidOperationException("Solo se pueden completar devoluciones aprobadas");
        }

        devolucion.Estado = EstadoDevolucion.Completada;
        devolucion.UpdatedAt = DateTime.Now;

        // Procesar stock según acción recomendada en cada detalle
        foreach (var detalle in devolucion.Detalles)
        {
            if (detalle.AccionRecomendada == AccionProducto.ReintegrarStock)
            {
                // Aumentar stock
                await _movimientoStockService.RegistrarAjusteAsync(
                    detalle.ProductoId,
                    TipoMovimiento.Entrada,
                    detalle.Cantidad,
                    $"DEV-{devolucion.NumeroDevolucion}",
                    $"Reintegro por devolución {devolucion.NumeroDevolucion}"
                );
            }
            else if (detalle.AccionRecomendada == AccionProducto.Cuarentena)
            {
                // TODO: Implementar stock en cuarentena
                // Por ahora solo registramos el movimiento
                await _movimientoStockService.RegistrarAjusteAsync(
                    detalle.ProductoId,
                    TipoMovimiento.Entrada,
                    detalle.Cantidad,
                    $"DEV-{devolucion.NumeroDevolucion}",
                    $"En cuarentena por devolución {devolucion.NumeroDevolucion}"
                );
            }
        }

        await _context.SaveChangesAsync();
        return devolucion;
    }

    public async Task<string> GenerarNumeroDevolucionAsync()
    {
        var ultimaDevolucion = await _context.Devoluciones
            .OrderByDescending(d => d.Id)
            .FirstOrDefaultAsync();

        int siguiente = (ultimaDevolucion?.Id ?? 0) + 1;
        return $"DEV-{DateTime.Now:yyyyMM}-{siguiente:D6}";
    }

    public async Task<bool> PuedeDevolverVentaAsync(int ventaId)
    {
        var diasDesdeVenta = await ObtenerDiasDesdeVentaAsync(ventaId);
        // Política: se pueden devolver productos hasta 30 días después de la compra
        return diasDesdeVenta <= 30;
    }

    public async Task<int> ObtenerDiasDesdeVentaAsync(int ventaId)
    {
        var venta = await _context.Ventas.FindAsync(ventaId);
        if (venta == null) return int.MaxValue;

        return (DateTime.Now - venta.FechaVenta).Days;
    }

    #endregion

    #region Detalles de Devolución

    public async Task<List<DevolucionDetalle>> ObtenerDetallesDevolucionAsync(int devolucionId)
    {
        return await _context.DevolucionDetalles
            .Include(dd => dd.Producto)
            .Include(dd => dd.Garantia)
            .Where(dd => dd.DevolucionId == devolucionId && !dd.IsDeleted)
            .ToListAsync();
    }

    public async Task<DevolucionDetalle> AgregarDetalleAsync(DevolucionDetalle detalle)
    {
        detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
        _context.DevolucionDetalles.Add(detalle);
        await _context.SaveChangesAsync();

        // Actualizar total de la devolución
        var devolucion = await _context.Devoluciones.FindAsync(detalle.DevolucionId);
        if (devolucion != null)
        {
            var detalles = await ObtenerDetallesDevolucionAsync(detalle.DevolucionId);
            devolucion.TotalDevolucion = detalles.Sum(d => d.Subtotal);
            await _context.SaveChangesAsync();
        }

        return detalle;
    }

    public async Task<DevolucionDetalle> ActualizarEstadoProductoAsync(int detalleId, EstadoProductoDevuelto estado, AccionProducto accion)
    {
        var detalle = await _context.DevolucionDetalles.FindAsync(detalleId);
        if (detalle == null)
        {
            throw new KeyNotFoundException($"Detalle con ID {detalleId} no encontrado");
        }

        detalle.EstadoProducto = estado;
        detalle.AccionRecomendada = accion;
        detalle.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return detalle;
    }

    public async Task<bool> VerificarAccesoriosAsync(int detalleId, bool completos, string? faltantes)
    {
        var detalle = await _context.DevolucionDetalles.FindAsync(detalleId);
        if (detalle == null) return false;

        detalle.AccesoriosCompletos = completos;
        detalle.AccesoriosFaltantes = faltantes;
        detalle.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Garantías

    public async Task<List<Garantia>> ObtenerTodasGarantiasAsync()
    {
        return await _context.Garantias
            .Include(g => g.Cliente)
            .Include(g => g.Producto)
            .Where(g => !g.IsDeleted)
            .OrderByDescending(g => g.FechaInicio)
            .ToListAsync();
    }

    public async Task<List<Garantia>> ObtenerGarantiasVigentesAsync()
    {
        var hoy = DateTime.Now;
        return await _context.Garantias
            .Include(g => g.Cliente)
            .Include(g => g.Producto)
            .Where(g => !g.IsDeleted &&
                       g.Estado == EstadoGarantia.Vigente &&
                       g.FechaVencimiento >= hoy)
            .OrderBy(g => g.FechaVencimiento)
            .ToListAsync();
    }

    public async Task<List<Garantia>> ObtenerGarantiasPorClienteAsync(int clienteId)
    {
        return await _context.Garantias
            .Include(g => g.Producto)
            .Where(g => g.ClienteId == clienteId && !g.IsDeleted)
            .OrderByDescending(g => g.FechaInicio)
            .ToListAsync();
    }

    public async Task<Garantia?> ObtenerGarantiaAsync(int id)
    {
        return await _context.Garantias
            .Include(g => g.Cliente)
            .Include(g => g.Producto)
            .Include(g => g.VentaDetalle)
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);
    }

    public async Task<Garantia?> ObtenerGarantiaPorNumeroAsync(string numeroGarantia)
    {
        return await _context.Garantias
            .Include(g => g.Cliente)
            .Include(g => g.Producto)
            .FirstOrDefaultAsync(g => g.NumeroGarantia == numeroGarantia && !g.IsDeleted);
    }

    public async Task<Garantia> CrearGarantiaAsync(Garantia garantia)
    {
        garantia.NumeroGarantia = await GenerarNumeroGarantiaAsync();
        garantia.FechaVencimiento = garantia.FechaInicio.AddMonths(garantia.MesesGarantia);
        garantia.Estado = EstadoGarantia.Vigente;

        _context.Garantias.Add(garantia);
        await _context.SaveChangesAsync();
        return garantia;
    }

    public async Task<Garantia> ActualizarGarantiaAsync(Garantia garantia)
    {
        var existente = await ObtenerGarantiaAsync(garantia.Id);
        if (existente == null)
        {
            throw new KeyNotFoundException($"Garantía con ID {garantia.Id} no encontrada");
        }

        existente.Estado = garantia.Estado;
        existente.ObservacionesActivacion = garantia.ObservacionesActivacion;
        existente.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return existente;
    }

    public async Task<bool> ValidarGarantiaVigenteAsync(int garantiaId)
    {
        var garantia = await ObtenerGarantiaAsync(garantiaId);
        if (garantia == null) return false;

        return garantia.Estado == EstadoGarantia.Vigente &&
               garantia.FechaVencimiento >= DateTime.Now;
    }

    public async Task<List<Garantia>> ObtenerGarantiasProximasVencerAsync(int dias = 30)
    {
        var hoy = DateTime.Now;
        var fechaLimite = hoy.AddDays(dias);

        return await _context.Garantias
            .Include(g => g.Cliente)
            .Include(g => g.Producto)
            .Where(g => !g.IsDeleted &&
                       g.Estado == EstadoGarantia.Vigente &&
                       g.FechaVencimiento >= hoy &&
                       g.FechaVencimiento <= fechaLimite)
            .OrderBy(g => g.FechaVencimiento)
            .ToListAsync();
    }

    public async Task<string> GenerarNumeroGarantiaAsync()
    {
        var ultimaGarantia = await _context.Garantias
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync();

        int siguiente = (ultimaGarantia?.Id ?? 0) + 1;
        return $"GAR-{DateTime.Now:yyyyMM}-{siguiente:D6}";
    }

    #endregion

    #region RMAs

    public async Task<List<RMA>> ObtenerTodosRMAsAsync()
    {
        return await _context.RMAs
            .Include(r => r.Proveedor)
            .Include(r => r.Devolucion).ThenInclude(d => d.Cliente)
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.FechaSolicitud)
            .ToListAsync();
    }

    public async Task<List<RMA>> ObtenerRMAsPorEstadoAsync(EstadoRMA estado)
    {
        return await _context.RMAs
            .Include(r => r.Proveedor)
            .Include(r => r.Devolucion)
            .Where(r => r.Estado == estado && !r.IsDeleted)
            .OrderByDescending(r => r.FechaSolicitud)
            .ToListAsync();
    }

    public async Task<List<RMA>> ObtenerRMAsPorProveedorAsync(int proveedorId)
    {
        return await _context.RMAs
            .Include(r => r.Devolucion).ThenInclude(d => d.Cliente)
            .Where(r => r.ProveedorId == proveedorId && !r.IsDeleted)
            .OrderByDescending(r => r.FechaSolicitud)
            .ToListAsync();
    }

    public async Task<RMA?> ObtenerRMAAsync(int id)
    {
        return await _context.RMAs
            .Include(r => r.Proveedor)
            .Include(r => r.Devolucion).ThenInclude(d => d.Cliente)
            .Include(r => r.Devolucion).ThenInclude(d => d.Detalles).ThenInclude(dd => dd.Producto)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public async Task<RMA?> ObtenerRMAPorNumeroAsync(string numeroRMA)
    {
        return await _context.RMAs
            .Include(r => r.Proveedor)
            .Include(r => r.Devolucion)
            .FirstOrDefaultAsync(r => r.NumeroRMA == numeroRMA && !r.IsDeleted);
    }

    public async Task<RMA> CrearRMAAsync(RMA rma)
    {
        rma.NumeroRMA = await GenerarNumeroRMAAsync();
        rma.Estado = EstadoRMA.Pendiente;

        _context.RMAs.Add(rma);
        await _context.SaveChangesAsync();

        // Marcar la devolución como que requiere RMA
        var devolucion = await _context.Devoluciones.FindAsync(rma.DevolucionId);
        if (devolucion != null)
        {
            devolucion.RequiereRMA = true;
            devolucion.RMAId = rma.Id;
            await _context.SaveChangesAsync();
        }

        return rma;
    }

    public async Task<RMA> ActualizarRMAAsync(RMA rma)
    {
        var existente = await ObtenerRMAAsync(rma.Id);
        if (existente == null)
        {
            throw new KeyNotFoundException($"RMA con ID {rma.Id} no encontrado");
        }

        existente.ObservacionesProveedor = rma.ObservacionesProveedor;
        existente.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return existente;
    }

    public async Task<RMA> AprobarRMAProveedorAsync(int rmaId, string numeroRMAProveedor)
    {
        var rma = await ObtenerRMAAsync(rmaId);
        if (rma == null)
        {
            throw new KeyNotFoundException($"RMA con ID {rmaId} no encontrado");
        }

        rma.Estado = EstadoRMA.AprobadoProveedor;
        rma.FechaAprobacion = DateTime.Now;
        rma.NumeroRMAProveedor = numeroRMAProveedor;
        rma.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return rma;
    }

    public async Task<RMA> RegistrarEnvioRMAAsync(int rmaId, string numeroGuia)
    {
        var rma = await ObtenerRMAAsync(rmaId);
        if (rma == null)
        {
            throw new KeyNotFoundException($"RMA con ID {rmaId} no encontrado");
        }

        rma.Estado = EstadoRMA.EnTransito;
        rma.FechaEnvio = DateTime.Now;
        rma.NumeroGuiaEnvio = numeroGuia;
        rma.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return rma;
    }

    public async Task<RMA> RegistrarRecepcionProveedorAsync(int rmaId)
    {
        var rma = await ObtenerRMAAsync(rmaId);
        if (rma == null)
        {
            throw new KeyNotFoundException($"RMA con ID {rmaId} no encontrado");
        }

        rma.Estado = EstadoRMA.RecibidoProveedor;
        rma.FechaRecepcionProveedor = DateTime.Now;
        rma.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return rma;
    }

    public async Task<RMA> ResolverRMAAsync(int rmaId, TipoResolucionRMA tipoResolucion, decimal? montoReembolso, string detalleResolucion)
    {
        var rma = await ObtenerRMAAsync(rmaId);
        if (rma == null)
        {
            throw new KeyNotFoundException($"RMA con ID {rmaId} no encontrado");
        }

        rma.Estado = EstadoRMA.Resuelto;
        rma.TipoResolucion = tipoResolucion;
        rma.FechaResolucion = DateTime.Now;
        rma.MontoReembolso = montoReembolso;
        rma.DetalleResolucion = detalleResolucion;
        rma.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return rma;
    }

    public async Task<string> GenerarNumeroRMAAsync()
    {
        var ultimoRMA = await _context.RMAs
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        int siguiente = (ultimoRMA?.Id ?? 0) + 1;
        return $"RMA-{DateTime.Now:yyyyMM}-{siguiente:D6}";
    }

    #endregion

    #region Notas de Crédito

    public async Task<List<NotaCredito>> ObtenerTodasNotasCreditoAsync()
    {
        return await _context.NotasCredito
            .Include(nc => nc.Cliente)
            .Include(nc => nc.Devolucion)
            .Where(nc => !nc.IsDeleted)
            .OrderByDescending(nc => nc.FechaEmision)
            .ToListAsync();
    }

    public async Task<List<NotaCredito>> ObtenerNotasCreditoPorClienteAsync(int clienteId)
    {
        return await _context.NotasCredito
            .Include(nc => nc.Devolucion)
            .Where(nc => nc.ClienteId == clienteId && !nc.IsDeleted)
            .OrderByDescending(nc => nc.FechaEmision)
            .ToListAsync();
    }

    public async Task<List<NotaCredito>> ObtenerNotasCreditoVigentesAsync(int clienteId)
    {
        var hoy = DateTime.Now;
        return await _context.NotasCredito
            .Where(nc => nc.ClienteId == clienteId &&
                        !nc.IsDeleted &&
                        nc.MontoDisponible > 0 &&
                        (nc.FechaVencimiento == null || nc.FechaVencimiento >= hoy) &&
                        nc.Estado == EstadoNotaCredito.Vigente)
            .OrderBy(nc => nc.FechaEmision)
            .ToListAsync();
    }

    public async Task<NotaCredito?> ObtenerNotaCreditoAsync(int id)
    {
        return await _context.NotasCredito
            .Include(nc => nc.Cliente)
            .Include(nc => nc.Devolucion)
            .FirstOrDefaultAsync(nc => nc.Id == id && !nc.IsDeleted);
    }

    public async Task<NotaCredito?> ObtenerNotaCreditoPorNumeroAsync(string numeroNotaCredito)
    {
        return await _context.NotasCredito
            .Include(nc => nc.Cliente)
            .FirstOrDefaultAsync(nc => nc.NumeroNotaCredito == numeroNotaCredito && !nc.IsDeleted);
    }

    public async Task<NotaCredito> CrearNotaCreditoAsync(NotaCredito notaCredito)
    {
        notaCredito.NumeroNotaCredito = await GenerarNumeroNotaCreditoAsync();
        notaCredito.Estado = EstadoNotaCredito.Vigente;
        notaCredito.MontoUtilizado = 0;

        _context.NotasCredito.Add(notaCredito);
        await _context.SaveChangesAsync();
        return notaCredito;
    }

    public async Task<NotaCredito> UtilizarNotaCreditoAsync(int notaCreditoId, decimal monto)
    {
        var notaCredito = await ObtenerNotaCreditoAsync(notaCreditoId);
        if (notaCredito == null)
        {
            throw new KeyNotFoundException($"Nota de crédito con ID {notaCreditoId} no encontrada");
        }

        if (notaCredito.MontoDisponible < monto)
        {
            throw new InvalidOperationException($"Saldo insuficiente. Disponible: ${notaCredito.MontoDisponible:N2}");
        }

        notaCredito.MontoUtilizado += monto;

        if (notaCredito.MontoDisponible == 0)
        {
            notaCredito.Estado = EstadoNotaCredito.UtilizadaTotalmente;
        }
        else
        {
            notaCredito.Estado = EstadoNotaCredito.UtilizadaParcialmente;
        }

        notaCredito.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return notaCredito;
    }

    public async Task<decimal> ObtenerCreditoDisponibleClienteAsync(int clienteId)
    {
        var notasVigentes = await ObtenerNotasCreditoVigentesAsync(clienteId);
        return notasVigentes.Sum(nc => nc.MontoDisponible);
    }

    public async Task<string> GenerarNumeroNotaCreditoAsync()
    {
        var ultimaNotaCredito = await _context.NotasCredito
            .OrderByDescending(nc => nc.Id)
            .FirstOrDefaultAsync();

        int siguiente = (ultimaNotaCredito?.Id ?? 0) + 1;
        return $"NC-{DateTime.Now:yyyyMM}-{siguiente:D6}";
    }

    #endregion

    #region Reportes y Estadísticas

    public async Task<Dictionary<MotivoDevolucion, int>> ObtenerEstadisticasMotivoDevolucionAsync(DateTime? desde = null, DateTime? hasta = null)
    {
        var query = _context.Devoluciones.Where(d => !d.IsDeleted);

        if (desde.HasValue)
            query = query.Where(d => d.FechaDevolucion >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(d => d.FechaDevolucion <= hasta.Value);

        return await query
            .GroupBy(d => d.Motivo)
            .Select(g => new { Motivo = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Motivo, x => x.Cantidad);
    }

    public async Task<List<Producto>> ObtenerProductosMasDevueltosAsync(int top = 10)
    {
        var productosIds = await _context.DevolucionDetalles
            .Where(dd => !dd.IsDeleted)
            .GroupBy(dd => dd.ProductoId)
            .OrderByDescending(g => g.Sum(dd => dd.Cantidad))
            .Take(top)
            .Select(g => g.Key)
            .ToListAsync();

        return await _context.Productos
            .Where(p => productosIds.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<decimal> ObtenerTotalDevolucionesPeriodoAsync(DateTime desde, DateTime hasta)
    {
        return await _context.Devoluciones
            .Where(d => !d.IsDeleted &&
                       d.FechaDevolucion >= desde &&
                       d.FechaDevolucion <= hasta &&
                       d.Estado == EstadoDevolucion.Completada)
            .SumAsync(d => d.TotalDevolucion);
    }

    public async Task<int> ObtenerCantidadRMAsPendientesAsync()
    {
        return await _context.RMAs
            .Where(r => !r.IsDeleted &&
                       (r.Estado == EstadoRMA.Pendiente ||
                        r.Estado == EstadoRMA.AprobadoProveedor ||
                        r.Estado == EstadoRMA.EnTransito))
            .CountAsync();
    }

    #endregion
}