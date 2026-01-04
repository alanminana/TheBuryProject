using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Tests.TestDoubles;

/// <summary>
/// Implementación vacía de IValidacionVentaService para tests.
/// Por defecto permite todas las ventas sin restricciones.
/// </summary>
internal sealed class NoopValidacionVentaService : IValidacionVentaService
{
    public Task<bool> ClientePuedeRecibirCreditoAsync(int clienteId, decimal montoSolicitado)
    {
        return Task.FromResult(true);
    }

    public Task<ResumenCrediticioClienteViewModel> ObtenerResumenCrediticioAsync(int clienteId)
    {
        return Task.FromResult(new ResumenCrediticioClienteViewModel
        {
            EstadoAptitud = "Apto",
            ColorSemaforo = "success",
            DocumentacionCompleta = true,
            CupoDisponible = 1000000m
        });
    }

    public Task<PrevalidacionResultViewModel> PrevalidarAsync(int clienteId, decimal monto)
    {
        return Task.FromResult(new PrevalidacionResultViewModel
        {
            Resultado = ResultadoPrevalidacion.Aprobable,
            LimiteCredito = 1000000m,
            CupoDisponible = 1000000m,
            ClienteId = clienteId,
            MontoSolicitado = monto,
            Timestamp = DateTime.Now
        });
    }

    public Task<ValidacionVentaResult> ValidarConfirmacionVentaAsync(int ventaId)
    {
        return Task.FromResult(new ValidacionVentaResult());
    }

    public Task<ValidacionVentaResult> ValidarVentaCreditoPersonalAsync(int clienteId, decimal montoVenta, int? creditoId = null)
    {
        return Task.FromResult(new ValidacionVentaResult());
    }
}
