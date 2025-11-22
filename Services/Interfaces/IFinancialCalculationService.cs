namespace TheBuryProject.Services
{
    public interface IFinancialCalculationService
    {
        decimal CalcularCuotaSistemaFrances(decimal monto, decimal tasaMensual, int cuotas);
        decimal CalcularTotalConInteres(decimal monto, decimal tasaMensual, int cuotas);
        decimal CalcularCFTEA(decimal totalAPagar, decimal montoInicial, int cuotas);
        decimal CalcularInteresTotal(decimal monto, decimal tasaMensual, int cuotas);
    }
}