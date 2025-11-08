namespace TheBuryProject.Models.Enums
{
    /// <summary>
    /// Tipos de alertas de cobranza
    /// </summary>
    public enum TipoAlerta
    {
        ProximoVencimiento = 1,  // Cuota próxima a vencer
        Vencido = 2,             // Cuota vencida
        MoraLeve = 3,            // 1-15 días de mora
        MoraModerada = 4,        // 16-30 días de mora
        MoraGrave = 5,           // 31-60 días de mora
        MoraCritica = 6,         // Más de 60 días de mora
        CreditoEnRiesgo = 7      // Múltiples cuotas vencidas
    }
}