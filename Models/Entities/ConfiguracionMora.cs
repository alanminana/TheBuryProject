using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Configuración del sistema de mora y alertas de cobranza
    /// </summary>
    public class ConfiguracionMora : BaseEntity
    {
        public int DiasGracia { get; set; } = 3;
        public decimal PorcentajeRecargo { get; set; } = 5.0m;
        public bool CalculoAutomatico { get; set; } = true;
        public bool NotificacionAutomatica { get; set; } = true;
        public bool JobActivo { get; set; } = true;
        public TimeSpan HoraEjecucion { get; set; } = new TimeSpan(8, 0, 0);
        public DateTime? UltimaEjecucion { get; set; }
    }
}
