namespace TheBuryProject.ViewModels
{
    /// <summary>
    /// Vista consolidada para mostrar una línea de crédito con los datos básicos del cliente/garante.
    /// </summary>
    public class CreditoDetalleViewModel
    {
        public CreditoViewModel Credito { get; set; } = new();

        public EvaluacionCreditoViewModel? Evaluacion { get; set; }

        public ClienteResumenViewModel Cliente => Credito.Cliente;

        public ClienteResumenViewModel? Garante => Credito.Garante;
    }
}
