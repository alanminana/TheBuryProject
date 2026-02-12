using Moq;
using Microsoft.Extensions.Logging;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TheBuryProject.Tests.ValidacionVenta
{
    /// <summary>
    /// Tests de flujo para validación unificada de ventas con crédito personal.
    /// Verifica que las ventas se marquen correctamente como PendienteRequisitos o RequiereAutorizacion.
    /// </summary>
    public class ValidacionVentaServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IClienteAptitudService> _mockAptitudService;
        private readonly Mock<ILogger<ValidacionVentaService>> _mockLogger;
        private readonly ValidacionVentaService _service;

        public ValidacionVentaServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _mockAptitudService = new Mock<IClienteAptitudService>();
            _mockLogger = new Mock<ILogger<ValidacionVentaService>>();

            _service = new ValidacionVentaService(
                _context,
                _mockAptitudService.Object,
                _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Helpers

        private Cliente CrearClienteTest(int id = 1)
        {
            var cliente = new Cliente
            {
                Id = id,
                Nombre = "Cliente Test",
                Apellido = "Apellido Test",
                TipoDocumento = "DNI",
                NumeroDocumento = "12345678",
                Email = "test@test.com",
                Telefono = "1234567890",
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            };
            _context.Clientes.Add(cliente);
            _context.SaveChanges();
            return cliente;
        }

        private void ConfigurarAptitudApto()
        {
            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(It.IsAny<int>()))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.Apto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle { TieneCupoAsignado = true, CupoDisponible = 100000, Evaluado = true },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(It.IsAny<int>()))
                .ReturnsAsync(100000m);
        }

        private void ConfigurarAptitudDocsFaltantes(List<string> docsFaltantes)
        {
            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(It.IsAny<int>()))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoApto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle 
                    { 
                        Completa = false, 
                        Evaluada = true,
                        DocumentosFaltantes = docsFaltantes
                    },
                    Cupo = new AptitudCupoDetalle { TieneCupoAsignado = true, CupoDisponible = 100000, Evaluado = true },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true },
                    Detalles = new List<AptitudDetalleItem>
                    {
                        new() { Categoria = "Documentación", Descripcion = $"Faltan: {string.Join(", ", docsFaltantes)}", EsBloqueo = true }
                    }
                });
        }

        private void ConfigurarAptitudSinCupo()
        {
            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(It.IsAny<int>()))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoApto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = false, 
                        CupoDisponible = 0, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true },
                    Detalles = new List<AptitudDetalleItem>
                    {
                        new() { Categoria = "Cupo", Descripcion = "Sin límite de crédito asignado", EsBloqueo = true }
                    }
                });
        }

        private void ConfigurarAptitudConMora(int diasMora)
        {
            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(It.IsAny<int>()))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.RequiereAutorizacion,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle { TieneCupoAsignado = true, CupoDisponible = 100000, Evaluado = true },
                    Mora = new AptitudMoraDetalle 
                    { 
                        TieneMora = true, 
                        Evaluada = true,
                        DiasMaximoMora = diasMora,
                        RequiereAutorizacion = true,
                        EsBloqueante = false
                    },
                    Detalles = new List<AptitudDetalleItem>
                    {
                        new() 
                        { 
                            Categoria = "Mora", 
                            Descripcion = $"Cliente en mora ({diasMora} días) - Requiere autorización", 
                            EsBloqueo = false 
                        }
                    }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(It.IsAny<int>()))
                .ReturnsAsync(100000m);
        }

        private void ConfigurarAptitudNoEvaluado()
        {
            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(It.IsAny<int>()))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoEvaluado,
                    ConfiguracionCompleta = false,
                    AdvertenciaConfiguracion = "El sistema de crédito no está configurado"
                });
        }

        #endregion

        #region Tests: Cliente Apto

        [Fact]
        public async Task ValidarVenta_ClienteApto_MontoDisponible_PuedeProceeder()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudApto();

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m);

            // Assert
            Assert.True(resultado.PuedeProceeder);
            Assert.False(resultado.RequiereAutorizacion);
            Assert.False(resultado.PendienteRequisitos);
            Assert.Empty(resultado.RazonesAutorizacion);
            Assert.Empty(resultado.RequisitosPendientes);
            Assert.Equal(EstadoCrediticioCliente.Apto, resultado.EstadoAptitud);
        }

        [Fact]
        public async Task ValidarVenta_ClienteApto_MontoExcedeCupo_RequiereAutorizacion()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudApto();

            // Act - Monto mayor que cupo disponible (100000)
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 150000m);

            // Assert
            Assert.False(resultado.PuedeProceeder);
            Assert.True(resultado.RequiereAutorizacion);
            Assert.False(resultado.PendienteRequisitos);
            Assert.Contains(resultado.RazonesAutorizacion, r => r.Tipo == TipoRazonAutorizacion.ExcedeCupo);
        }

        #endregion

        #region Tests: Documentación Faltante

        [Fact]
        public async Task ValidarVenta_DocumentacionFaltante_PendienteRequisitos()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudDocsFaltantes(new List<string> { "DNI", "Recibo de Sueldo" });

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m);

            // Assert
            Assert.False(resultado.PuedeProceeder);
            Assert.False(resultado.RequiereAutorizacion);
            Assert.True(resultado.PendienteRequisitos);
            Assert.Contains(resultado.RequisitosPendientes, r => r.Tipo == TipoRequisitoPendiente.DocumentacionFaltante);
            Assert.Equal(EstadoCrediticioCliente.NoApto, resultado.EstadoAptitud);
        }

        [Fact]
        public async Task ValidarVenta_DocumentacionFaltante_MensajeIncluye_Faltantes()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudDocsFaltantes(new List<string> { "DNI", "Recibo de Sueldo" });

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m);

            // Assert
            Assert.Contains("DNI", resultado.MensajeResumen);
            Assert.Contains("Recibo de Sueldo", resultado.MensajeResumen);
        }

        #endregion

        #region Tests: Sin Límite de Crédito

        [Fact]
        public async Task ValidarVenta_SinLimiteCredito_PendienteRequisitos()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudSinCupo();

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m);

            // Assert
            Assert.False(resultado.PuedeProceeder);
            Assert.True(resultado.PendienteRequisitos);
            Assert.Contains(resultado.RequisitosPendientes, r => r.Tipo == TipoRequisitoPendiente.SinLimiteCredito);
        }

        #endregion

        #region Tests: Cliente con Mora

        [Fact]
        public async Task ValidarVenta_ClienteConMora_RequiereAutorizacion()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudConMora(diasMora: 15);

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m);

            // Assert
            Assert.False(resultado.PuedeProceeder);
            Assert.True(resultado.RequiereAutorizacion);
            Assert.False(resultado.PendienteRequisitos);
            Assert.Contains(resultado.RazonesAutorizacion, r => r.Tipo == TipoRazonAutorizacion.MoraActiva);
            Assert.Equal(EstadoCrediticioCliente.RequiereAutorizacion, resultado.EstadoAptitud);
        }

        [Fact]
        public async Task ValidarVenta_ClienteConMora_DetalleIncluyeDias()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudConMora(diasMora: 30);

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m);

            // Assert
            var razonMora = resultado.RazonesAutorizacion.FirstOrDefault(r => r.Tipo == TipoRazonAutorizacion.MoraActiva);
            Assert.NotNull(razonMora);
            Assert.Contains("mora", razonMora!.DetalleAdicional?.ToLower() ?? "");
        }

        #endregion

        #region Tests: Sistema No Configurado

        [Fact]
        public async Task ValidarVenta_SistemaNoConfigurado_PendienteRequisitos()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudNoEvaluado();

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m);

            // Assert
            Assert.False(resultado.PuedeProceeder);
            Assert.True(resultado.PendienteRequisitos);
            Assert.Contains(resultado.RequisitosPendientes, r => r.Tipo == TipoRequisitoPendiente.SinEvaluacionCrediticia);
        }

        #endregion

        #region Tests: Estado de Autorización Sugerido

        [Fact]
        public async Task ValidarVenta_NoRequiereAutorizacion_EstadoNoRequiere()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudApto();

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m);

            // Assert
            Assert.Equal(EstadoAutorizacionVenta.NoRequiere, resultado.EstadoAutorizacionSugerido);
        }

        [Fact]
        public async Task ValidarVenta_RequiereAutorizacion_EstadoPendiente()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudConMora(diasMora: 10);

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m);

            // Assert
            Assert.Equal(EstadoAutorizacionVenta.PendienteAutorizacion, resultado.EstadoAutorizacionSugerido);
        }

        #endregion

        #region Tests: Resumen Crediticio

        [Fact]
        public async Task ObtenerResumenCrediticio_ClienteApto_RetornaInfoCompleta()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudApto();

            // Act
            var resumen = await _service.ObtenerResumenCrediticioAsync(cliente.Id);

            // Assert
            Assert.Contains("Apto", resumen.EstadoAptitud);
            Assert.Equal("success", resumen.ColorSemaforo);
            Assert.True(resumen.DocumentacionCompleta);
            Assert.False(resumen.TieneMoraActiva);
            Assert.True(resumen.PuedeRecibirCredito);
        }

        [Fact]
        public async Task ObtenerResumenCrediticio_ClienteNoApto_MensajeAdvertencia()
        {
            // Arrange
            var cliente = CrearClienteTest();
            ConfigurarAptitudDocsFaltantes(new List<string> { "DNI" });
            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(It.IsAny<int>()))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoApto,
                    Motivo = "Documentación incompleta",
                    ConfiguracionCompleta = true
                });

            // Act
            var resumen = await _service.ObtenerResumenCrediticioAsync(cliente.Id);

            // Assert
            Assert.NotNull(resumen.MensajeAdvertencia);
            Assert.False(resumen.PuedeRecibirCredito);
        }

        #endregion

        #region Tests: Validación con Crédito Específico

        [Fact]
        public async Task ValidarVenta_CreditoEspecifico_SaldoSuficiente_PuedeProceeder()
        {
            // Arrange
            var cliente = CrearClienteTest();
            var credito = new Credito
            {
                ClienteId = cliente.Id,
                Numero = "C001",
                MontoAprobado = 100000,
                SaldoPendiente = 80000,
                Estado = EstadoCredito.Activo,
                TasaInteres = 2.5m,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            };
            _context.Creditos.Add(credito);
            _context.SaveChanges();

            ConfigurarAptitudApto();

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m, credito.Id);

            // Assert
            Assert.True(resultado.PuedeProceeder);
        }

        [Fact]
        public async Task ValidarVenta_CreditoEspecifico_SaldoInsuficiente_RequiereAutorizacion()
        {
            // Arrange
            var cliente = CrearClienteTest();
            var credito = new Credito
            {
                ClienteId = cliente.Id,
                Numero = "C001",
                MontoAprobado = 100000,
                SaldoPendiente = 30000,
                Estado = EstadoCredito.Activo,
                TasaInteres = 2.5m,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            };
            _context.Creditos.Add(credito);
            _context.SaveChanges();

            ConfigurarAptitudApto();

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m, credito.Id);

            // Assert
            Assert.True(resultado.RequiereAutorizacion);
            Assert.Contains(resultado.RazonesAutorizacion, r => r.Tipo == TipoRazonAutorizacion.ExcedeCupo);
        }

        [Fact]
        public async Task ValidarVenta_CreditoNoActivo_PendienteRequisitos()
        {
            // Arrange
            var cliente = CrearClienteTest();
            var credito = new Credito
            {
                ClienteId = cliente.Id,
                Numero = "C001",
                MontoAprobado = 100000,
                SaldoPendiente = 80000,
                Estado = EstadoCredito.Solicitado, // No activo
                TasaInteres = 2.5m,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            };
            _context.Creditos.Add(credito);
            _context.SaveChanges();

            ConfigurarAptitudApto();

            // Act
            var resultado = await _service.ValidarVentaCreditoPersonalAsync(cliente.Id, 50000m, credito.Id);

            // Assert
            Assert.True(resultado.PendienteRequisitos);
            Assert.Contains(resultado.RequisitosPendientes, r => r.Tipo == TipoRequisitoPendiente.SinCreditoAprobado);
        }

        #endregion
    }
}
