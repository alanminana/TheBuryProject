namespace TheBuryProject.Models.Enums
{
    public enum TipoPago
    {
        Efectivo = 0,
        Transferencia = 1,
        TarjetaDebito = 2,
        TarjetaCredito = 3,
        Cheque = 4,
        CreditoPersonall = 5,  // CAMBIADO: antes era "Credito"
        MercadoPago = 6,
        CuentaCorriente = 7,
        Tarjeta = 8
    }
}
