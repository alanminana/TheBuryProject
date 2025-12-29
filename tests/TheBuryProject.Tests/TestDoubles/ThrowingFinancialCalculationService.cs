using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Tests.TestDoubles;

internal sealed class ThrowingFinancialCalculationService : IFinancialCalculationService
{
    public decimal CalcularCuotaSistemaFrances(decimal monto, decimal tasaMensual, int cuotas) =>
        throw new NotImplementedException();

    public decimal CalcularTotalConInteres(decimal monto, decimal tasaMensual, int cuotas) =>
        throw new NotImplementedException();

    public decimal CalcularCFTEA(decimal totalAPagar, decimal montoInicial, int cuotas) =>
        throw new NotImplementedException();

    public decimal CalcularInteresTotal(decimal monto, decimal tasaMensual, int cuotas) =>
        throw new NotImplementedException();

    public decimal ComputePmt(decimal tasaMensual, int cuotas, decimal monto) =>
        throw new NotImplementedException();

    public decimal ComputeFinancedAmount(decimal total, decimal anticipo) =>
        throw new NotImplementedException();
}
