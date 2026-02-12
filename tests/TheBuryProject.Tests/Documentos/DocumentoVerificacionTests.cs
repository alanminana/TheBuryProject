using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;
using Moq;

namespace TheBuryProject.Tests.Documentos;

/// <summary>
/// Tests para la funcionalidad de verificacion de documentos.
/// Cubre: VerificarAsync, RechazarAsync, VerificarTodosAsync
/// </summary>
public class DocumentoVerificacionTests
{
    private DocumentoClienteService CreateService(SqliteInMemoryDb db)
    {
        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.WebRootPath).Returns(".");
        environment.Setup(e => e.ContentRootPath).Returns(".");

        return new DocumentoClienteService(
            db.Context,
            mapper,
            NullLogger<DocumentoClienteService>.Instance,
            environment.Object);
    }

    private async Task<(Cliente cliente, List<DocumentoCliente> documentos)> SetupClienteConDocumentosAsync(
        SqliteInMemoryDb db, int cantidadPendientes, int cantidadVerificados = 0)
    {
        var cliente = new Cliente
        {
            TipoDocumento = "DNI",
            NumeroDocumento = "12345678",
            Apellido = "Perez",
            Nombre = "Juan",
            Telefono = "123",
            Domicilio = "Calle 123",
            Activo = true
        };
        db.Context.Clientes.Add(cliente);
        await db.Context.SaveChangesAsync();

        var documentos = new List<DocumentoCliente>();

        // Crear documentos pendientes
        for (int i = 0; i < cantidadPendientes; i++)
        {
            var doc = new DocumentoCliente
            {
                ClienteId = cliente.Id,
                TipoDocumento = (TipoDocumentoCliente)(i % 4), // Rotar tipos
                NombreArchivo = $"documento_pendiente_{i}.pdf",
                RutaArchivo = $"/uploads/{cliente.Id}/doc{i}.pdf",
                FechaSubida = DateTime.Now.AddDays(-i),
                Estado = EstadoDocumento.Pendiente,
                TipoMIME = "application/pdf",
                TamanoBytes = 1024
            };
            documentos.Add(doc);
            db.Context.Set<DocumentoCliente>().Add(doc);
        }

        // Crear documentos ya verificados
        for (int i = 0; i < cantidadVerificados; i++)
        {
            var doc = new DocumentoCliente
            {
                ClienteId = cliente.Id,
                TipoDocumento = TipoDocumentoCliente.Otro,
                NombreArchivo = $"documento_verificado_{i}.pdf",
                RutaArchivo = $"/uploads/{cliente.Id}/verificado{i}.pdf",
                FechaSubida = DateTime.Now.AddDays(-10 - i),
                FechaVerificacion = DateTime.Now.AddDays(-5),
                Estado = EstadoDocumento.Verificado,
                VerificadoPor = "admin",
                TipoMIME = "application/pdf",
                TamanoBytes = 1024
            };
            documentos.Add(doc);
            db.Context.Set<DocumentoCliente>().Add(doc);
        }

        await db.Context.SaveChangesAsync();
        return (cliente, documentos);
    }

    #region VerificarAsync Tests

    [Fact]
    public async Task VerificarAsync_DocumentoPendiente_CambiaEstadoAVerificado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 1);
        var documento = documentos[0];

        // Act
        var resultado = await service.VerificarAsync(documento.Id, "admin", "Todo correcto");

        // Assert
        Assert.True(resultado);
        var docActualizado = await db.Context.Set<DocumentoCliente>().FindAsync(documento.Id);
        Assert.NotNull(docActualizado);
        Assert.Equal(EstadoDocumento.Verificado, docActualizado!.Estado);
        Assert.Equal("admin", docActualizado!.VerificadoPor);
        Assert.NotNull(docActualizado!.FechaVerificacion);
        Assert.Equal("Todo correcto", docActualizado!.Observaciones);
    }

    [Fact]
    public async Task VerificarAsync_DocumentoNoExiste_RetornaFalse()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);

        // Act
        var resultado = await service.VerificarAsync(9999, "admin");

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task VerificarAsync_SinObservaciones_VerificaSinObservaciones()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 1);
        var documento = documentos[0];

        // Act
        var resultado = await service.VerificarAsync(documento.Id, "admin");

        // Assert
        Assert.True(resultado);
        var docActualizado = await db.Context.Set<DocumentoCliente>().FindAsync(documento.Id);
        Assert.NotNull(docActualizado);
        Assert.Equal(EstadoDocumento.Verificado, docActualizado!.Estado);
        Assert.Null(docActualizado!.Observaciones);
    }

    #endregion

    #region RechazarAsync Tests

    [Fact]
    public async Task RechazarAsync_DocumentoPendiente_CambiaEstadoARechazado()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 1);
        var documento = documentos[0];

        // Act
        var resultado = await service.RechazarAsync(documento.Id, "Documento ilegible", "admin");

        // Assert
        Assert.True(resultado);
        var docActualizado = await db.Context.Set<DocumentoCliente>().FindAsync(documento.Id);
        Assert.NotNull(docActualizado);
        Assert.Equal(EstadoDocumento.Rechazado, docActualizado!.Estado);
        Assert.Equal("admin", docActualizado!.VerificadoPor);
        Assert.Equal("Documento ilegible", docActualizado!.MotivoRechazo);
        Assert.NotNull(docActualizado!.FechaVerificacion);
    }

    [Fact]
    public async Task RechazarAsync_DocumentoNoExiste_RetornaFalse()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);

        // Act
        var resultado = await service.RechazarAsync(9999, "Motivo", "admin");

        // Assert
        Assert.False(resultado);
    }

    #endregion

    #region VerificarTodosAsync Tests

    [Fact]
    public async Task VerificarTodosAsync_ConDocumentosPendientes_VerificaTodos()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 3);

        // Act
        var resultado = await service.VerificarTodosAsync(cliente.Id, "supervisor", "Verificacion masiva");

        // Assert
        Assert.Equal(3, resultado);

        // Verificar que todos los documentos estan verificados
        var docsActualizados = db.Context.Set<DocumentoCliente>()
            .Where(d => d.ClienteId == cliente.Id && !d.IsDeleted)
            .ToList();

        Assert.All(docsActualizados, doc =>
        {
            Assert.Equal(EstadoDocumento.Verificado, doc.Estado);
            Assert.Equal("supervisor", doc.VerificadoPor);
            Assert.Equal("Verificacion masiva", doc.Observaciones);
            Assert.NotNull(doc.FechaVerificacion);
        });
    }

    [Fact]
    public async Task VerificarTodosAsync_SinDocumentosPendientes_RetornaCero()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 0, 2); // Solo verificados

        // Act
        var resultado = await service.VerificarTodosAsync(cliente.Id, "supervisor");

        // Assert
        Assert.Equal(0, resultado);
    }

    [Fact]
    public async Task VerificarTodosAsync_ConMezclaDeEstados_SoloVerificaPendientes()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 2, 3); // 2 pendientes, 3 verificados

        // Act
        var resultado = await service.VerificarTodosAsync(cliente.Id, "supervisor");

        // Assert
        Assert.Equal(2, resultado); // Solo los 2 pendientes

        var docsVerificados = db.Context.Set<DocumentoCliente>()
            .Count(d => d.ClienteId == cliente.Id && d.Estado == EstadoDocumento.Verificado && !d.IsDeleted);

        Assert.Equal(5, docsVerificados); // 2 recien verificados + 3 que ya estaban
    }

    [Fact]
    public async Task VerificarTodosAsync_ClienteInexistente_RetornaCero()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);

        // Act
        var resultado = await service.VerificarTodosAsync(9999, "supervisor");

        // Assert
        Assert.Equal(0, resultado);
    }

    [Fact]
    public async Task VerificarTodosAsync_SinObservaciones_VerificaSinObservaciones()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 2);

        // Act
        var resultado = await service.VerificarTodosAsync(cliente.Id, "supervisor");

        // Assert
        Assert.Equal(2, resultado);

        var docsActualizados = db.Context.Set<DocumentoCliente>()
            .Where(d => d.ClienteId == cliente.Id && !d.IsDeleted)
            .ToList();

        Assert.All(docsActualizados, doc =>
        {
            Assert.Null(doc.Observaciones);
        });
    }

    [Fact]
    public async Task VerificarTodosAsync_NoAfectaDocumentosEliminados()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 2);

        // Marcar uno como eliminado
        documentos[0].IsDeleted = true;
        await db.Context.SaveChangesAsync();

        // Act
        var resultado = await service.VerificarTodosAsync(cliente.Id, "supervisor");

        // Assert
        Assert.Equal(1, resultado); // Solo 1, el otro esta eliminado

        var docEliminado = await db.Context.Set<DocumentoCliente>().FindAsync(documentos[0].Id);
        Assert.NotNull(docEliminado);
        Assert.Equal(EstadoDocumento.Pendiente, docEliminado!.Estado); // Sigue pendiente porque esta eliminado
    }

    #endregion

    #region VerificarBatchAsync Tests

    [Fact]
    public async Task VerificarBatchAsync_DocumentosPendientes_VerificaTodos()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 3);
        var idsPendientes = documentos.Where(d => d.Estado == EstadoDocumento.Pendiente).Select(d => d.Id).ToList();

        // Act
        var resultado = await service.VerificarBatchAsync(idsPendientes, "supervisor", "Verificacion masiva");

        // Assert
        Assert.Equal(3, resultado.Exitosos);
        Assert.Equal(0, resultado.Fallidos);
        Assert.Empty(resultado.Errores);

        var docsActualizados = db.Context.Set<DocumentoCliente>()
            .Where(d => idsPendientes.Contains(d.Id))
            .ToList();

        Assert.All(docsActualizados, doc =>
        {
            Assert.Equal(EstadoDocumento.Verificado, doc.Estado);
            Assert.Equal("supervisor", doc.VerificadoPor);
            Assert.Equal("Verificacion masiva", doc.Observaciones);
        });
    }

    [Fact]
    public async Task VerificarBatchAsync_AlgunosNoExisten_RetornaErroresParciales()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 2);
        var idsValidos = documentos.Select(d => d.Id).ToList();
        var idsMezclados = new List<int> { idsValidos[0], 9999, idsValidos[1], 8888 };

        // Act
        var resultado = await service.VerificarBatchAsync(idsMezclados, "supervisor");

        // Assert
        Assert.Equal(2, resultado.Exitosos);
        Assert.Equal(2, resultado.Fallidos);
        Assert.Equal(2, resultado.Errores.Count);
        Assert.Contains(resultado.Errores, e => e.Id == 9999);
        Assert.Contains(resultado.Errores, e => e.Id == 8888);
    }

    [Fact]
    public async Task VerificarBatchAsync_DocumentosNoEnEstadoPendiente_RetornaErrores()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 1, 2); // 1 pendiente, 2 verificados
        var todosIds = documentos.Select(d => d.Id).ToList();

        // Act
        var resultado = await service.VerificarBatchAsync(todosIds, "supervisor");

        // Assert
        Assert.Equal(1, resultado.Exitosos); // Solo el pendiente
        Assert.Equal(2, resultado.Fallidos); // Los ya verificados
        Assert.Equal(2, resultado.Errores.Count);
        Assert.All(resultado.Errores, e => Assert.Contains("estado actual", e.Mensaje));
    }

    [Fact]
    public async Task VerificarBatchAsync_ListaVacia_RetornaResultadoVacio()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);

        // Act
        var resultado = await service.VerificarBatchAsync(new List<int>(), "supervisor");

        // Assert
        Assert.Equal(0, resultado.Exitosos);
        Assert.Equal(0, resultado.Fallidos);
        Assert.Empty(resultado.Errores);
    }

    #endregion

    #region RechazarBatchAsync Tests

    [Fact]
    public async Task RechazarBatchAsync_DocumentosPendientes_RechazaTodos()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 3);
        var idsPendientes = documentos.Where(d => d.Estado == EstadoDocumento.Pendiente).Select(d => d.Id).ToList();
        var motivo = "Documentos ilegibles";

        // Act
        var resultado = await service.RechazarBatchAsync(idsPendientes, motivo, "supervisor");

        // Assert
        Assert.Equal(3, resultado.Exitosos);
        Assert.Equal(0, resultado.Fallidos);
        Assert.Empty(resultado.Errores);

        var docsActualizados = db.Context.Set<DocumentoCliente>()
            .Where(d => idsPendientes.Contains(d.Id))
            .ToList();

        Assert.All(docsActualizados, doc =>
        {
            Assert.Equal(EstadoDocumento.Rechazado, doc.Estado);
            Assert.Equal("supervisor", doc.VerificadoPor);
            Assert.Equal(motivo, doc.MotivoRechazo);
        });
    }

    [Fact]
    public async Task RechazarBatchAsync_SinMotivo_RetornaErrores()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 2);
        var ids = documentos.Select(d => d.Id).ToList();

        // Act
        var resultado = await service.RechazarBatchAsync(ids, "", "supervisor");

        // Assert
        Assert.Equal(0, resultado.Exitosos);
        Assert.Equal(2, resultado.Fallidos);
        Assert.All(resultado.Errores, e => Assert.Contains("motivo del rechazo", e.Mensaje));
    }

    [Fact]
    public async Task RechazarBatchAsync_AlgunosNoEnEstadoPendiente_RetornaErroresParciales()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);
        var (cliente, documentos) = await SetupClienteConDocumentosAsync(db, 2, 1); // 2 pendientes, 1 verificado
        var todosIds = documentos.Select(d => d.Id).ToList();

        // Act
        var resultado = await service.RechazarBatchAsync(todosIds, "Motivo test", "supervisor");

        // Assert
        Assert.Equal(2, resultado.Exitosos); // Los 2 pendientes
        Assert.Equal(1, resultado.Fallidos); // El verificado
        Assert.Single(resultado.Errores);
        Assert.Contains("estado actual", resultado.Errores[0].Mensaje);
    }

    [Fact]
    public async Task RechazarBatchAsync_ListaVacia_RetornaResultadoVacio()
    {
        // Arrange
        using var db = new SqliteInMemoryDb("TestUser");
        var service = CreateService(db);

        // Act
        var resultado = await service.RechazarBatchAsync(new List<int>(), "Motivo test", "supervisor");

        // Assert
        Assert.Equal(0, resultado.Exitosos);
        Assert.Equal(0, resultado.Fallidos);
        Assert.Empty(resultado.Errores);
    }

    #endregion
}

