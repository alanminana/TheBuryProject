using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    public class EvaluacionCreditoService : IEvaluacionCreditoService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<EvaluacionCreditoService> _logger;

        private const decimal PUNTAJE_RIESGO_MINIMO = 3.0m;
        private const decimal RELACION_CUOTA_INGRESO_MAX = 0.35m; // 35% del sueldo
        private const decimal MONTO_REQUIERE_GARANTE = 500000m;

        public EvaluacionCreditoService(
            AppDbContext context,
            IMapper mapper,
            ILogger<EvaluacionCreditoService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<EvaluacionCreditoViewModel> EvaluarSolicitudAsync(
            int clienteId,
            decimal montoSolicitado,
            int? garanteId = null)
        {
            try
            {
                _logger.LogInformation("=== INICIANDO EVALUACIÓN CREDITICIA ===");
                _logger.LogInformation("ClienteId: {ClienteId}, Monto: {Monto}, GaranteId: {GaranteId}",
                    clienteId, montoSolicitado, garanteId);

                var cliente = await _context.Clientes
                    .Include(c => c.Creditos)
                    .FirstOrDefaultAsync(c => c.Id == clienteId && !c.IsDeleted);

                if (cliente == null)
                    throw new Exception($"Cliente {clienteId} no encontrado");

                var evaluacion = new EvaluacionCreditoViewModel
                {
                    ClienteId = clienteId,
                    ClienteNombre = $"{cliente.Apellido}, {cliente.Nombre}",
                    MontoSolicitado = montoSolicitado,
                    SueldoCliente = cliente.Sueldo,
                    PuntajeRiesgoCliente = cliente.PuntajeRiesgo,
                    TieneGarante = garanteId.HasValue,
                    FechaEvaluacion = DateTime.Now,
                    Reglas = new List<ReglaEvaluacionViewModel>()
                };

                decimal puntajeTotal = 0;

                // 1️⃣ Puntaje de riesgo
                var reglaPuntaje = EvaluarPuntajeRiesgo(cliente.PuntajeRiesgo);
                evaluacion.Reglas.Add(reglaPuntaje);
                puntajeTotal += reglaPuntaje.Peso;

                // 2️⃣ Documentación (usa DocumentosCliente)
                var reglaDoc = await EvaluarDocumentacionAsync(clienteId);
                evaluacion.Reglas.Add(reglaDoc);
                evaluacion.TieneDocumentacionCompleta = reglaDoc.Cumple;
                puntajeTotal += reglaDoc.Peso;

                // 3️⃣ Ingresos
                var reglaIngresos = EvaluarIngresos(cliente, montoSolicitado);
                evaluacion.Reglas.Add(reglaIngresos);
                evaluacion.TieneIngresosSuficientes = reglaIngresos.Cumple;
                evaluacion.RelacionCuotaIngreso = reglaIngresos.Cumple && cliente.Sueldo.HasValue && cliente.Sueldo > 0
                    ? (montoSolicitado * 0.10m) / cliente.Sueldo.Value
                    : null;
                puntajeTotal += reglaIngresos.Peso;

                // 4️⃣ Historial crediticio
                var reglaHistorial = EvaluarHistorial(cliente);
                evaluacion.Reglas.Add(reglaHistorial);
                evaluacion.TieneBuenHistorial = reglaHistorial.Cumple;
                puntajeTotal += reglaHistorial.Peso;

                // 5️⃣ Garante
                var reglaGarante = EvaluarGarante(montoSolicitado, garanteId.HasValue);
                evaluacion.Reglas.Add(reglaGarante);
                puntajeTotal += reglaGarante.Peso;

                evaluacion.PuntajeFinal = Math.Max(0, puntajeTotal);

                // Resultado general
                if (evaluacion.PuntajeFinal >= 70)
                {
                    evaluacion.Resultado = ResultadoEvaluacion.Aprobado;
                    evaluacion.Motivo = $"Cliente calificado. Puntaje: {evaluacion.PuntajeFinal}/100. Cumple con los criterios principales.";
                }
                else if (evaluacion.PuntajeFinal >= 50)
                {
                    evaluacion.Resultado = ResultadoEvaluacion.RequiereAnalisis;
                    evaluacion.Motivo = $"Cliente requiere análisis manual. Puntaje: {evaluacion.PuntajeFinal}/100. Revisar: {ObtenerMotivosAnalisis(evaluacion)}";
                }
                else
                {
                    evaluacion.Resultado = ResultadoEvaluacion.Rechazado;
                    evaluacion.Motivo = $"Cliente no califica. Puntaje: {evaluacion.PuntajeFinal}/100. Razones: {ObtenerMotivosRechazo(evaluacion)}";
                }

                _logger.LogInformation("Evaluación completada: {Resultado} - Puntaje: {Puntaje}",
                    evaluacion.Resultado, evaluacion.PuntajeFinal);

                return evaluacion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar solicitud de crédito");
                throw;
            }
        }

        private ReglaEvaluacionViewModel EvaluarPuntajeRiesgo(decimal puntajeRiesgo)
        {
            var regla = new ReglaEvaluacionViewModel { Nombre = "Puntaje de Riesgo" };

            if (puntajeRiesgo >= 7.0m)
                (regla.Cumple, regla.Peso, regla.Detalle) = (true, 30, $"Excelente: {puntajeRiesgo}/10");
            else if (puntajeRiesgo >= 5.0m)
                (regla.Cumple, regla.Peso, regla.Detalle) = (true, 20, $"Bueno: {puntajeRiesgo}/10");
            else if (puntajeRiesgo >= PUNTAJE_RIESGO_MINIMO)
                (regla.Cumple, regla.Peso, regla.Detalle) = (true, 10, $"Aceptable: {puntajeRiesgo}/10");
            else
                (regla.Cumple, regla.Peso, regla.Detalle) = (false, 0, $"Insuficiente: {puntajeRiesgo}/10 (mínimo {PUNTAJE_RIESGO_MINIMO})");

            return regla;
        }

        private async Task<ReglaEvaluacionViewModel> EvaluarDocumentacionAsync(int clienteId)
        {
            var regla = new ReglaEvaluacionViewModel { Nombre = "Documentación" };

            var documentosVerificados = await _context.Set<DocumentoCliente>()
                .Where(d => d.ClienteId == clienteId
                         && !d.IsDeleted
                         && d.Estado == EstadoDocumento.Verificado
                         && (!d.FechaVencimiento.HasValue || d.FechaVencimiento.Value >= DateTime.Today))
                .ToListAsync();

            int docsImportantes = 0;

            if (documentosVerificados.Any(d => d.TipoDocumento == TipoDocumentoCliente.DNI))
                docsImportantes++;

            if (documentosVerificados.Any(d => d.TipoDocumento == TipoDocumentoCliente.ReciboSueldo))
                docsImportantes++;

            if (documentosVerificados.Any(d => d.TipoDocumento == TipoDocumentoCliente.ServicioLuz ||
                                              d.TipoDocumento == TipoDocumentoCliente.ServicioGas ||
                                              d.TipoDocumento == TipoDocumentoCliente.ServicioAgua))
                docsImportantes++;

            bool tieneVeraz = documentosVerificados.Any(d => d.TipoDocumento == TipoDocumentoCliente.Veraz);
            bool tieneCUIL = documentosVerificados.Any(d => d.TipoDocumento == TipoDocumentoCliente.ConstanciaCUIL);

            if (docsImportantes >= 3)
            {
                regla.Cumple = true;
                regla.Peso = tieneVeraz || tieneCUIL ? 25 : 20;
                regla.Detalle = $"Documentación completa ({documentosVerificados.Count} documentos verificados)";
            }
            else if (docsImportantes >= 2)
            {
                regla.Cumple = true;
                regla.Peso = 10;
                regla.Detalle = $"Documentación parcial ({docsImportantes}/3 documentos importantes)";
            }
            else
            {
                regla.Cumple = false;
                regla.Peso = 0;
                regla.Detalle = $"Documentación insuficiente ({docsImportantes}/3 documentos importantes)";
            }

            return regla;
        }

        private ReglaEvaluacionViewModel EvaluarIngresos(Cliente cliente, decimal montoSolicitado)
        {
            var regla = new ReglaEvaluacionViewModel { Nombre = "Capacidad de Pago" };

            if (!cliente.Sueldo.HasValue || cliente.Sueldo <= 0)
                return new ReglaEvaluacionViewModel { Nombre = "Capacidad de Pago", Cumple = false, Peso = 0, Detalle = "No declaró ingresos" };

            decimal cuotaEstimada = montoSolicitado * 0.10m;
            decimal relacionCuotaIngreso = cuotaEstimada / cliente.Sueldo.Value;

            if (relacionCuotaIngreso <= 0.25m)
                (regla.Cumple, regla.Peso, regla.Detalle) = (true, 25, $"Excelente capacidad de pago ({relacionCuotaIngreso:P0} del sueldo)");
            else if (relacionCuotaIngreso <= RELACION_CUOTA_INGRESO_MAX)
                (regla.Cumple, regla.Peso, regla.Detalle) = (true, 15, $"Capacidad de pago aceptable ({relacionCuotaIngreso:P0} del sueldo)");
            else if (relacionCuotaIngreso <= 0.45m)
                (regla.Cumple, regla.Peso, regla.Detalle) = (false, 5, $"Capacidad ajustada ({relacionCuotaIngreso:P0} del sueldo)");
            else
                (regla.Cumple, regla.Peso, regla.Detalle) = (false, -10, $"Pago insuficiente ({relacionCuotaIngreso:P0}, máximo {RELACION_CUOTA_INGRESO_MAX:P0})");

            return regla;
        }

        private ReglaEvaluacionViewModel EvaluarHistorial(Cliente cliente)
        {
            var regla = new ReglaEvaluacionViewModel { Nombre = "Historial Crediticio" };

            var creditos = cliente.Creditos.Where(c => !c.IsDeleted && c.Estado != EstadoCredito.Solicitado).ToList();

            if (!creditos.Any())
                return new ReglaEvaluacionViewModel
                {
                    Nombre = "Historial Crediticio",
                    Cumple = true,
                    Peso = 10,
                    Detalle = "Sin historial previo (cliente nuevo)"
                };

            var finalizados = creditos.Count(c => c.Estado == EstadoCredito.Finalizado);
            var cancelados = creditos.Count(c => c.Estado == EstadoCredito.Cancelado);
            var activos = creditos.Count(c => c.Estado == EstadoCredito.Activo);

            if (cancelados > 0)
                (regla.Cumple, regla.Peso, regla.Detalle) = (false, -15, $"Historial negativo: {cancelados} crédito(s) cancelado(s)");
            else if (finalizados >= 2)
                (regla.Cumple, regla.Peso, regla.Detalle) = (true, 15, $"Excelente historial: {finalizados} crédito(s) pagado(s)");
            else if (finalizados >= 1 || activos > 0)
                (regla.Cumple, regla.Peso, regla.Detalle) = (true, 10, $"Buen historial: {finalizados} finalizado(s), {activos} activo(s)");
            else
                (regla.Cumple, regla.Peso, regla.Detalle) = (true, 5, "Historial en construcción");

            return regla;
        }

        private ReglaEvaluacionViewModel EvaluarGarante(decimal montoSolicitado, bool tieneGarante)
        {
            var regla = new ReglaEvaluacionViewModel { Nombre = "Garantía" };

            if (montoSolicitado >= MONTO_REQUIERE_GARANTE)
            {
                if (tieneGarante)
                    (regla.Cumple, regla.Peso, regla.Detalle) = (true, 10, $"Garante presente (requerido ≥ ${MONTO_REQUIERE_GARANTE:N0})");
                else
                    (regla.Cumple, regla.Peso, regla.Detalle) = (false, -10, $"Falta garante (requerido ≥ ${MONTO_REQUIERE_GARANTE:N0})");
            }
            else
            {
                if (tieneGarante)
                    (regla.Cumple, regla.Peso, regla.Detalle) = (true, 5, "Garante adicional (no requerido)");
                else
                    (regla.Cumple, regla.Peso, regla.Detalle) = (true, 0, "Garante no requerido");
            }

            return regla;
        }

        private string ObtenerMotivosAnalisis(EvaluacionCreditoViewModel eval) =>
            string.Join(", ", eval.Reglas.Where(r => !r.Cumple || r.Peso < 15).Select(r => r.Nombre));

        private string ObtenerMotivosRechazo(EvaluacionCreditoViewModel eval) =>
            string.Join("; ", eval.Reglas.Where(r => !r.Cumple).Select(r => $"{r.Nombre} ({r.Detalle})"));

        public async Task<EvaluacionCreditoViewModel?> GetEvaluacionByCreditoIdAsync(int creditoId)
        {
            var evaluacion = await _context.Set<EvaluacionCredito>()
                .Include(e => e.Cliente)
                .Include(e => e.Credito)
                .Where(e => e.CreditoId == creditoId && !e.IsDeleted)
                .OrderByDescending(e => e.FechaEvaluacion)
                .FirstOrDefaultAsync();

            return evaluacion != null ? _mapper.Map<EvaluacionCreditoViewModel>(evaluacion) : null;
        }

        public async Task<List<EvaluacionCreditoViewModel>> GetEvaluacionesByClienteIdAsync(int clienteId)
        {
            var evaluaciones = await _context.Set<EvaluacionCredito>()
                .Include(e => e.Credito)
                .Where(e => e.ClienteId == clienteId && !e.IsDeleted)
                .OrderByDescending(e => e.FechaEvaluacion)
                .ToListAsync();

            return _mapper.Map<List<EvaluacionCreditoViewModel>>(evaluaciones);
        }
    }
}
