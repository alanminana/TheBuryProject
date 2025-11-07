namespace TheBuryProject.Models.Enums
{
    /// <summary>
    /// Estados posibles de un crédito
    /// </summary>
    public enum EstadoCredito
    {
        /// <summary>
        /// Crédito solicitado, pendiente de evaluación
        /// </summary>
        Solicitado = 0,

        /// <summary>
        /// Crédito aprobado, pendiente de desembolso
        /// </summary>
        Aprobado = 1,

        /// <summary>
        /// Crédito activo con cuotas en pago
        /// </summary>
        Activo = 2,

        /// <summary>
        /// Crédito finalizado, todas las cuotas pagadas
        /// </summary>
        Finalizado = 3,

        /// <summary>
        /// Crédito rechazado
        /// </summary>
        Rechazado = 4,

        /// <summary>
        /// Crédito cancelado
        /// </summary>
        Cancelado = 5
    }
}