using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    /// <summary>
    /// ViewModel consolidado para la vista de detalles del cliente con tabs
    /// </summary>
    public class ClienteDetalleViewModel
    {
        // Información básica del cliente
        public ClienteViewModel Cliente { get; set; } = new();

        // Documentos del cliente
        public List<DocumentoClienteViewModel> Documentos { get; set; } = new();

        // Créditos activos
        public List<CreditoViewModel> CreditosActivos { get; set; } = new();

        // Evaluación de crédito
        public EvaluacionCreditoResult EvaluacionCredito { get; set; } = new();

        // Tab activo (por defecto: información)
        public string TabActivo { get; set; } = "informacion";
    }

    /// <summary>
    /// Resultado de la evaluación para solicitar un crédito
    /// </summary>
    public class EvaluacionCreditoResult
    {
        // Validaciones de documentación
        public bool TieneDocumentosCompletos { get; set; }
        public List<string> DocumentosFaltantes { get; set; } = new();

        // Validaciones de capacidad crediticia
        public decimal MontoMaximoDisponible { get; set; }
        public decimal IngresosMensuales { get; set; }
        public decimal DeudaActual { get; set; }
        public decimal CapacidadPagoMensual { get; set; }
        public double PorcentajeEndeudamiento { get; set; }

        // Score crediticio
        public int ScoreCrediticio { get; set; }
        public string NivelRiesgo { get; set; } = "Medio";

        // Estado general
        public bool CumpleRequisitos { get; set; }
        public List<string> AlertasYRecomendaciones { get; set; } = new();

        // Garante
        public bool RequiereGarante { get; set; }
        public bool TieneGarante { get; set; }
        public string? GaranteNombre { get; set; }

        // Excepciones permitidas
        public bool PuedeAprobarConExcepcion { get; set; }
        public string? MotivoExcepcion { get; set; }
    }

    /// <summary>
    /// ViewModel para solicitar un crédito desde el cliente
    /// </summary>
    public class SolicitudCreditoViewModel
    {
        public int ClienteId { get; set; }
        public decimal MontoSolicitado { get; set; }
        public int CantidadCuotas { get; set; }
        public decimal TasaInteres { get; set; } = 5.0m; // Tasa mensual por defecto
        public string? Observaciones { get; set; }

        // Garante (opcional)
        public int? GaranteId { get; set; }
        public string? GaranteNombre { get; set; }
        public string? GaranteDocumento { get; set; }
        public string? GaranteTelefono { get; set; }

        // Excepción (si no cumple requisitos)
        public bool AprobarConExcepcion { get; set; }
        public string? MotivoExcepcion { get; set; }
        public string? AutorizadoPor { get; set; }
    }
}