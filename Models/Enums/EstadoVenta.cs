namespace TheBuryProject.Models.Enums
{
    public enum EstadoVenta
    {
        Cotizacion = 0,      // NUEVO: cotización sin compromiso
        Presupuesto = 1,     // Presupuesto (antes era 0)
        Confirmada = 2,      // Antes era 1
        Facturada = 3,       // Antes era 2
        Entregada = 4,       // Antes era 3
        Cancelada = 5        // Antes era 4
    }
}