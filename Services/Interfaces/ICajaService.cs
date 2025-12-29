using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services.Interfaces
{
    /// <summary>
    /// Servicio centralizado para gestin de cajas, aperturas, movimientos y cierres
    /// </summary>
    public interface ICajaService
    {
        #region CRUD de Cajas

        Task<List<Caja>> ObtenerTodasCajasAsync();
        Task<Caja?> ObtenerCajaPorIdAsync(int id);
        Task<Caja> CrearCajaAsync(CajaViewModel model);
        Task<Caja> ActualizarCajaAsync(int id, CajaViewModel model);
        Task EliminarCajaAsync(int id, byte[]? rowVersion = null);
        Task<bool> ExisteCodigoCajaAsync(string codigo, int? cajaIdExcluir = null);

        #endregion

        #region Apertura de Caja

        Task<AperturaCaja> AbrirCajaAsync(AbrirCajaViewModel model, string usuario);
        Task<AperturaCaja?> ObtenerAperturaActivaAsync(int cajaId);
        Task<AperturaCaja?> ObtenerAperturaPorIdAsync(int id);
        Task<List<AperturaCaja>> ObtenerAperturasAbiertasAsync();
        Task<bool> TieneCajaAbiertaAsync(int cajaId);

        #endregion

        #region Movimientos de Caja

        Task<MovimientoCaja> RegistrarMovimientoAsync(MovimientoCajaViewModel model, string usuario);
        Task<List<MovimientoCaja>> ObtenerMovimientosDeAperturaAsync(int aperturaId);
        Task<decimal> CalcularSaldoActualAsync(int aperturaId);

        #endregion

        #region Cierre de Caja

        Task<CierreCaja> CerrarCajaAsync(CerrarCajaViewModel model, string usuario);
        Task<CierreCaja?> ObtenerCierrePorIdAsync(int id);
        Task<List<CierreCaja>> ObtenerHistorialCierresAsync(
            int? cajaId = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null);

        #endregion

        #region Reportes y Estadsticas

        Task<DetallesAperturaViewModel> ObtenerDetallesAperturaAsync(int aperturaId);
        Task<ReporteCajaViewModel> GenerarReporteCajaAsync(
            DateTime fechaDesde,
            DateTime fechaHasta,
            int? cajaId = null);
        Task<HistorialCierresViewModel> ObtenerEstadisticasCierresAsync(
            int? cajaId = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null);

        #endregion
    }
}