namespace TheBuryProject.ViewModels
{
    public class ConfiguracionMoraViewModel
    {
        public int Id { get; set; }
        public int DiasGracia { get; set; }
        public decimal PorcentajeRecargo { get; set; }
        public bool CalculoAutomatico { get; set; }
        public bool NotificacionAutomatica { get; set; }
        public bool JobActivo { get; set; }
        public TimeSpan HoraEjecucion { get; set; }
        public DateTime? UltimaEjecucion { get; set; }
    }
}