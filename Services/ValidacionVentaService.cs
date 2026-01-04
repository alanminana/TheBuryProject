using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    /// <summary>
    /// Implementación del servicio de validación unificada para ventas con crédito personal.
    /// Integra el servicio de aptitud crediticia con las validaciones de venta.
    /// </summary>
    public class ValidacionVentaService : IValidacionVentaService
    {
        private readonly AppDbContext _context;
        private readonly IClienteAptitudService _aptitudService;
        private readonly ILogger<ValidacionVentaService> _logger;

        public ValidacionVentaService(
            AppDbContext context,
            IClienteAptitudService aptitudService,
            ILogger<ValidacionVentaService> logger)
        {
            _context = context;
            _aptitudService = aptitudService;
            _logger = logger;
        }

        #region Prevalidación (E1 - Solo lectura, no persiste)

        /// <inheritdoc />
        public async Task<PrevalidacionResultViewModel> PrevalidarAsync(int clienteId, decimal monto)
        {
            _logger.LogInformation(
                "Iniciando prevalidación para cliente {ClienteId}, monto {Monto}",
                clienteId, monto);

            var resultado = new PrevalidacionResultViewModel
            {
                ClienteId = clienteId,
                MontoSolicitado = monto,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // 1. Evaluar aptitud crediticia (sin guardar)
                var aptitud = await _aptitudService.EvaluarAptitudSinGuardarAsync(clienteId);

                // 2. Poblar información básica del resultado
                PoblarDatosBasicosPrevalidacion(resultado, aptitud);

                // 3. Evaluar según estado de aptitud
                EvaluarEstadoAptitud(resultado, aptitud, monto);

                // 4. Si pasó las validaciones básicas, verificar cupo para el monto específico
                if (resultado.Resultado != ResultadoPrevalidacion.NoViable)
                {
                    await VerificarCupoParaMontoPrevalidacion(resultado, clienteId, monto);
                }

                _logger.LogInformation(
                    "Prevalidación completada para cliente {ClienteId}: {Resultado}",
                    clienteId, resultado.Resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en prevalidación para cliente {ClienteId}", clienteId);

                resultado.Resultado = ResultadoPrevalidacion.NoViable;
                resultado.Motivos.Add(new MotivoPrevalidacion
                {
                    Categoria = CategoriaMotivo.Configuracion,
                    Descripcion = "Error al evaluar aptitud crediticia",
                    EsBloqueante = true
                });

                return resultado;
            }
        }

        private void PoblarDatosBasicosPrevalidacion(
            PrevalidacionResultViewModel resultado,
            AptitudCrediticiaViewModel aptitud)
        {
            // Documentación
            if (aptitud.Documentacion != null)
            {
                resultado.DocumentacionCompleta = aptitud.Documentacion.Completa;
                resultado.DocumentosFaltantes = aptitud.Documentacion.DocumentosFaltantes ?? new List<string>();
                resultado.DocumentosVencidos = aptitud.Documentacion.DocumentosVencidos ?? new List<string>();
            }

            // Cupo
            if (aptitud.Cupo != null)
            {
                resultado.LimiteCredito = aptitud.Cupo.LimiteCredito;
                resultado.CupoDisponible = aptitud.Cupo.CupoDisponible;
                resultado.CreditoUtilizado = aptitud.Cupo.CreditoUtilizado;
            }

            // Mora
            if (aptitud.Mora != null)
            {
                resultado.TieneMora = aptitud.Mora.TieneMora;
                resultado.DiasMora = aptitud.Mora.DiasMaximoMora;
                resultado.MontoMora = aptitud.Mora.MontoTotalMora;
            }
        }

        private void EvaluarEstadoAptitud(
            PrevalidacionResultViewModel resultado,
            AptitudCrediticiaViewModel aptitud,
            decimal monto)
        {
            // Verificar configuración del sistema
            if (!aptitud.ConfiguracionCompleta)
            {
                resultado.Resultado = ResultadoPrevalidacion.NoViable;
                resultado.Motivos.Add(new MotivoPrevalidacion
                {
                    Categoria = CategoriaMotivo.Configuracion,
                    Titulo = "Configuración del sistema",
                    Descripcion = aptitud.AdvertenciaConfiguracion ?? "Sistema de crédito no configurado",
                    AccionSugerida = "Contactar al administrador",
                    EsBloqueante = true
                });
                return;
            }

            switch (aptitud.Estado)
            {
                case EstadoCrediticioCliente.NoEvaluado:
                    EvaluarClienteNoEvaluado(resultado, aptitud);
                    break;

                case EstadoCrediticioCliente.NoApto:
                    EvaluarClienteNoApto(resultado, aptitud);
                    break;

                case EstadoCrediticioCliente.RequiereAutorizacion:
                    EvaluarClienteRequiereAutorizacion(resultado, aptitud);
                    break;

                case EstadoCrediticioCliente.Apto:
                    resultado.Resultado = ResultadoPrevalidacion.Aprobable;
                    break;
            }
        }

        private void EvaluarClienteNoEvaluado(
            PrevalidacionResultViewModel resultado,
            AptitudCrediticiaViewModel aptitud)
        {
            resultado.Resultado = ResultadoPrevalidacion.NoViable;

            // Analizar qué falta para estar evaluado
            if (aptitud.Documentacion != null && !aptitud.Documentacion.Completa)
            {
                var faltantes = string.Join(", ", aptitud.Documentacion.DocumentosFaltantes);
                resultado.Motivos.Add(new MotivoPrevalidacion
                {
                    Categoria = CategoriaMotivo.Documentacion,
                    Titulo = "Documentación incompleta",
                    Descripcion = $"Documentación faltante: {faltantes}",
                    AccionSugerida = "Cargar documentación obligatoria",
                    UrlAccion = $"/DocumentoCliente/Index?clienteId={resultado.ClienteId}",
                    EsBloqueante = true
                });
            }

            if (aptitud.Cupo != null && !aptitud.Cupo.TieneCupoAsignado)
            {
                resultado.Motivos.Add(new MotivoPrevalidacion
                {
                    Categoria = CategoriaMotivo.Cupo,
                    Titulo = "Sin límite de crédito",
                    Descripcion = "El cliente no tiene límite de crédito asignado.",
                    AccionSugerida = "Asignar límite de crédito desde la ficha del cliente",
                    UrlAccion = $"/Cliente/Details/{resultado.ClienteId}",
                    EsBloqueante = true
                });
            }

            // Si no hay motivos específicos, agregar uno genérico
            if (!resultado.Motivos.Any())
            {
                resultado.Motivos.Add(new MotivoPrevalidacion
                {
                    Categoria = CategoriaMotivo.EstadoCliente,
                    Titulo = "Cliente sin evaluar",
                    Descripcion = "El cliente no ha sido evaluado crediticiamente.",
                    AccionSugerida = "Completar evaluación del cliente",
                    UrlAccion = $"/Cliente/Details/{resultado.ClienteId}",
                    EsBloqueante = true
                });
            }
        }

        private void EvaluarClienteNoApto(
            PrevalidacionResultViewModel resultado,
            AptitudCrediticiaViewModel aptitud)
        {
            resultado.Resultado = ResultadoPrevalidacion.NoViable;

            // Analizar los detalles para determinar qué bloquea
            foreach (var detalle in aptitud.Detalles.Where(d => d.EsBloqueo))
            {
                var motivo = new MotivoPrevalidacion
                {
                    Descripcion = detalle.Descripcion,
                    EsBloqueante = true
                };

                switch (detalle.Categoria)
                {
                    case "Documentación":
                        motivo.Categoria = CategoriaMotivo.Documentacion;
                        motivo.Titulo = "Documentación";
                        motivo.AccionSugerida = "Actualizar documentación";
                        motivo.UrlAccion = $"/DocumentoCliente/Index?clienteId={resultado.ClienteId}";
                        break;

                    case "Cupo":
                        motivo.Categoria = CategoriaMotivo.Cupo;
                        motivo.Titulo = "Cupo";
                        motivo.AccionSugerida = "Asignar o aumentar límite de crédito";
                        motivo.UrlAccion = $"/Cliente/Details/{resultado.ClienteId}";
                        break;

                    case "Mora":
                        motivo.Categoria = CategoriaMotivo.Mora;
                        motivo.Titulo = "Mora activa";
                        motivo.AccionSugerida = "Regularizar mora antes de continuar";
                        motivo.UrlAccion = $"/Mora/FichaCliente/{resultado.ClienteId}";
                        break;

                    default:
                        motivo.Categoria = CategoriaMotivo.EstadoCliente;
                        motivo.Titulo = "Estado del cliente";
                        break;
                }

                resultado.Motivos.Add(motivo);
            }

            // Si no hay detalles específicos, usar el motivo general
            if (!resultado.Motivos.Any())
            {
                resultado.Motivos.Add(new MotivoPrevalidacion
                {
                    Categoria = CategoriaMotivo.EstadoCliente,
                    Titulo = "Cliente no apto",
                    Descripcion = aptitud.Motivo ?? "Cliente no apto para crédito",
                    AccionSugerida = "Revisar estado del cliente",
                    UrlAccion = $"/Cliente/Details/{resultado.ClienteId}",
                    EsBloqueante = true
                });
            }
        }

        private void EvaluarClienteRequiereAutorizacion(
            PrevalidacionResultViewModel resultado,
            AptitudCrediticiaViewModel aptitud)
        {
            resultado.Resultado = ResultadoPrevalidacion.RequiereAutorizacion;

            // Analizar razones de por qué requiere autorización
            foreach (var detalle in aptitud.Detalles.Where(d => !d.EsBloqueo))
            {
                var motivo = new MotivoPrevalidacion
                {
                    Descripcion = detalle.Descripcion,
                    EsBloqueante = false
                };

                switch (detalle.Categoria)
                {
                    case "Mora":
                        motivo.Categoria = CategoriaMotivo.Mora;
                        motivo.Titulo = "Mora no bloqueante";
                        motivo.AccionSugerida = "Supervisor debe autorizar venta";
                        break;

                    default:
                        motivo.Categoria = CategoriaMotivo.EstadoCliente;
                        motivo.Titulo = "Requiere revisión";
                        break;
                }

                resultado.Motivos.Add(motivo);
            }

            // Si no hay detalles específicos, agregar razón genérica
            if (!resultado.Motivos.Any())
            {
                resultado.Motivos.Add(new MotivoPrevalidacion
                {
                    Categoria = CategoriaMotivo.EstadoCliente,
                    Titulo = "Requiere autorización",
                    Descripcion = aptitud.Motivo ?? "Cliente requiere autorización para crédito",
                    AccionSugerida = "La venta requerirá aprobación de un supervisor",
                    EsBloqueante = false
                });
            }
        }

        private async Task VerificarCupoParaMontoPrevalidacion(
            PrevalidacionResultViewModel resultado,
            int clienteId,
            decimal monto)
        {
            // Obtener cupo disponible actual
            var cupoDisponible = await _aptitudService.GetCupoDisponibleAsync(clienteId);
            resultado.CupoDisponible = cupoDisponible;

            if (monto > cupoDisponible)
            {
                // Si ya es NoViable, no cambiar
                if (resultado.Resultado == ResultadoPrevalidacion.NoViable)
                    return;

                // Si excede cupo pero hay cupo asignado, requiere autorización
                if (resultado.LimiteCredito.HasValue && resultado.LimiteCredito.Value > 0)
                {
                    resultado.Resultado = ResultadoPrevalidacion.RequiereAutorizacion;
                    resultado.Motivos.Add(new MotivoPrevalidacion
                    {
                        Categoria = CategoriaMotivo.Cupo,
                        Titulo = "Cupo insuficiente",
                        Descripcion = $"El monto solicitado ({monto:C0}) excede el cupo disponible ({cupoDisponible:C0}).",
                        AccionSugerida = "Supervisor debe autorizar exceso de cupo",
                        EsBloqueante = false
                    });
                }
                else
                {
                    // Sin cupo asignado, es bloqueante
                    resultado.Resultado = ResultadoPrevalidacion.NoViable;
                    resultado.Motivos.Add(new MotivoPrevalidacion
                    {
                        Categoria = CategoriaMotivo.Cupo,
                        Titulo = "Sin límite de crédito",
                        Descripcion = "El cliente no tiene límite de crédito asignado.",
                        AccionSugerida = "Asignar límite de crédito al cliente",
                        UrlAccion = $"/Cliente/Details/{clienteId}",
                        EsBloqueante = true
                    });
                }
            }
        }

        #endregion

        #region Validación de Venta (Métodos existentes)

        public async Task<ValidacionVentaResult> ValidarVentaCreditoPersonalAsync(
            int clienteId,
            decimal montoVenta,
            int? creditoId = null)
        {
            var resultado = new ValidacionVentaResult();

            // 1. Evaluar aptitud crediticia del cliente (sin guardar aún)
            var aptitud = await _aptitudService.EvaluarAptitudSinGuardarAsync(clienteId);
            resultado.EstadoAptitud = aptitud.Estado;

            // 2. Procesar según estado de aptitud
            switch (aptitud.Estado)
            {
                case EstadoCrediticioCliente.NoEvaluado:
                    await ProcesarClienteNoEvaluado(resultado, clienteId, aptitud);
                    break;

                case EstadoCrediticioCliente.NoApto:
                    ProcesarClienteNoApto(resultado, aptitud);
                    break;

                case EstadoCrediticioCliente.RequiereAutorizacion:
                    await ProcesarClienteRequiereAutorizacion(resultado, clienteId, montoVenta, aptitud, creditoId);
                    break;

                case EstadoCrediticioCliente.Apto:
                    await ProcesarClienteApto(resultado, clienteId, montoVenta, creditoId);
                    break;
            }

            return resultado;
        }

        private async Task ProcesarClienteNoEvaluado(
            ValidacionVentaResult resultado,
            int clienteId,
            AptitudCrediticiaViewModel aptitud)
        {
            // Cliente NoEvaluado = NoViable (no puede guardarse venta)
            resultado.NoViable = true;
            resultado.PendienteRequisitos = true;

            // Verificar si es por falta de configuración
            if (!aptitud.ConfiguracionCompleta)
            {
                resultado.RequisitosPendientes.Add(new RequisitoPendiente
                {
                    Tipo = TipoRequisitoPendiente.SinEvaluacionCrediticia,
                    Descripcion = "Sistema de crédito no configurado",
                    AccionRequerida = aptitud.AdvertenciaConfiguracion ?? "Contactar al administrador"
                });
                return;
            }

            // Verificar documentación
            if (aptitud.Documentacion != null && !aptitud.Documentacion.Completa)
            {
                var faltantes = string.Join(", ", aptitud.Documentacion.DocumentosFaltantes);
                resultado.RequisitosPendientes.Add(new RequisitoPendiente
                {
                    Tipo = TipoRequisitoPendiente.DocumentacionFaltante,
                    Descripcion = $"Documentación faltante: {faltantes}",
                    AccionRequerida = "Cargar documentación obligatoria",
                    UrlAccion = $"/DocumentoCliente/Index?clienteId={clienteId}"
                });
            }

            // Verificar límite de crédito
            if (aptitud.Cupo != null && !aptitud.Cupo.TieneCupoAsignado)
            {
                resultado.RequisitosPendientes.Add(new RequisitoPendiente
                {
                    Tipo = TipoRequisitoPendiente.SinLimiteCredito,
                    Descripcion = "Cliente sin límite de crédito asignado",
                    AccionRequerida = "Asignar límite de crédito al cliente",
                    UrlAccion = $"/Cliente/Details/{clienteId}"
                });
            }

            await Task.CompletedTask;
        }

        private void ProcesarClienteNoApto(
            ValidacionVentaResult resultado,
            AptitudCrediticiaViewModel aptitud)
        {
            // Cliente NoApto = NoViable (no puede guardarse venta)
            resultado.NoViable = true;
            resultado.PendienteRequisitos = true;

            // Analizar los detalles para determinar qué bloquea
            foreach (var detalle in aptitud.Detalles.Where(d => d.EsBloqueo))
            {
                switch (detalle.Categoria)
                {
                    case "Documentación":
                        resultado.RequisitosPendientes.Add(new RequisitoPendiente
                        {
                            Tipo = detalle.Descripcion.Contains("vencido")
                                ? TipoRequisitoPendiente.DocumentacionFaltante
                                : TipoRequisitoPendiente.DocumentacionFaltante,
                            Descripcion = detalle.Descripcion,
                            AccionRequerida = "Actualizar documentación"
                        });
                        break;

                    case "Cupo":
                        resultado.RequisitosPendientes.Add(new RequisitoPendiente
                        {
                            Tipo = TipoRequisitoPendiente.SinLimiteCredito,
                            Descripcion = detalle.Descripcion,
                            AccionRequerida = "Asignar o aumentar límite de crédito"
                        });
                        break;

                    case "Mora":
                        resultado.RequisitosPendientes.Add(new RequisitoPendiente
                        {
                            Tipo = TipoRequisitoPendiente.ClienteNoApto,
                            Descripcion = detalle.Descripcion,
                            AccionRequerida = "Regularizar mora antes de continuar"
                        });
                        break;
                }
            }

            if (!resultado.RequisitosPendientes.Any())
            {
                resultado.RequisitosPendientes.Add(new RequisitoPendiente
                {
                    Tipo = TipoRequisitoPendiente.ClienteNoApto,
                    Descripcion = aptitud.Motivo ?? "Cliente no apto para crédito",
                    AccionRequerida = "Revisar estado del cliente"
                });
            }
        }

        private async Task ProcesarClienteRequiereAutorizacion(
            ValidacionVentaResult resultado,
            int clienteId,
            decimal montoVenta,
            AptitudCrediticiaViewModel aptitud,
            int? creditoId)
        {
            // El cliente puede recibir crédito pero requiere autorización
            resultado.RequiereAutorizacion = true;

            // Agregar razones de autorización
            foreach (var detalle in aptitud.Detalles.Where(d => !d.EsBloqueo))
            {
                if (detalle.Categoria == "Mora")
                {
                    resultado.RazonesAutorizacion.Add(new RazonAutorizacion
                    {
                        Tipo = TipoRazonAutorizacion.MoraActiva,
                        Descripcion = "Cliente tiene mora activa",
                        DetalleAdicional = detalle.Descripcion,
                        ValorAsociado = aptitud.Mora?.DiasMaximoMora
                    });
                }
            }

            // Agregar razón genérica si corresponde
            if (!resultado.RazonesAutorizacion.Any())
            {
                resultado.RazonesAutorizacion.Add(new RazonAutorizacion
                {
                    Tipo = TipoRazonAutorizacion.ClienteRequiereAutorizacion,
                    Descripcion = "Cliente requiere autorización para crédito",
                    DetalleAdicional = aptitud.Motivo
                });
            }

            // Verificar también si el monto excede el cupo
            await VerificarCupoParaMonto(resultado, clienteId, montoVenta, creditoId);
        }

        private async Task ProcesarClienteApto(
            ValidacionVentaResult resultado,
            int clienteId,
            decimal montoVenta,
            int? creditoId)
        {
            // Cliente apto, verificar que el monto no exceda el cupo
            await VerificarCupoParaMonto(resultado, clienteId, montoVenta, creditoId);
        }

        private async Task VerificarCupoParaMonto(
            ValidacionVentaResult resultado,
            int clienteId,
            decimal montoVenta,
            int? creditoId)
        {
            // Si se especifica un crédito, verificar contra ese crédito específico
            if (creditoId.HasValue)
            {
                var credito = await _context.Creditos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == creditoId.Value && !c.IsDeleted);

                if (credito == null)
                {
                    resultado.PendienteRequisitos = true;
                    resultado.RequisitosPendientes.Add(new RequisitoPendiente
                    {
                        Tipo = TipoRequisitoPendiente.SinCreditoAprobado,
                        Descripcion = "El crédito especificado no existe o fue eliminado"
                    });
                    return;
                }

                if (credito.Estado != EstadoCredito.Activo && credito.Estado != EstadoCredito.Aprobado)
                {
                    resultado.PendienteRequisitos = true;
                    resultado.RequisitosPendientes.Add(new RequisitoPendiente
                    {
                        Tipo = TipoRequisitoPendiente.SinCreditoAprobado,
                        Descripcion = $"El crédito no está activo (estado: {credito.Estado})"
                    });
                    return;
                }

                if (montoVenta > credito.SaldoPendiente)
                {
                    resultado.RequiereAutorizacion = true;
                    resultado.RazonesAutorizacion.Add(new RazonAutorizacion
                    {
                        Tipo = TipoRazonAutorizacion.ExcedeCupo,
                        Descripcion = "El monto excede el saldo disponible del crédito",
                        ValorAsociado = montoVenta,
                        ValorLimite = credito.SaldoPendiente,
                        DetalleAdicional = $"Solicitado: {montoVenta:C0}, Disponible: {credito.SaldoPendiente:C0}"
                    });
                }
                return;
            }

            // Verificar contra el cupo general del cliente
            var cupoDisponible = await _aptitudService.GetCupoDisponibleAsync(clienteId);
            if (montoVenta > cupoDisponible)
            {
                resultado.RequiereAutorizacion = true;
                resultado.RazonesAutorizacion.Add(new RazonAutorizacion
                {
                    Tipo = TipoRazonAutorizacion.ExcedeCupo,
                    Descripcion = "El monto excede el cupo disponible del cliente",
                    ValorAsociado = montoVenta,
                    ValorLimite = cupoDisponible,
                    DetalleAdicional = $"Solicitado: {montoVenta:C0}, Disponible: {cupoDisponible:C0}"
                });
            }
        }

        public async Task<ValidacionVentaResult> ValidarConfirmacionVentaAsync(int ventaId)
        {
            var venta = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.Id == ventaId && !v.IsDeleted);

            if (venta == null)
            {
                return new ValidacionVentaResult
                {
                    PendienteRequisitos = true,
                    RequisitosPendientes = new List<RequisitoPendiente>
                    {
                        new() { Tipo = TipoRequisitoPendiente.SinCreditoAprobado, Descripcion = "Venta no encontrada" }
                    }
                };
            }

            // Si no es crédito personal, no hay validaciones adicionales
            if (venta.TipoPago != TipoPago.CreditoPersonall)
            {
                return new ValidacionVentaResult(); // Puede proceder
            }

            return await ValidarVentaCreditoPersonalAsync(venta.ClienteId, venta.Total, venta.CreditoId);
        }

        public async Task<bool> ClientePuedeRecibirCreditoAsync(int clienteId, decimal montoSolicitado)
        {
            var resultado = await ValidarVentaCreditoPersonalAsync(clienteId, montoSolicitado);
            return resultado.PuedeProceeder;
        }

        public async Task<ResumenCrediticioClienteViewModel> ObtenerResumenCrediticioAsync(int clienteId)
        {
            var resumen = new ResumenCrediticioClienteViewModel();

            // Evaluar aptitud (sin guardar)
            var aptitud = await _aptitudService.EvaluarAptitudSinGuardarAsync(clienteId);

            resumen.EstadoAptitud = aptitud.TextoEstado;
            resumen.ColorSemaforo = aptitud.ColorSemaforo;
            resumen.Icono = aptitud.Icono;

            // Información de documentación
            if (aptitud.Documentacion != null)
            {
                resumen.DocumentacionCompleta = aptitud.Documentacion.Completa;
                if (!aptitud.Documentacion.Completa)
                {
                    resumen.DocumentosFaltantes = string.Join(", ", aptitud.Documentacion.DocumentosFaltantes);
                }
            }

            // Información de cupo
            if (aptitud.Cupo != null)
            {
                resumen.LimiteCredito = aptitud.Cupo.LimiteCredito;
                resumen.CupoDisponible = aptitud.Cupo.CupoDisponible;
                resumen.CreditoUtilizado = aptitud.Cupo.CreditoUtilizado;
            }

            // Información de mora
            if (aptitud.Mora != null)
            {
                resumen.TieneMoraActiva = aptitud.Mora.TieneMora;
                resumen.DiasMaxMora = aptitud.Mora.DiasMaximoMora;
            }

            // Mensaje de advertencia
            if (aptitud.Estado == EstadoCrediticioCliente.NoApto)
            {
                resumen.MensajeAdvertencia = aptitud.Motivo ?? "Cliente no apto para crédito";
            }
            else if (aptitud.Estado == EstadoCrediticioCliente.RequiereAutorizacion)
            {
                resumen.MensajeAdvertencia = "Este cliente requiere autorización para crédito";
            }
            else if (aptitud.Estado == EstadoCrediticioCliente.NoEvaluado && !aptitud.ConfiguracionCompleta)
            {
                resumen.MensajeAdvertencia = aptitud.AdvertenciaConfiguracion;
            }

            // Créditos activos
            var creditosActivos = await _context.Creditos
                .AsNoTracking()
                .Where(c => c.ClienteId == clienteId &&
                           !c.IsDeleted &&
                           (c.Estado == EstadoCredito.Activo || c.Estado == EstadoCredito.Aprobado))
                .ToListAsync();

            resumen.CreditosActivos = creditosActivos.Select(c => new CreditoActivoResumen
            {
                Id = c.Id,
                Numero = c.Numero,
                MontoAprobado = c.MontoAprobado,
                SaldoDisponible = c.SaldoPendiente,
                Estado = c.Estado.ToString()
            }).ToList();

            return resumen;
        }
        
        #endregion
    }
}
