namespace TheBuryProject.ViewModels
{
    public class DashboardViewModel
    {
        // KPIs Generales
        public int TotalClientes { get; set; }
        public int ClientesActivos { get; set; }
        public int ClientesNuevosEsteMes { get; set; }

        // KPIs de Ventas
        public decimal VentasTotalesHoy { get; set; }
        public decimal VentasTotalesMes { get; set; }
        public decimal VentasTotalesAnio { get; set; }
        public int CantidadVentasHoy { get; set; }
        public int CantidadVentasMes { get; set; }
        public decimal TicketPromedio { get; set; }

        // KPIs de Créditos
        public int CreditosActivos { get; set; }
        public decimal MontoTotalCreditos { get; set; }
        public decimal SaldoPendienteCobro { get; set; }
        public int CuotasVencidasHoy { get; set; }
        public int CuotasProximasVencer { get; set; }
        public decimal TasaMorosidad { get; set; }
        public decimal MontoMoraTotal { get; set; }

        // KPIs de Cobranza
        public decimal CobradoHoy { get; set; }
        public decimal CobradoMes { get; set; }
        public int CuotasCobradas { get; set; }
        public decimal EfectividadCobranza { get; set; }

        // KPIs de Stock
        public int ProductosTotales { get; set; }
        public int ProductosBajoStock { get; set; }
        public int ProductosSinStock { get; set; }
        public decimal ValorInventario { get; set; }

        // Datos para gráficos
        public List<VentasPorDiaDto> VentasUltimos7Dias { get; set; } = new();
        public List<VentasPorMesDto> VentasUltimos12Meses { get; set; } = new();
        public List<ProductoMasVendidoDto> ProductosMasVendidos { get; set; } = new();
        public List<CreditoPorEstadoDto> CreditosPorEstado { get; set; } = new();
        public List<CobranzaPorDiaDto> CobranzaUltimos30Dias { get; set; } = new();

        // Alertas y Notificaciones
        public List<AlertaDto> AlertasPendientes { get; set; } = new();
        public int TotalAlertas { get; set; }
    }

    public class VentasPorDiaDto
    {
        public DateTime Fecha { get; set; }
        public decimal MontoTotal { get; set; }
        public int CantidadVentas { get; set; }
    }

    public class VentasPorMesDto
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string MesNombre { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public int CantidadVentas { get; set; }
    }

    public class ProductoMasVendidoDto
    {
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
        public decimal MontoTotal { get; set; }
    }

    public class CreditoPorEstadoDto
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal MontoTotal { get; set; }
    }

    public class CobranzaPorDiaDto
    {
        public DateTime Fecha { get; set; }
        public decimal MontoCobrado { get; set; }
        public int CuotasCobradas { get; set; }
    }

    public class AlertaDto
    {
        public string Tipo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Icono { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? Url { get; set; }
    }
}