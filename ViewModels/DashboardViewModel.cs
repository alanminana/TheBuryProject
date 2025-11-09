using TheBuryProject.Models.DTOs;

namespace TheBuryProject.ViewModels
{
    public class DashboardViewModel
    {
        // KPIs Generales
        public int TotalClientes { get; set; }
        public int ClientesActivos { get; set; }
        public decimal VentasTotalesHoy { get; set; }
        public decimal VentasTotalesMes { get; set; }
        public decimal VentasTotalesAnio { get; set; }
        public decimal TicketPromedio { get; set; }

        // KPIs de Créditos
        public int CreditosActivos { get; set; }
        public decimal MontoTotalCreditos { get; set; }
        public decimal SaldoPendienteTotal { get; set; }
        public int CuotasVencidasTotal { get; set; }
        public decimal MontoVencidoTotal { get; set; }

        // KPIs de Cobranza
        public decimal CobranzaHoy { get; set; }
        public decimal CobranzaMes { get; set; }
        public decimal CobranzaAnio { get; set; }
        public decimal TasaMorosidad { get; set; }
        public decimal EfectividadCobranza { get; set; }

        // KPIs de Stock
        public int ProductosTotales { get; set; }
        public int ProductosBajoStock { get; set; }
        public decimal ValorTotalStock { get; set; }

        // Datos para gráficos
        public List<VentasPorDiaDto> VentasUltimos7Dias { get; set; } = new();
        public List<VentasPorMesDto> VentasUltimos12Meses { get; set; } = new();
        public List<ProductoMasVendidoDto> ProductosMasVendidos { get; set; } = new();
        public List<EstadoCreditoDto> CreditosPorEstado { get; set; } = new();
        public List<CobranzaPorMesDto> CobranzaUltimos6Meses { get; set; } = new();
    }
}