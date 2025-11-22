using TheBuryProject.ViewModels;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Helpers
{
    /// <summary>
    /// Contiene métodos para cálculo de score crediticio
    /// </summary>
    public static class CreditoScoringHelper
    {
        private const int SCORE_BASE = 500;
        private const int SCORE_MIN = 300;
        private const int SCORE_MAX = 850;
        private const int PUNTOS_POR_DOCUMENTO = 50;
        private const int PUNTOS_ANTIGÜEDAD_AÑO = 100;
        private const int PUNTOS_ANTIGÜEDAD_MES = 50;
        private const decimal FACTOR_ENDEUDAMIENTO = 5m;

        /// <summary>
        /// Calcula un score crediticio simplificado
        /// </summary>
        public static int CalcularScoreCrediticio(ClienteDetalleViewModel modelo)
        {
            int score = SCORE_BASE;

            // Puntos por documentación verificada
            score += AñadirPuntosDocumentacion(modelo);

            // Penalización por endeudamiento
            score -= ObtenerPenalizacionEndeudamiento(modelo);

            // Bonificación por antigüedad laboral
            score += ObtenerBonificacionAntigüedad(modelo);

            return Math.Max(SCORE_MIN, Math.Min(SCORE_MAX, score));
        }

        /// <summary>
        /// Calcula puntos adicionales por documentación verificada
        /// </summary>
        private static int AñadirPuntosDocumentacion(ClienteDetalleViewModel modelo)
        {
            return modelo.Documentos.Count(d => d.Estado == EstadoDocumento.Verificado) * PUNTOS_POR_DOCUMENTO;
        }

        /// <summary>
        /// Obtiene la penalización por endeudamiento alto
        /// </summary>
        private static int ObtenerPenalizacionEndeudamiento(ClienteDetalleViewModel modelo)
        {
            if (!modelo.CreditosActivos.Any() || modelo.Cliente.IngresoMensual == null || modelo.Cliente.IngresoMensual <= 0)
                return 0;

            var cuotaMensualActual = modelo.CreditosActivos.Sum(c => c.MontoTotal / c.CantidadCuotas);
            var endeudamiento = (cuotaMensualActual / modelo.Cliente.IngresoMensual.Value) * 100;

            if (endeudamiento > 40)
                return (int)((endeudamiento - 40) * FACTOR_ENDEUDAMIENTO);

            return 0;
        }

        /// <summary>
        /// Obtiene bonificación por antigüedad laboral
        /// </summary>
        private static int ObtenerBonificacionAntigüedad(ClienteDetalleViewModel modelo)
        {
            if (string.IsNullOrEmpty(modelo.Cliente.TiempoTrabajo))
                return 0;

            if (modelo.Cliente.TiempoTrabajo.Contains("año"))
                return PUNTOS_ANTIGÜEDAD_AÑO;

            if (modelo.Cliente.TiempoTrabajo.Contains("mes"))
                return PUNTOS_ANTIGÜEDAD_MES;

            return 0;
        }
    }
}