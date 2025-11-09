namespace TheBuryProject.Models.Enums
{
    public enum TipoAlertaCobranza
    {
        CuotaVencida = 1,
        ProximoVencimiento = 2,
        MoraElevada = 3,
        ClienteRiesgo = 4
    }

    public enum PrioridadAlerta
    {
        Baja = 1,
        Media = 2,
        Alta = 3,
        Critica = 4
    }
}