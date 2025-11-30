using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{

    public class FinancialCalculationService : IFinancialCalculationService
    {
        public decimal CalcularCuotaSistemaFrances(decimal monto, decimal tasaMensual, int cuotas)
        {
            if (monto <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero", nameof(monto));

            if (cuotas <= 0)
                throw new ArgumentException("La cantidad de cuotas debe ser mayor a cero", nameof(cuotas));

            if (tasaMensual == 0)
                return monto / cuotas;

            var factor = (decimal)Math.Pow((double)(1 + tasaMensual), cuotas);
            return monto * (tasaMensual * factor) / (factor - 1);
        }

        public decimal CalcularTotalConInteres(decimal monto, decimal tasaMensual, int cuotas)
        {
            if (tasaMensual == 0)
                return monto;

            var cuotaMensual = CalcularCuotaSistemaFrances(monto, tasaMensual, cuotas);
            return cuotaMensual * cuotas;
        }

        public decimal CalcularCFTEA(decimal totalAPagar, decimal montoInicial, int cuotas)
        {
            if (cuotas <= 0 || montoInicial <= 0)
                return 0;

            var baseCFTEA = (double)(totalAPagar / montoInicial);
            var expCFTEA = 12.0 / cuotas;
            return (decimal)(Math.Pow(baseCFTEA, expCFTEA) - 1) * 100;
        }

        public decimal CalcularInteresTotal(decimal monto, decimal tasaMensual, int cuotas)
        {
            var totalConInteres = CalcularTotalConInteres(monto, tasaMensual, cuotas);
            return totalConInteres - monto;
        }
    }
}