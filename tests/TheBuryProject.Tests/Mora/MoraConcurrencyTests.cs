using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Mora;

public class MoraConcurrencyTests
{
    private static IMapper CreateMapper()
    {
        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        return mapperConfig.CreateMapper();
    }

    [Fact]
    public async Task ResolverAlertaAsync_con_RowVersion_viejo_lanza_conflicto_y_no_resuelve_en_db()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var cliente = new Cliente
        {
            TipoDocumento = "DNI",
            NumeroDocumento = "123",
            Apellido = "Test",
            Nombre = "Cliente",
            Telefono = "000",
            Domicilio = "Calle 1",
            Activo = true
        };
        db.Context.Clientes.Add(cliente);
        await db.Context.SaveChangesAsync();

        var credito = new Credito
        {
            ClienteId = cliente.Id,
            Cliente = cliente,
            Numero = "CR-1",
            MontoSolicitado = 100,
            MontoAprobado = 100,
            TasaInteres = 0,
            CantidadCuotas = 1,
            MontoCuota = 100,
            CFTEA = 0,
            TotalAPagar = 100,
            SaldoPendiente = 100,
            Estado = EstadoCredito.Aprobado,
            PuntajeRiesgoInicial = 5
        };
        db.Context.Creditos.Add(credito);
        await db.Context.SaveChangesAsync();

        var alerta = new AlertaCobranza
        {
            ClienteId = cliente.Id,
            CreditoId = credito.Id,
            Tipo = TipoAlertaCobranza.CuotaVencida,
            Prioridad = PrioridadAlerta.Alta,
            Mensaje = "Test",
            MontoVencido = 100m,
            CuotasVencidas = 1,
            FechaAlerta = DateTime.UtcNow.AddDays(-1),
            Resuelta = false,
            RowVersion = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 }
        };
        db.Context.AlertasCobranza.Add(alerta);
        await db.Context.SaveChangesAsync();

        var rowVersionViejo = alerta.RowVersion;
        Assert.NotNull(rowVersionViejo);
        Assert.NotEmpty(rowVersionViejo);

        await using (var ctx2 = db.CreateNewContext())
        {
            var alertaOtraSesion = await ctx2.AlertasCobranza.SingleAsync(a => a.Id == alerta.Id);
            alertaOtraSesion.Observaciones = "Cambio por otro usuario";
            alertaOtraSesion.RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };
            await ctx2.SaveChangesAsync();
        }

        var service = new MoraService(db.Context, CreateMapper(), NullLogger<MoraService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.ResolverAlertaAsync(alerta.Id, "ok", rowVersionViejo));

        Assert.Contains("modificada por otro usuario", ex.Message, StringComparison.OrdinalIgnoreCase);

        await using var ctx3 = db.CreateNewContext();
        var alertaDb = await ctx3.AlertasCobranza.AsNoTracking().SingleAsync(a => a.Id == alerta.Id);
        Assert.False(alertaDb.Resuelta);
        Assert.Null(alertaDb.FechaResolucion);
    }

    [Fact]
    public async Task MarcarAlertaComoLeidaAsync_con_RowVersion_viejo_lanza_conflicto_y_no_persiste_cambio()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var cliente = new Cliente
        {
            TipoDocumento = "DNI",
            NumeroDocumento = "456",
            Apellido = "Test",
            Nombre = "Cliente",
            Telefono = "000",
            Domicilio = "Calle 1",
            Activo = true
        };
        db.Context.Clientes.Add(cliente);
        await db.Context.SaveChangesAsync();

        var credito = new Credito
        {
            ClienteId = cliente.Id,
            Cliente = cliente,
            Numero = "CR-2",
            MontoSolicitado = 100,
            MontoAprobado = 100,
            TasaInteres = 0,
            CantidadCuotas = 1,
            MontoCuota = 100,
            CFTEA = 0,
            TotalAPagar = 100,
            SaldoPendiente = 100,
            Estado = EstadoCredito.Aprobado,
            PuntajeRiesgoInicial = 5
        };
        db.Context.Creditos.Add(credito);
        await db.Context.SaveChangesAsync();

        var alerta = new AlertaCobranza
        {
            ClienteId = cliente.Id,
            CreditoId = credito.Id,
            Tipo = TipoAlertaCobranza.ProximoVencimiento,
            Prioridad = PrioridadAlerta.Media,
            Mensaje = "Test",
            MontoVencido = 50m,
            CuotasVencidas = 0,
            FechaAlerta = DateTime.UtcNow.AddDays(-1),
            Resuelta = false,
            RowVersion = new byte[] { 2, 2, 2, 2, 2, 2, 2, 2 }
        };
        db.Context.AlertasCobranza.Add(alerta);
        await db.Context.SaveChangesAsync();

        var rowVersionViejo = alerta.RowVersion;

        DateTime? updatedAtOtraSesion;
        await using (var ctx2 = db.CreateNewContext())
        {
            var alertaOtraSesion = await ctx2.AlertasCobranza.SingleAsync(a => a.Id == alerta.Id);
            alertaOtraSesion.Observaciones = "Cambio por otro usuario";
            alertaOtraSesion.RowVersion = new byte[] { 8, 8, 8, 8, 8, 8, 8, 8 };
            await ctx2.SaveChangesAsync();
            updatedAtOtraSesion = alertaOtraSesion.UpdatedAt;
        }

        var service = new MoraService(db.Context, CreateMapper(), NullLogger<MoraService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.MarcarAlertaComoLeidaAsync(alerta.Id, rowVersionViejo));

        Assert.Contains("modificada por otro usuario", ex.Message, StringComparison.OrdinalIgnoreCase);

        await using var ctx3 = db.CreateNewContext();
        var alertaDb = await ctx3.AlertasCobranza.AsNoTracking().SingleAsync(a => a.Id == alerta.Id);
        Assert.NotNull(updatedAtOtraSesion);
        Assert.Equal(updatedAtOtraSesion, alertaDb.UpdatedAt);
    }
}
