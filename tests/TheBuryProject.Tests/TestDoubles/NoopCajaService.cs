using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Tests.TestDoubles;

/// <summary>
/// Mock de ICajaService para tests. Por defecto simula que hay una caja abierta.
/// </summary>
internal sealed class NoopCajaService : ICajaService
{
    private readonly bool _hayCajaAbierta;
    private readonly TheBuryProject.Models.Entities.AperturaCaja? _aperturaActiva;

    public NoopCajaService(bool hayCajaAbierta = true, TheBuryProject.Models.Entities.AperturaCaja? aperturaActiva = null)
    {
        _hayCajaAbierta = hayCajaAbierta;
        _aperturaActiva = aperturaActiva;
    }

    // ✅ Método clave: por defecto retorna true para permitir ventas en tests
    public Task<bool> ExisteAlgunaCajaAbiertaAsync() => Task.FromResult(_hayCajaAbierta);

    public Task<List<TheBuryProject.Models.Entities.Caja>> ObtenerTodasCajasAsync() 
        => Task.FromResult(new List<TheBuryProject.Models.Entities.Caja>());
    public Task<TheBuryProject.Models.Entities.Caja?> ObtenerCajaPorIdAsync(int id) 
        => Task.FromResult<TheBuryProject.Models.Entities.Caja?>(null);
    public Task<TheBuryProject.Models.Entities.Caja> CrearCajaAsync(CajaViewModel model) 
        => throw new NotImplementedException();
    public Task<TheBuryProject.Models.Entities.Caja> ActualizarCajaAsync(int id, CajaViewModel model) 
        => throw new NotImplementedException();
    public Task EliminarCajaAsync(int id, byte[]? rowVersion = null) => Task.CompletedTask;
    public Task<bool> ExisteCodigoCajaAsync(string codigo, int? cajaIdExcluir = null) => Task.FromResult(false);

    public Task<TheBuryProject.Models.Entities.AperturaCaja> AbrirCajaAsync(AbrirCajaViewModel model, string usuario) 
        => throw new NotImplementedException();
    public Task<TheBuryProject.Models.Entities.AperturaCaja?> ObtenerAperturaActivaAsync(int cajaId) 
        => Task.FromResult<TheBuryProject.Models.Entities.AperturaCaja?>(null);
    public Task<TheBuryProject.Models.Entities.AperturaCaja?> ObtenerAperturaPorIdAsync(int id) 
        => Task.FromResult<TheBuryProject.Models.Entities.AperturaCaja?>(null);
    public Task<List<TheBuryProject.Models.Entities.AperturaCaja>> ObtenerAperturasAbiertasAsync() 
        => Task.FromResult(new List<TheBuryProject.Models.Entities.AperturaCaja>());
    public Task<bool> TieneCajaAbiertaAsync(int cajaId) => Task.FromResult(_hayCajaAbierta);
    public Task<TheBuryProject.Models.Entities.AperturaCaja?> ObtenerAperturaActivaParaUsuarioAsync(string usuario)
        => Task.FromResult(_hayCajaAbierta ? _aperturaActiva : null);

    public Task<TheBuryProject.Models.Entities.MovimientoCaja> RegistrarMovimientoAsync(MovimientoCajaViewModel model, string usuario) 
        => throw new NotImplementedException();
    public Task<List<TheBuryProject.Models.Entities.MovimientoCaja>> ObtenerMovimientosDeAperturaAsync(int aperturaId) 
        => Task.FromResult(new List<TheBuryProject.Models.Entities.MovimientoCaja>());
    public Task<decimal> CalcularSaldoActualAsync(int aperturaId) => Task.FromResult(0m);

    // ✅ Nuevos métodos para integración venta-caja
    public Task<TheBuryProject.Models.Entities.MovimientoCaja?> RegistrarMovimientoVentaAsync(
        int ventaId, string ventaNumero, decimal monto, TipoPago tipoPago, string usuario)
        => Task.FromResult<TheBuryProject.Models.Entities.MovimientoCaja?>(null);
    
    public Task<TheBuryProject.Models.Entities.AperturaCaja?> ObtenerAperturaActivaParaVentaAsync()
        => Task.FromResult<TheBuryProject.Models.Entities.AperturaCaja?>(null);

    // ✅ Método para integración pago cuotas-caja
    public Task<TheBuryProject.Models.Entities.MovimientoCaja?> RegistrarMovimientoCuotaAsync(
        int cuotaId, string creditoNumero, int numeroCuota, decimal monto, string medioPago, string usuario)
        => Task.FromResult<TheBuryProject.Models.Entities.MovimientoCaja?>(null);

    // ✅ Método para integración anticipo crédito-caja
    public Task<TheBuryProject.Models.Entities.MovimientoCaja?> RegistrarMovimientoAnticipoAsync(
        int creditoId, string creditoNumero, decimal montoAnticipo, string usuario)
        => Task.FromResult<TheBuryProject.Models.Entities.MovimientoCaja?>(null);

    public Task<TheBuryProject.Models.Entities.CierreCaja> CerrarCajaAsync(CerrarCajaViewModel model, string usuario) 
        => throw new NotImplementedException();
    public Task<TheBuryProject.Models.Entities.CierreCaja?> ObtenerCierrePorIdAsync(int id) 
        => Task.FromResult<TheBuryProject.Models.Entities.CierreCaja?>(null);
    public Task<List<TheBuryProject.Models.Entities.CierreCaja>> ObtenerHistorialCierresAsync(int? cajaId = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null) 
        => Task.FromResult(new List<TheBuryProject.Models.Entities.CierreCaja>());

    public Task<DetallesAperturaViewModel> ObtenerDetallesAperturaAsync(int aperturaId) => throw new NotImplementedException();
    public Task<ReporteCajaViewModel> GenerarReporteCajaAsync(DateTime fechaDesde, DateTime fechaHasta, int? cajaId = null) 
        => Task.FromResult(new ReporteCajaViewModel());
    public Task<HistorialCierresViewModel> ObtenerEstadisticasCierresAsync(int? cajaId = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null) 
        => Task.FromResult(new HistorialCierresViewModel());
}
