namespace TheBuryProject.Models.Enums
{
    public enum TipoAlertaCobranza
    {
        CuotaVencida = 1,
        ProximoVencimiento = 2,
        MoraElevada = 3,
        ClienteRiesgo = 4
    }

    public enum TipoAlertaStock
    {
        StockBajo = 1,
        StockCritico = 2,
        StockAgotado = 3,
        ProductoSinMovimiento = 4
    }

    public enum TipoAlertaGeneral
    {
        Sistema = 1,
        Operacional = 2,
        Financiera = 3,
        Comercial = 4
    }

    public enum PrioridadAlerta
    {
        Baja = 1,
        Media = 2,
        Alta = 3,
        Critica = 4
    }

    public enum EstadoAlerta
    {
        Pendiente = 1,
        EnProceso = 2,
        Resuelta = 3,
        Ignorada = 4
    }
}