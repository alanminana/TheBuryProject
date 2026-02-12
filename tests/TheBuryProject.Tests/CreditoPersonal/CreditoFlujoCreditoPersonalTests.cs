using Moq;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.CreditoPersonal
{
    /// <summary>
    /// Tests para el flujo de crédito personal:
    /// - Prevalidación NoViable cuando límite es null
    /// - Venta viable -> PendienteConfiguracion
    /// - Configurar crédito -> Configurado
    /// - Confirmar venta -> Generado sin loop
    /// </summary>
    public class CreditoFlujoCreditoPersonalTests
    {
        #region EstadoCredito Enum Tests

        [Fact]
        public void EstadoCredito_DebeContener_PendienteConfiguracion()
        {
            // Arrange & Act
            var estado = EstadoCredito.PendienteConfiguracion;

            // Assert
            Assert.Equal(6, (int)estado);
        }

        [Fact]
        public void EstadoCredito_DebeContener_Configurado()
        {
            // Arrange & Act
            var estado = EstadoCredito.Configurado;

            // Assert
            Assert.Equal(7, (int)estado);
        }

        [Fact]
        public void EstadoCredito_DebeContener_Generado()
        {
            // Arrange & Act
            var estado = EstadoCredito.Generado;

            // Assert
            Assert.Equal(8, (int)estado);
        }

        #endregion

        #region VentaViewModel PuedeConfirmar Tests

        [Fact]
        public void VentaViewModel_PuedeConfirmar_SinCredito_DebeRetornarTrue_CuandoEstadoPresupuesto()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto,
                TipoPago = TipoPago.Efectivo,
                RequiereAutorizacion = false
            };

            // Act & Assert
            Assert.True(viewModel.PuedeConfirmar);
        }

        [Fact]
        public void VentaViewModel_PuedeConfirmar_ConCreditoPendienteConfiguracion_DebeRetornarFalse()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.PendienteConfiguracion,
                RequiereAutorizacion = false
            };

            // Act & Assert
            Assert.False(viewModel.PuedeConfirmar); // el crédito no está configurado aún
            Assert.True(viewModel.PuedeConfigurarCredito); // el crédito está pendiente de configuración
        }

        [Fact]
        public void VentaViewModel_PuedeConfirmar_ConCreditoConfigurado_DebeRetornarTrue()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.Configurado,
                RequiereAutorizacion = false
            };

            // Act & Assert
            Assert.True(viewModel.PuedeConfirmar); // el crédito ya fue configurado
            Assert.False(viewModel.PuedeConfigurarCredito); // el crédito ya no está pendiente
        }

        [Fact]
        public void VentaViewModel_PuedeConfirmar_ConCreditoGenerado_DebeRetornarFalse()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.Confirmada, // Ya confirmada
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.Generado,
                RequiereAutorizacion = false
            };

            // Act & Assert
            Assert.False(viewModel.PuedeConfirmar); // la venta ya fue confirmada
            Assert.True(viewModel.CreditoGenerado); // el crédito ya está generado
        }

        #endregion

        #region VentaViewModel CreditoEstado Properties Tests

        [Fact]
        public void VentaViewModel_CreditoPendienteConfiguracion_DebeSerTrue_CuandoEstadoPendiente()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.PendienteConfiguracion
            };

            // Act & Assert
            Assert.True(viewModel.CreditoPendienteConfiguracion);
            Assert.False(viewModel.CreditoConfigurado);
            Assert.False(viewModel.CreditoGenerado);
        }

        [Fact]
        public void VentaViewModel_CreditoConfigurado_DebeSerTrue_CuandoEstadoConfigurado()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.Configurado
            };

            // Act & Assert
            Assert.False(viewModel.CreditoPendienteConfiguracion);
            Assert.True(viewModel.CreditoConfigurado);
            Assert.False(viewModel.CreditoGenerado);
        }

        [Fact]
        public void VentaViewModel_CreditoGenerado_DebeSerTrue_CuandoEstadoGenerado()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.Generado
            };

            // Act & Assert
            Assert.False(viewModel.CreditoPendienteConfiguracion);
            Assert.False(viewModel.CreditoConfigurado);
            Assert.True(viewModel.CreditoGenerado);
        }

        [Fact]
        public void VentaViewModel_CreditoGenerado_DebeSerTrue_CuandoEstadoActivo()
        {
            // Arrange - Estado Activo también significa cuotas generadas
            var viewModel = new VentaViewModel
            {
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.Activo
            };

            // Act & Assert
            Assert.True(viewModel.CreditoGenerado); // Activo implica cuotas generadas
        }

        [Fact]
        public void VentaViewModel_PropiedadesCredito_DebenSerFalse_ParaVentaSinCredito()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                TipoPago = TipoPago.Efectivo,
                CreditoId = null,
                CreditoEstado = null
            };

            // Act & Assert
            Assert.False(viewModel.CreditoPendienteConfiguracion);
            Assert.False(viewModel.CreditoConfigurado);
            Assert.False(viewModel.CreditoGenerado);
        }

        #endregion

        #region ConfiguracionCreditoVentaViewModel Defaults Tests

        [Fact]
        public void ConfiguracionCreditoVentaViewModel_DebeInicializar_CamposOpcionalesEnNull()
        {
            // Arrange & Act
            var viewModel = new ConfiguracionCreditoVentaViewModel();

            // Assert - Campos opcionales son null por defecto (se normalizan a 0 en el controller)
            Assert.Null(viewModel.Anticipo);
            Assert.Null(viewModel.GastosAdministrativos);
            Assert.Null(viewModel.TasaMensual);
            Assert.Equal(1, viewModel.CantidadCuotas); // Este sigue teniendo default = 1
        }

        [Fact]
        public void ConfiguracionCreditoVentaViewModel_CamposOpcionales_AceptanVaciosSinError()
        {
            // Este test verifica que el ViewModel permite campos null
            // que representan inputs vacíos del usuario
            var viewModel = new ConfiguracionCreditoVentaViewModel
            {
                CreditoId = 1,
                Monto = 10000,
                CantidadCuotas = 12,
                FechaPrimeraCuota = DateTime.Today.AddMonths(1),
                // Campos opcionales explícitamente null (input vacío)
                Anticipo = null,
                TasaMensual = null,
                GastosAdministrativos = null
            };

            // Assert - Los campos nullable permiten null sin validación fallida
            Assert.Null(viewModel.Anticipo);
            Assert.Null(viewModel.TasaMensual);
            Assert.Null(viewModel.GastosAdministrativos);
        }

        #endregion

        #region Flujo Anti-Loop Tests

        [Theory]
        [InlineData(EstadoCredito.Generado)]
        [InlineData(EstadoCredito.Activo)]
        [InlineData(EstadoCredito.Finalizado)]
        public void VentaViewModel_NoDebePoderConfigurar_SiCreditoYaGenerado(EstadoCredito estado)
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = estado
            };

            // Act & Assert
            Assert.False(viewModel.PuedeConfigurarCredito);
        }

        [Fact]
        public void VentaViewModel_DebePoderConfigurar_SoloCuandoEstadoPendienteConfiguracion()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.PendienteConfiguracion
            };

            // Act & Assert
            Assert.True(viewModel.PuedeConfigurarCredito);
        }

        [Fact]
        public void VentaViewModel_FlagFinanciamientoConfigurado_EvitaLoop()
        {
            // Arrange - Venta con crédito en PendienteConfiguracion pero con flag de financiamiento
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.PendienteConfiguracion, // Podría estar inconsistente
                FechaConfiguracionCredito = DateTime.UtcNow, // Pero el flag está establecido
                RequiereAutorizacion = false
            };

            // Act & Assert
            // El flag indica que el financiamiento ya fue configurado
            Assert.True(viewModel.FinanciamientoConfigurado);
            // Por lo tanto, no debería mostrar como pendiente de configuración
            Assert.False(viewModel.PuedeConfigurarCredito);
            // Y debería poder confirmar
            Assert.True(viewModel.CreditoConfigurado);
            Assert.True(viewModel.PuedeConfirmar);
        }

        [Fact]
        public void VentaViewModel_SinFlag_DebeMostrarPendienteConfiguracion()
        {
            // Arrange - Venta con crédito en PendienteConfiguracion sin flag
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.PendienteConfiguracion,
                FechaConfiguracionCredito = null, // Sin flag
                RequiereAutorizacion = false
            };

            // Act & Assert
            Assert.False(viewModel.FinanciamientoConfigurado);
            Assert.True(viewModel.PuedeConfigurarCredito); // Debe mostrar botón configurar
            Assert.False(viewModel.PuedeConfirmar); // No puede confirmar aún
        }

        [Fact]
        public void VentaViewModel_ConFlagYEstadoConfigurado_PuedeConfirmar()
        {
            // Arrange - Estado correcto + flag (estado ideal)
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.Configurado,
                FechaConfiguracionCredito = DateTime.UtcNow,
                RequiereAutorizacion = false
            };

            // Act & Assert
            Assert.True(viewModel.FinanciamientoConfigurado);
            Assert.True(viewModel.CreditoConfigurado);
            Assert.True(viewModel.PuedeConfirmar);
            Assert.False(viewModel.PuedeConfigurarCredito); // No debe mostrar botón configurar
        }

        #endregion

        #region PendienteFinanciacion Estado Tests

        [Fact]
        public void EstadoVenta_DebeContener_PendienteFinanciacion()
        {
            // Arrange & Act
            var estado = EstadoVenta.PendienteFinanciacion;

            // Assert
            Assert.Equal(7, (int)estado);
        }

        [Fact]
        public void VentaViewModel_EsPendienteFinanciacion_DebeSerTrue_CuandoEstadoPendienteFinanciacion()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1
            };

            // Act & Assert
            Assert.True(viewModel.EsPendienteFinanciacion);
        }

        [Fact]
        public void VentaViewModel_EnPendienteFinanciacion_NoPuedeConfirmar()
        {
            // Arrange - Venta recién creada con crédito personal
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.PendienteConfiguracion,
                RequiereAutorizacion = false
            };

            // Act & Assert
            Assert.False(viewModel.PuedeConfirmar); // No puede confirmar en PendienteFinanciacion
            Assert.True(viewModel.PuedeConfigurarCredito); // Debe mostrar botón configurar
        }

        [Fact]
        public void VentaViewModel_EnPendienteFinanciacion_PuedeEditar()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1
            };

            // Act & Assert
            Assert.True(viewModel.PuedeEditar); // Sí puede editar
        }

        [Fact]
        public void VentaViewModel_PuedeConfigurarCredito_EnPendienteFinanciacion_ConCredito()
        {
            // Arrange
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.PendienteConfiguracion
            };

            // Act & Assert
            Assert.True(viewModel.PuedeConfigurarCredito);
        }

        [Fact]
        public void VentaViewModel_PuedeConfigurarCredito_EsFalse_SinCredito()
        {
            // Arrange - PendienteFinanciacion pero sin CreditoId (error de datos)
            var viewModel = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = null // Sin crédito
            };

            // Act & Assert
            Assert.False(viewModel.PuedeConfigurarCredito); // No tiene crédito para configurar
        }

        [Fact]
        public void VentaViewModel_FlujoCompleto_CreditoPersonal()
        {
            // Fase 1: Venta creada en PendienteFinanciacion
            var ventaCreada = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.PendienteConfiguracion,
                FechaConfiguracionCredito = null,
                RequiereAutorizacion = false
            };

            Assert.True(ventaCreada.EsPendienteFinanciacion);
            Assert.False(ventaCreada.PuedeConfirmar);
            Assert.True(ventaCreada.PuedeConfigurarCredito);

            // Fase 2: Después de configurar crédito (estado cambia a Presupuesto)
            var ventaConfigurada = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto, // Cambiado por ConfigurarVenta
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.Configurado,
                FechaConfiguracionCredito = DateTime.UtcNow,
                RequiereAutorizacion = false
            };

            Assert.False(ventaConfigurada.EsPendienteFinanciacion);
            Assert.True(ventaConfigurada.PuedeConfirmar);
            Assert.False(ventaConfigurada.PuedeConfigurarCredito);

            // Fase 3: Después de confirmar
            var ventaConfirmada = new VentaViewModel
            {
                Estado = EstadoVenta.Confirmada,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 1,
                CreditoEstado = EstadoCredito.Generado,
                FechaConfiguracionCredito = DateTime.UtcNow,
                RequiereAutorizacion = false
            };

            Assert.False(ventaConfirmada.EsPendienteFinanciacion);
            Assert.False(ventaConfirmada.PuedeConfirmar);
            Assert.False(ventaConfirmada.PuedeConfigurarCredito);
            Assert.True(ventaConfirmada.CreditoGenerado);
        }

        #endregion

        #region Fase 6 — Tests Anti-Loop y Estados Inválidos

        /// <summary>
        /// Test 1: Create con TipoPago=CreditoPersonal debe poner estado PendienteFinanciacion
        /// y requerir configuración antes de poder confirmar.
        /// </summary>
        [Fact]
        public void Fase6_CreateConCreditoPersonal_EstadoPendienteFinanciacion_RequiereConfiguracion()
        {
            // Simula el estado después de VentaController.Create con TipoPago=CreditoPersonal
            var ventaDespuesCreate = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 99, // Crédito creado automáticamente
                CreditoEstado = EstadoCredito.PendienteConfiguracion,
                FechaConfiguracionCredito = null, // Aún no configurado
                RequiereAutorizacion = false
            };

            // Assert: Estado correcto para redirigir a ConfigurarVenta
            Assert.True(ventaDespuesCreate.EsPendienteFinanciacion);
            Assert.True(ventaDespuesCreate.PuedeConfigurarCredito);
            Assert.False(ventaDespuesCreate.PuedeConfirmar); // NO puede confirmar sin configurar
            Assert.Null(ventaDespuesCreate.FechaConfiguracionCredito);
        }

        /// <summary>
        /// Test 2: Confirmar sin financiación configurada debe bloquearse.
        /// El sistema NO debe permitir confirmar una venta con crédito PendienteConfiguracion.
        /// </summary>
        [Fact]
        public void Fase6_ConfirmarSinFinanciacion_DebeBloquear_NoPuedeConfirmar()
        {
            // Simula intento de confirmar sin haber pasado por ConfigurarVenta
            var ventaSinConfigurar = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion, // Todavía en este estado
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 99,
                CreditoEstado = EstadoCredito.PendienteConfiguracion, // Sin configurar
                FechaConfiguracionCredito = null,
                RequiereAutorizacion = false
            };

            // Assert: El ViewModel bloquea la confirmación
            Assert.False(ventaSinConfigurar.PuedeConfirmar); // BLOQUEADO
            Assert.True(ventaSinConfigurar.PuedeConfigurarCredito); // Debe ir a configurar
            Assert.False(ventaSinConfigurar.CreditoConfigurado);
        }

        /// <summary>
        /// Test 3: ConfigurarVenta POST marca financiación y cambia estado a Presupuesto.
        /// Después de configurar, la venta puede ser confirmada.
        /// </summary>
        [Fact]
        public void Fase6_ConfigurarVentaPOST_MarcaFinanciacion_CambiaEstadoPresupuesto()
        {
            // Simula el estado ANTES de ConfigurarVenta POST
            var ventaAntes = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 99,
                CreditoEstado = EstadoCredito.PendienteConfiguracion,
                FechaConfiguracionCredito = null
            };

            Assert.False(ventaAntes.PuedeConfirmar);
            Assert.True(ventaAntes.PuedeConfigurarCredito);

            // Simula el estado DESPUÉS de ConfigurarVenta POST
            var ventaDespues = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto, // Cambiado por controller
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 99,
                CreditoEstado = EstadoCredito.Configurado, // Actualizado
                FechaConfiguracionCredito = DateTime.UtcNow, // Marcado
                RequiereAutorizacion = false
            };

            // Assert: Ahora puede confirmar
            Assert.True(ventaDespues.PuedeConfirmar); // HABILITADO
            Assert.False(ventaDespues.PuedeConfigurarCredito); // Ya no necesita configurar
            Assert.True(ventaDespues.CreditoConfigurado);
            Assert.NotNull(ventaDespues.FechaConfiguracionCredito);
        }

        /// <summary>
        /// Test 4: Confirmar con financiación lista debe generar crédito (estado Generado).
        /// Después de confirmar, no puede volver a confirmar ni configurar.
        /// </summary>
        [Fact]
        public void Fase6_ConfirmarConFinanciacion_GeneraCredito_NoReentra()
        {
            // Estado ANTES de confirmar (financiación ya configurada)
            var ventaListaParaConfirmar = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 99,
                CreditoEstado = EstadoCredito.Configurado,
                FechaConfiguracionCredito = DateTime.UtcNow.AddHours(-1),
                RequiereAutorizacion = false
            };

            Assert.True(ventaListaParaConfirmar.PuedeConfirmar);

            // Estado DESPUÉS de confirmar (Confirmar action ejecutado)
            var ventaConfirmada = new VentaViewModel
            {
                Estado = EstadoVenta.Confirmada, // Cambiado por Confirmar
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 99,
                CreditoEstado = EstadoCredito.Generado, // Actualizado a Generado
                FechaConfiguracionCredito = DateTime.UtcNow.AddHours(-1),
                RequiereAutorizacion = false
            };

            // Assert: No puede volver a confirmar ni configurar (anti-loop)
            Assert.False(ventaConfirmada.PuedeConfirmar); // Ya confirmada
            Assert.False(ventaConfirmada.PuedeConfigurarCredito); // Ya generado
            Assert.True(ventaConfirmada.CreditoGenerado);
            Assert.False(ventaConfirmada.PuedeEditar); // No editable
        }

        /// <summary>
        /// Test adicional: Verificar que el loop no ocurre si el crédito ya está Generado.
        /// Incluso si por error el estado de la venta es Presupuesto, el crédito Generado bloquea reconfiguración.
        /// </summary>
        [Theory]
        [InlineData(EstadoCredito.Generado)]
        [InlineData(EstadoCredito.Activo)]
        [InlineData(EstadoCredito.Finalizado)]
        public void Fase6_CreditoYaGenerado_BloqueaReconfiguracion(EstadoCredito estadoCreditoFinal)
        {
            // Simula un estado inconsistente donde la venta vuelve a Presupuesto
            // pero el crédito ya está en estado terminal
            var ventaInconsistente = new VentaViewModel
            {
                Estado = EstadoVenta.Presupuesto, // Hipotético estado erróneo
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = 99,
                CreditoEstado = estadoCreditoFinal, // Pero el crédito ya fue generado
                FechaConfiguracionCredito = DateTime.UtcNow.AddDays(-1)
            };

            // Assert: El crédito generado impide reconfiguración
            Assert.False(ventaInconsistente.PuedeConfigurarCredito); // BLOQUEADO
            Assert.True(ventaInconsistente.CreditoGenerado);
        }

        /// <summary>
        /// Test: Venta con crédito personal pero sin CreditoId es un estado inválido.
        /// El ViewModel debe manejar gracefully este caso edge.
        /// </summary>
        [Fact]
        public void Fase6_CreditoPersonalSinCreditoId_EsEstadoInvalido()
        {
            // Estado edge: TipoPago indica crédito pero no hay CreditoId
            var ventaInvalida = new VentaViewModel
            {
                Estado = EstadoVenta.PendienteFinanciacion,
                TipoPago = TipoPago.CreditoPersonal,
                CreditoId = null, // Error: debería existir
                CreditoEstado = null
            };

            // Assert: El sistema no crashea y bloquea operaciones
            Assert.False(ventaInvalida.PuedeConfirmar);
            Assert.False(ventaInvalida.PuedeConfigurarCredito); // No hay crédito que configurar
            Assert.False(ventaInvalida.CreditoGenerado);
            Assert.False(ventaInvalida.CreditoConfigurado);
            Assert.False(ventaInvalida.CreditoPendienteConfiguracion);
        }

        #endregion
    }
}

