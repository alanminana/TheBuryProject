namespace TheBuryProject.ViewModels.Requests
{
    public class CalcularCreditoPreviewRequest
    {
        public decimal MontoSolicitado { get; set; }

        public decimal TasaInteres { get; set; }

        public int CantidadCuotas { get; set; }

        public decimal? CapacidadPagoMensual { get; set; }
    }
}
