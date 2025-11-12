using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    public interface ICajaService
    {
        // CRUD de Cajas
        Task<List<Caja>> ObtenerTodasCajasAsync();
        Task<Caja?> ObtenerCajaPorIdAsync(int id);
        Task<Caja> CrearCajaAsync(CajaViewModel model);
        Task<Caja> ActualizarCajaAsync(int id, CajaViewModel model);
        Task EliminarCajaAsync(int id);
        Task<bool> ExisteCodigoCajaAsync(string codigo, int? cajaIdExcluir = null);

        // Apertura de Caja
        Task<AperturaCaja> AbrirCajaAsync(AbrirCajaViewModel model, string usuario);
        Task<AperturaCaja?> ObtenerAperturaActivaAsync(int cajaId);
        Task<AperturaCaja?> ObtenerAperturaPorIdAsync(int id);
        Task<List<AperturaCaja>> ObtenerAperturasAbiertasAsync();
        Task<bool> TieneCajaAbiertaAsync(int cajaId);

        // Movimientos de Caja
        Task<MovimientoCaja> RegistrarMovimientoAsync(MovimientoCajaViewModel model, string usuario);
        Task<List<MovimientoCaja>> ObtenerMovimientosDeAperturaAsync(int aperturaId);
        Task<decimal> CalcularSaldoActualAsync(int aperturaId);

        // Cierre de Caja
        Task<CierreCaja> CerrarCajaAsync(CerrarCajaViewModel model, string usuario);
        Task<CierreCaja?> ObtenerCierrePorIdAsync(int id);
        Task<List<CierreCaja>> ObtenerHistorialCierresAsync(int? cajaId = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null);

        // Reportes y Estadísticas
        Task<DetallesAperturaViewModel> ObtenerDetallesAperturaAsync(int aperturaId);
        Task<ReporteCajaViewModel> GenerarReporteCajaAsync(DateTime fechaDesde, DateTime fechaHasta, int? cajaId = null);
        Task<HistorialCierresViewModel> ObtenerEstadisticasCierresAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null);
    }
}