using TheBuryProject.Models.Enums;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Helpers
{
    /// <summary>
    /// Contiene métodos auxiliares para el ClienteController
    /// </summary>
    public static class ClienteControllerHelper
    {
        /// <summary>
        /// Estructura para retornar cálculos de crédito
        /// </summary>
        public class CreditoCalculos
        {
            public decimal TasaMensualDecimal { get; set; }
            public decimal CuotaMensual { get; set; }
            public decimal TotalAPagar { get; set; }
            public decimal CFTEA { get; set; }
        }

        /// <summary>
        /// Calcula los parámetros financieros del crédito
        /// </summary>
        public static CreditoCalculos CalcularParametrosCredito(decimal montoSolicitado, decimal tasaInteres, int cantidadCuotas)
        {
            decimal tasaMensualDecimal = tasaInteres / 100;
            decimal cuotaMensual = tasaMensualDecimal > 0
                ? CalcularCuotaSistemaFrances(montoSolicitado, tasaMensualDecimal, cantidadCuotas)
                : montoSolicitado / cantidadCuotas;

            decimal totalAPagar = cuotaMensual * cantidadCuotas;
            decimal cftea = CalcularCFTEA(totalAPagar, montoSolicitado, cantidadCuotas);

            return new CreditoCalculos
            {
                TasaMensualDecimal = tasaMensualDecimal,
                CuotaMensual = cuotaMensual,
                TotalAPagar = totalAPagar,
                CFTEA = cftea
            };
        }

        /// <summary>
        /// Calcula la cuota usando el sistema francés
        /// </summary>
        public static decimal CalcularCuotaSistemaFrances(decimal monto, decimal tasaMensual, int cuotas)
        {
            var factor = (decimal)Math.Pow((double)(1 + tasaMensual), cuotas);
            return monto * (tasaMensual * factor) / (factor - 1);
        }

        /// <summary>
        /// Calcula el CFTEA (Costo Financiero Total Efectivo Anual)
        /// </summary>
        public static decimal CalcularCFTEA(decimal totalAPagar, decimal montoSolicitado, int cantidadCuotas)
        {
            if (cantidadCuotas <= 0 || montoSolicitado <= 0)
                return 0;

            var baseCFTEA = (double)(totalAPagar / montoSolicitado);
            var expCFTEA = 12.0 / cantidadCuotas;
            return (decimal)(Math.Pow(baseCFTEA, expCFTEA) - 1) * 100;
        }

        /// <summary>
        /// Verifica si se tienen todos los documentos requeridos
        /// </summary>
        public static bool VerificaDocumentosRequeridos(List<string> tiposDocumentosVerificados)
        {
            return tiposDocumentosVerificados.Contains("DNI") &&
                   tiposDocumentosVerificados.Contains("Recibo de Sueldo") &&
                   tiposDocumentosVerificados.Contains("Veraz") &&
                   (tiposDocumentosVerificados.Contains("Servicio de Luz") ||
                    tiposDocumentosVerificados.Contains("Servicio de Gas") ||
                    tiposDocumentosVerificados.Contains("Servicio de Agua"));
        }

        /// <summary>
        /// Obtiene la lista de documentos faltantes
        /// </summary>
        public static List<string> ObtenerDocumentosFaltantes(List<string> tiposVerificados)
        {
            var faltantes = new List<string>();
            var documentosRequeridos = new (string nombre, Func<List<string>, bool> verificador)[]
            {
                ("DNI", d => d.Contains("DNI")),
                ("Recibo de Sueldo", d => d.Contains("Recibo de Sueldo")),
                ("Veraz", d => d.Contains("Veraz")),
                ("Servicio (Luz/Gas/Agua)", d => d.Contains("Servicio de Luz") || d.Contains("Servicio de Gas") || d.Contains("Servicio de Agua"))
            };

            foreach (var (nombre, verificador) in documentosRequeridos)
            {
                if (!verificador(tiposVerificados))
                    faltantes.Add(nombre);
            }

            return faltantes;
        }

        /// <summary>
        /// Determina el nivel de riesgo basado en el score
        /// </summary>
        public static string DeterminarNivelRiesgo(int score)
        {
            return score switch
            {
                >= 700 => "Bajo",
                >= 500 => "Medio",
                _ => "Alto"
            };
        }

        /// <summary>
        /// Verifica si el cliente cumple con todos los requisitos
        /// </summary>
        public static bool VerificaCumplimientoRequisitos(EvaluacionCreditoResult evaluacion)
        {
            return evaluacion.TieneDocumentosCompletos &&
                   evaluacion.PorcentajeEndeudamiento < 50 &&
                   evaluacion.ScoreCrediticio >= 400 &&
                   (!evaluacion.RequiereGarante || evaluacion.TieneGarante);
        }

        /// <summary>
        /// Genera alertas y recomendaciones para el cliente
        /// </summary>
        public static void GenerarAlertasYRecomendaciones(EvaluacionCreditoResult evaluacion)
        {
            if (!evaluacion.TieneDocumentosCompletos)
                evaluacion.AlertasYRecomendaciones.Add($"⚠️ Faltan documentos: {string.Join(", ", evaluacion.DocumentosFaltantes)}");

            if (evaluacion.PorcentajeEndeudamiento > 40)
                evaluacion.AlertasYRecomendaciones.Add($"⚠️ Endeudamiento alto: {evaluacion.PorcentajeEndeudamiento:F1}%");

            if (evaluacion.RequiereGarante && !evaluacion.TieneGarante)
                evaluacion.AlertasYRecomendaciones.Add("⚠️ Se requiere garante");

            if (evaluacion.ScoreCrediticio < 500)
                evaluacion.AlertasYRecomendaciones.Add($"⚠️ Score crediticio bajo: {evaluacion.ScoreCrediticio}");

            if (evaluacion.MontoMaximoDisponible <= 0)
                evaluacion.AlertasYRecomendaciones.Add("⚠️ Sin capacidad de pago disponible");

            evaluacion.PuedeAprobarConExcepcion = !evaluacion.CumpleRequisitos &&
                                                  evaluacion.IngresosMensuales > 0 &&
                                                  evaluacion.PorcentajeEndeudamiento < 60;

            if (evaluacion.CumpleRequisitos)
                evaluacion.AlertasYRecomendaciones.Add("✅ El cliente cumple con todos los requisitos");
            else if (evaluacion.PuedeAprobarConExcepcion)
                evaluacion.AlertasYRecomendaciones.Add("⚠️ Puede aprobarse con excepción autorizada");
        }
    }
}