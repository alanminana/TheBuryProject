using System;
using System.Linq;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioAnio = new DateTime(hoy.Year, 1, 1);

            var dashboard = new DashboardViewModel
            {
                TotalClientes = await _context.Clientes.CountAsync(c => !c.IsDeleted),
                ClientesActivos = await _context.Clientes.CountAsync(c => !c.IsDeleted && c.Activo),
                ClientesNuevosEsteMes = await _context.Clientes.CountAsync(c => !c.IsDeleted && c.CreatedAt >= inicioMes),

                VentasTotalesHoy = await _context.Ventas.Where(v => !v.IsDeleted && v.FechaVenta.Date == hoy).SumAsync(v => (decimal?)v.Total) ?? 0,
                VentasTotalesMes = await _context.Ventas.Where(v => !v.IsDeleted && v.FechaVenta >= inicioMes).SumAsync(v => (decimal?)v.Total) ?? 0,
                VentasTotalesAnio = await _context.Ventas.Where(v => !v.IsDeleted && v.FechaVenta >= inicioAnio).SumAsync(v => (decimal?)v.Total) ?? 0,
                CantidadVentasHoy = await _context.Ventas.CountAsync(v => !v.IsDeleted && v.FechaVenta.Date == hoy),
                CantidadVentasMes = await _context.Ventas.CountAsync(v => !v.IsDeleted && v.FechaVenta >= inicioMes),

                CreditosActivos = await _context.Creditos.CountAsync(c => !c.IsDeleted && c.Estado == EstadoCredito.Activo),
                MontoTotalCreditos = await _context.Creditos.Where(c => !c.IsDeleted && c.Estado == EstadoCredito.Activo).SumAsync(c => (decimal?)c.TotalAPagar) ?? 0,
                SaldoPendienteCobro = await _context.Cuotas.Where(c => !c.IsDeleted && c.Estado != EstadoCuota.Pagada && c.Estado != EstadoCuota.Cancelada).SumAsync(c => (decimal?)(c.MontoTotal - c.MontoPagado)) ?? 0,
                CuotasVencidasHoy = await _context.Cuotas.CountAsync(c => !c.IsDeleted && c.Estado == EstadoCuota.Vencida && c.FechaVencimiento.Date == hoy),
                CuotasProximasVencer = await _context.Cuotas.CountAsync(c => !c.IsDeleted && c.Estado == EstadoCuota.Pendiente && c.FechaVencimiento > hoy && c.FechaVencimiento <= hoy.AddDays(7)),
                MontoMoraTotal = await _context.Cuotas.Where(c => !c.IsDeleted && c.Estado == EstadoCuota.Vencida).SumAsync(c => (decimal?)c.MontoPunitorio) ?? 0,

                CobradoHoy = await _context.Cuotas.Where(c => !c.IsDeleted && c.FechaPago.HasValue && c.FechaPago.Value.Date == hoy).SumAsync(c => (decimal?)c.MontoPagado) ?? 0,
                CobradoMes = await _context.Cuotas.Where(c => !c.IsDeleted && c.FechaPago.HasValue && c.FechaPago.Value >= inicioMes).SumAsync(c => (decimal?)c.MontoPagado) ?? 0,
                CuotasCobradas = await _context.Cuotas.CountAsync(c => !c.IsDeleted && c.Estado == EstadoCuota.Pagada),

                ProductosTotales = await _context.Productos.CountAsync(p => !p.IsDeleted),
                ProductosBajoStock = await _context.Productos.CountAsync(p => !p.IsDeleted && p.StockActual <= p.StockMinimo && p.StockActual > 0),
                ProductosSinStock = await _context.Productos.CountAsync(p => !p.IsDeleted && p.StockActual == 0),
                ValorInventario = await _context.Productos.Where(p => !p.IsDeleted).SumAsync(p => (decimal?)(p.PrecioCompra * p.StockActual)) ?? 0,

                VentasUltimos7Dias = await GetVentasUltimos7DiasAsync(),
                VentasUltimos12Meses = await GetVentasUltimos12MesesAsync(),
                ProductosMasVendidos = await GetProductosMasVendidosAsync(5),
                CreditosPorEstado = await GetCreditosPorEstadoAsync(),
                CobranzaUltimos30Dias = await GetCobranzaUltimos30DiasAsync(),
                AlertasPendientes = await GetAlertasPendientesAsync()
            };

            dashboard.TicketPromedio = dashboard.CantidadVentasMes > 0 ? dashboard.VentasTotalesMes / dashboard.CantidadVentasMes : 0;
            dashboard.TasaMorosidad = await CalcularTasaMorosidadAsync();
            dashboard.EfectividadCobranza = await CalcularEfectividadCobranzaAsync();
            dashboard.TotalAlertas = dashboard.AlertasPendientes.Count;

            return dashboard;
        }

        public async Task<List<VentasPorDiaDto>> GetVentasUltimos7DiasAsync()
        {
            var fecha7DiasAtras = DateTime.Today.AddDays(-6);
            var ventas = await _context.Ventas.Where(v => !v.IsDeleted && v.FechaVenta >= fecha7DiasAtras).GroupBy(v => v.FechaVenta.Date).Select(g => new VentasPorDiaDto { Fecha = g.Key, MontoTotal = g.Sum(v => v.Total), CantidadVentas = g.Count() }).ToListAsync();
            var resultado = new List<VentasPorDiaDto>();
            for (int i = 0; i < 7; i++)
            {
                var fecha = DateTime.Today.AddDays(-6 + i);
                var venta = ventas.FirstOrDefault(v => v.Fecha == fecha);
                resultado.Add(venta ?? new VentasPorDiaDto { Fecha = fecha, MontoTotal = 0, CantidadVentas = 0 });
            }
            return resultado;
        }

        public async Task<List<VentasPorMesDto>> GetVentasUltimos12MesesAsync()
        {
            var fecha12MesesAtras = DateTime.Today.AddMonths(-11);
            var inicioMes = new DateTime(fecha12MesesAtras.Year, fecha12MesesAtras.Month, 1);
            var ventas = await _context.Ventas.Where(v => !v.IsDeleted && v.FechaVenta >= inicioMes).GroupBy(v => new { v.FechaVenta.Year, v.FechaVenta.Month }).Select(g => new VentasPorMesDto { Anio = g.Key.Year, Mes = g.Key.Month, MontoTotal = g.Sum(v => v.Total), CantidadVentas = g.Count() }).ToListAsync();
            var resultado = new List<VentasPorMesDto>();
            for (int i = 0; i < 12; i++)
            {
                var fecha = DateTime.Today.AddMonths(-11 + i);
                var venta = ventas.FirstOrDefault(v => v.Anio == fecha.Year && v.Mes == fecha.Month);
                if (venta == null) venta = new VentasPorMesDto { Anio = fecha.Year, Mes = fecha.Month, MontoTotal = 0, CantidadVentas = 0 };
                venta.MesNombre = new DateTime(venta.Anio, venta.Mes, 1).ToString("MMM yyyy", new CultureInfo("es-AR"));
                resultado.Add(venta);
            }
            return resultado;
        }

        public async Task<List<ProductoMasVendidoDto>> GetProductosMasVendidosAsync(int top = 10)
        {
            var fecha30DiasAtras = DateTime.Today.AddDays(-30);
            return await _context.VentaDetalles.Include(d => d.Producto).Where(d => !d.IsDeleted && d.Venta.FechaVenta >= fecha30DiasAtras).GroupBy(d => new { d.ProductoId, d.Producto.Nombre }).Select(g => new ProductoMasVendidoDto { ProductoId = g.Key.ProductoId, ProductoNombre = g.Key.Nombre, CantidadVendida = (int)g.Sum(d => d.Cantidad), MontoTotal = g.Sum(d => d.Subtotal) }).OrderByDescending(p => p.CantidadVendida).Take(top).ToListAsync();
        }

        public async Task<List<CreditoPorEstadoDto>> GetCreditosPorEstadoAsync()
        {
            return await _context.Creditos.Where(c => !c.IsDeleted).GroupBy(c => c.Estado).Select(g => new CreditoPorEstadoDto { Estado = g.Key.ToString(), Cantidad = g.Count(), MontoTotal = g.Sum(c => c.TotalAPagar) }).ToListAsync();
        }

        public async Task<List<CobranzaPorDiaDto>> GetCobranzaUltimos30DiasAsync()
        {
            var fecha30DiasAtras = DateTime.Today.AddDays(-29);
            var cobranzas = await _context.Cuotas.Where(c => !c.IsDeleted && c.FechaPago.HasValue && c.FechaPago.Value >= fecha30DiasAtras).GroupBy(c => c.FechaPago.Value.Date).Select(g => new CobranzaPorDiaDto { Fecha = g.Key, MontoCobrado = g.Sum(c => c.MontoPagado), CuotasCobradas = g.Count() }).ToListAsync();
            var resultado = new List<CobranzaPorDiaDto>();
            for (int i = 0; i < 30; i++)
            {
                var fecha = DateTime.Today.AddDays(-29 + i);
                var cobranza = cobranzas.FirstOrDefault(c => c.Fecha == fecha);
                resultado.Add(cobranza ?? new CobranzaPorDiaDto { Fecha = fecha, MontoCobrado = 0, CuotasCobradas = 0 });
            }
            return resultado;
        }

        public async Task<List<AlertaDto>> GetAlertasPendientesAsync()
        {
            var alertas = new List<AlertaDto>();
            var hoy = DateTime.Today;
            var productosStockBajo = await _context.Productos.Where(p => !p.IsDeleted && p.StockActual <= p.StockMinimo && p.StockActual > 0).CountAsync();
            if (productosStockBajo > 0) alertas.Add(new AlertaDto { Tipo = "Stock Bajo", Mensaje = $"{productosStockBajo} producto(s) con stock bajo", Prioridad = "Media", Fecha = hoy, Icono = "bi-box-seam", Color = "warning", Url = "/Productos/Index" });
            var productosSinStock = await _context.Productos.Where(p => !p.IsDeleted && p.StockActual == 0).CountAsync();
            if (productosSinStock > 0) alertas.Add(new AlertaDto { Tipo = "Sin Stock", Mensaje = $"{productosSinStock} producto(s) sin stock", Prioridad = "Alta", Fecha = hoy, Icono = "bi-exclamation-triangle-fill", Color = "danger", Url = "/Productos/Index" });
            var cuotasVencidas = await _context.Cuotas.CountAsync(c => !c.IsDeleted && c.Estado == EstadoCuota.Vencida);
            if (cuotasVencidas > 0) alertas.Add(new AlertaDto { Tipo = "Cuotas Vencidas", Mensaje = $"{cuotasVencidas} cuota(s) vencida(s)", Prioridad = "Alta", Fecha = hoy, Icono = "bi-calendar-x", Color = "danger", Url = "/Creditos/Index" });
            var cuotasProximasVencer = await _context.Cuotas.CountAsync(c => !c.IsDeleted && c.Estado == EstadoCuota.Pendiente && c.FechaVencimiento > hoy && c.FechaVencimiento <= hoy.AddDays(7));
            if (cuotasProximasVencer > 0) alertas.Add(new AlertaDto { Tipo = "Próximos Vencimientos", Mensaje = $"{cuotasProximasVencer} cuota(s) vencen en los próximos 7 días", Prioridad = "Media", Fecha = hoy, Icono = "bi-clock", Color = "warning", Url = "/Creditos/Index" });
            if (_context.Model.FindEntityType(typeof(TheBuryProject.Models.Entities.AlertaCobranza)) != null)
            {
                try
                {
                    var alertasMora = await _context.AlertasCobranza.CountAsync(a => !a.IsDeleted && !a.Resuelta && a.Prioridad == 3);
                    if (alertasMora > 0) alertas.Add(new AlertaDto { Tipo = "Alertas Críticas de Mora", Mensaje = $"{alertasMora} alerta(s) crítica(s) de cobranza", Prioridad = "Alta", Fecha = hoy, Icono = "bi-exclamation-octagon-fill", Color = "danger", Url = "/Mora/Alertas" });
                }
                catch { }
            }
            return alertas.OrderByDescending(a => a.Prioridad == "Alta" ? 3 : a.Prioridad == "Media" ? 2 : 1).ToList();
        }

        public async Task<decimal> CalcularTasaMorosidadAsync()
        {
            var totalCuotas = await _context.Cuotas.CountAsync(c => !c.IsDeleted && c.Estado != EstadoCuota.Cancelada);
            if (totalCuotas == 0) return 0;
            var cuotasVencidas = await _context.Cuotas.CountAsync(c => !c.IsDeleted && c.Estado == EstadoCuota.Vencida);
            return Math.Round((decimal)cuotasVencidas / totalCuotas * 100, 2);
        }

        public async Task<decimal> CalcularEfectividadCobranzaAsync()
        {
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var cuotasVencidasMes = await _context.Cuotas.Where(c => !c.IsDeleted && c.FechaVencimiento >= inicioMes && c.FechaVencimiento < DateTime.Today).CountAsync();
            if (cuotasVencidasMes == 0) return 100;
            var cuotasCobradas = await _context.Cuotas.Where(c => !c.IsDeleted && c.FechaVencimiento >= inicioMes && c.FechaVencimiento < DateTime.Today && c.Estado == EstadoCuota.Pagada).CountAsync();
            return Math.Round((decimal)cuotasCobradas / cuotasVencidasMes * 100, 2);
        }
    }
}