using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Tests.TestDoubles;

internal sealed class NoopConfiguracionPagoService : IConfiguracionPagoService
{
    public Task<List<ConfiguracionPagoViewModel>> GetAllAsync() => Task.FromResult(new List<ConfiguracionPagoViewModel>());
    public Task<ConfiguracionPagoViewModel?> GetByIdAsync(int id) => Task.FromResult<ConfiguracionPagoViewModel?>(null);
    public Task<ConfiguracionPagoViewModel?> GetByTipoPagoAsync(TipoPago tipoPago) => Task.FromResult<ConfiguracionPagoViewModel?>(null);
    public Task<decimal> ObtenerTasaInteresMensualCreditoPersonalAsync() => Task.FromResult(0m);
    public Task<ConfiguracionPagoViewModel> CreateAsync(ConfiguracionPagoViewModel viewModel) => Task.FromResult(viewModel);
    public Task<ConfiguracionPagoViewModel?> UpdateAsync(int id, ConfiguracionPagoViewModel viewModel) => Task.FromResult<ConfiguracionPagoViewModel?>(viewModel);
    public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    public Task<List<ConfiguracionTarjetaViewModel>> GetTarjetasActivasAsync() => Task.FromResult(new List<ConfiguracionTarjetaViewModel>());
    public Task<ConfiguracionTarjetaViewModel?> GetTarjetaByIdAsync(int id) => Task.FromResult<ConfiguracionTarjetaViewModel?>(null);
    public Task<bool> ValidarDescuento(TipoPago tipoPago, decimal descuento) => Task.FromResult(true);
    public Task<decimal> CalcularRecargo(TipoPago tipoPago, decimal monto) => Task.FromResult(0m);
}
