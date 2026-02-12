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
    /// Tests unitarios para la prevalidación de crédito personal (E1).
    /// Verifica que PrevalidarAsync retorne el resultado correcto sin persistir datos.
    /// </summary>
    public class PrevalidacionServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IClienteAptitudService> _mockAptitudService;
        private readonly Mock<ILogger<ValidacionVentaService>> _mockLogger;
        private readonly ValidacionVentaService _service;

        public PrevalidacionServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_Prevalidacion_{Guid.NewGuid()}")
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

        #endregion

        #region Tests: Cliente Aprobable

        [Fact]
        public async Task PrevalidarAsync_ClienteApto_SinMora_ConCupo_RetornaAprobable()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.Apto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle 
                    { 
                        Completa = true, 
                        Evaluada = true 
                    },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle 
                    { 
                        TieneMora = false, 
                        Evaluada = true 
                    }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(ResultadoPrevalidacion.Aprobable, resultado.Resultado);
            Assert.True(resultado.PermiteGuardar);
            Assert.Equal("success", resultado.ColorBadge);
            Assert.Empty(resultado.Motivos);
            Assert.Equal(100000m, resultado.LimiteCredito);
            Assert.Equal(100000m, resultado.CupoDisponible);
            Assert.False(resultado.TieneMora);
        }

        [Fact]
        public async Task PrevalidarAsync_ClienteApto_CupoExacto_RetornaAprobable()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 100000m; // Exactamente el cupo disponible

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.Apto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.Aprobable, resultado.Resultado);
            Assert.True(resultado.PermiteGuardar);
        }

        #endregion

        #region Tests: Requiere Autorización

        [Fact]
        public async Task PrevalidarAsync_ClienteConMoraNoBlockeante_RetornaRequiereAutorizacion()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;
            int diasMora = 15;

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.RequiereAutorizacion,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle 
                    { 
                        TieneMora = true, 
                        Evaluada = true,
                        DiasMaximoMora = diasMora,
                        RequiereAutorizacion = true,
                        EsBloqueante = false
                    }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.RequiereAutorizacion, resultado.Resultado);
            Assert.True(resultado.PermiteGuardar); // Permite guardar, pero requerirá autorización
            Assert.Equal("warning", resultado.ColorBadge);
            Assert.True(resultado.TieneMora);
            Assert.Equal(diasMora, resultado.DiasMora);
        }

        [Fact]
        public async Task PrevalidarAsync_MontoExcedeCupo_RetornaRequiereAutorizacion()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 150000m; // Excede el cupo de 100000

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.Apto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.RequiereAutorizacion, resultado.Resultado);
            Assert.True(resultado.PermiteGuardar); // Permite guardar, pero requerirá autorización
            Assert.Contains(resultado.Motivos, m => 
                m.Categoria == CategoriaMotivo.Cupo && !m.EsBloqueante);
        }

        #endregion

        #region Tests: No Viable

        [Fact]
        public async Task PrevalidarAsync_SinLimiteCredito_RetornaNoViable()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoApto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = false, 
                        LimiteCredito = 0,
                        CupoDisponible = 0, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true },
                    Detalles = new List<AptitudDetalleItem>
                    {
                        new() { Categoria = "Cupo", Descripcion = "Sin límite de crédito asignado", EsBloqueo = true }
                    }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(0m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.NoViable, resultado.Resultado);
            Assert.False(resultado.PermiteGuardar);
            Assert.Equal("danger", resultado.ColorBadge);
            Assert.Contains(resultado.Motivos, m => m.EsBloqueante);
        }

        [Fact]
        public async Task PrevalidarAsync_DocumentosFaltantes_RetornaNoViable()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;
            var docsFaltantes = new List<string> { "DNI Frente", "Recibo de Sueldo" };

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
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
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true },
                    Detalles = new List<AptitudDetalleItem>
                    {
                        new() { Categoria = "Documentación", Descripcion = "Faltan: DNI Frente, Recibo de Sueldo", EsBloqueo = true }
                    }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.NoViable, resultado.Resultado);
            Assert.False(resultado.PermiteGuardar);
            Assert.NotEmpty(resultado.DocumentosFaltantes);
            Assert.Contains("DNI Frente", resultado.DocumentosFaltantes);
            Assert.Contains("Recibo de Sueldo", resultado.DocumentosFaltantes);
        }

        [Fact]
        public async Task PrevalidarAsync_DocumentosVencidos_RetornaNoViable()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;
            var docsVencidos = new List<string> { "Recibo de Sueldo" };

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoApto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle 
                    { 
                        Completa = false, 
                        Evaluada = true,
                        DocumentosVencidos = docsVencidos
                    },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true },
                    Detalles = new List<AptitudDetalleItem>
                    {
                        new() { Categoria = "Documentación", Descripcion = "Vencidos: Recibo de Sueldo", EsBloqueo = true }
                    }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.NoViable, resultado.Resultado);
            Assert.False(resultado.PermiteGuardar);
            Assert.NotEmpty(resultado.DocumentosVencidos);
        }

        [Fact]
        public async Task PrevalidarAsync_MoraBloqueante_RetornaNoViable()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;
            int diasMora = 90; // Mora severa, bloqueante

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoApto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle 
                    { 
                        TieneMora = true, 
                        Evaluada = true,
                        DiasMaximoMora = diasMora,
                        RequiereAutorizacion = false,
                        EsBloqueante = true
                    },
                    Detalles = new List<AptitudDetalleItem>
                    {
                        new() { Categoria = "Mora", Descripcion = $"Mora crítica ({diasMora} días) - Bloquea operación", EsBloqueo = true }
                    }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.NoViable, resultado.Resultado);
            Assert.False(resultado.PermiteGuardar);
            Assert.True(resultado.TieneMora);
            Assert.Equal(diasMora, resultado.DiasMora);
        }

        [Fact]
        public async Task PrevalidarAsync_ClienteNoEvaluado_RetornaNoViable()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoEvaluado,
                    ConfiguracionCompleta = false,
                    Documentacion = new AptitudDocumentacionDetalle { Evaluada = false },
                    Cupo = new AptitudCupoDetalle { Evaluado = false },
                    Mora = new AptitudMoraDetalle { Evaluada = false }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(0m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.NoViable, resultado.Resultado);
            Assert.False(resultado.PermiteGuardar);
        }

        #endregion

        #region Tests: No persistencia

        [Fact]
        public async Task PrevalidarAsync_NoModificaBaseDeDatos()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;
            var changeTrackerEntriesAntes = _context.ChangeTracker.Entries().Count();

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.Apto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert - No se agregaron, modificaron ni eliminaron entidades
            var changeTrackerEntriesDespues = _context.ChangeTracker.Entries()
                .Count(e => e.State == EntityState.Added || 
                            e.State == EntityState.Modified || 
                            e.State == EntityState.Deleted);
            Assert.Equal(0, changeTrackerEntriesDespues);
        }

        #endregion

        #region Tests: Timestamp

        [Fact]
        public async Task PrevalidarAsync_IncluyeTimestamp()
        {
            // Arrange
            var cliente = CrearClienteTest();
            var antesDeEjecutar = DateTime.UtcNow.AddSeconds(-1); // Margen de tolerancia

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.Apto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, 50000m);

            // Assert - Verificar que el timestamp es reciente (dentro de los últimos segundos)
            Assert.True(resultado.Timestamp >= antesDeEjecutar);
            Assert.True(resultado.Timestamp <= DateTime.UtcNow.AddSeconds(1));
        }

        #endregion

        #region Tests de Titulo en Motivos

        [Fact]
        public async Task PrevalidarAsync_SinLimite_MotivoTieneTituloCorrecto()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoEvaluado,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = false, 
                        CupoDisponible = 0, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(0m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.NoViable, resultado.Resultado);
            var motivoCupo = resultado.Motivos.FirstOrDefault(m => m.Categoria == CategoriaMotivo.Cupo);
            Assert.NotNull(motivoCupo);
            Assert.Equal("Sin límite de crédito", motivoCupo!.Titulo);
            Assert.True(motivoCupo!.EsBloqueante);
        }

        [Fact]
        public async Task PrevalidarAsync_CupoInsuficiente_MotivoTieneTituloYDescripcionCorrectos()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 150000m; // Excede cupo disponible

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.Apto,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle { Completa = true, Evaluada = true },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true, 
                        LimiteCredito = 100000m,
                        CupoDisponible = 80000m, // Menos que el monto solicitado
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(80000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.RequiereAutorizacion, resultado.Resultado);
            var motivoCupo = resultado.Motivos.FirstOrDefault(m => m.Categoria == CategoriaMotivo.Cupo);
            Assert.NotNull(motivoCupo);
            Assert.Equal("Cupo insuficiente", motivoCupo!.Titulo);
            Assert.Contains("150.000", motivoCupo!.Descripcion); // Monto solicitado
            Assert.Contains("80.000", motivoCupo!.Descripcion);  // Cupo disponible
            Assert.False(motivoCupo!.EsBloqueante); // Requiere autorización, no bloqueante
        }

        [Fact]
        public async Task PrevalidarAsync_DocumentosFaltantes_MotivoTieneTituloCorrecto()
        {
            // Arrange
            var cliente = CrearClienteTest();
            decimal montoSolicitado = 50000m;
            var docsFaltantes = new List<string> { "DNI Frente" };

            _mockAptitudService.Setup(x => x.EvaluarAptitudSinGuardarAsync(cliente.Id))
                .ReturnsAsync(new AptitudCrediticiaViewModel
                {
                    Estado = EstadoCrediticioCliente.NoEvaluado,
                    ConfiguracionCompleta = true,
                    Documentacion = new AptitudDocumentacionDetalle 
                    { 
                        Completa = false, 
                        Evaluada = true,
                        DocumentosFaltantes = docsFaltantes
                    },
                    Cupo = new AptitudCupoDetalle 
                    { 
                        TieneCupoAsignado = true,
                        LimiteCredito = 100000m,
                        CupoDisponible = 100000m, 
                        Evaluado = true 
                    },
                    Mora = new AptitudMoraDetalle { TieneMora = false, Evaluada = true }
                });

            _mockAptitudService.Setup(x => x.GetCupoDisponibleAsync(cliente.Id))
                .ReturnsAsync(100000m);

            // Act
            var resultado = await _service.PrevalidarAsync(cliente.Id, montoSolicitado);

            // Assert
            Assert.Equal(ResultadoPrevalidacion.NoViable, resultado.Resultado);
            var motivoDoc = resultado.Motivos.FirstOrDefault(m => m.Categoria == CategoriaMotivo.Documentacion);
            Assert.NotNull(motivoDoc);
            Assert.Equal("Documentación incompleta", motivoDoc!.Titulo);
            Assert.Contains("DNI Frente", motivoDoc!.Descripcion);
            Assert.True(motivoDoc!.EsBloqueante);
        }

        #endregion
    }
}

