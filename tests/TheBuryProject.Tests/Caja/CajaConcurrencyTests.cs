using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestDoubles;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Cajas;

public class CajaConcurrencyTests
{
    [Fact]
    public async Task ActualizarCajaAsync_con_RowVersion_viejo_lanza_conflicto_concurrencia()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        db.Context.Cajas.Add(new Caja
        {
            Codigo = "CAJA01",
            Nombre = "Caja Principal",
            Activa = true,
            Estado = EstadoCaja.Cerrada
        });
        await db.Context.SaveChangesAsync();

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var cajaService = new CajaService(
            db.Context,
            mapper,
            NullLogger<CajaService>.Instance,
            new NoopNotificacionService());

        var cajaInicial = db.Context.Cajas.Single();
        var rowVersionViejo = cajaInicial.RowVersion;

        // Simular otra sesión que actualiza el registro y cambia RowVersion
        await using (var ctx2 = db.CreateNewContext())
        {
            var cajaOtraSesion = await ctx2.Cajas.SingleAsync();
            cajaOtraSesion.Nombre = "Caja Principal (otro usuario)";
            cajaOtraSesion.RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };
            await ctx2.SaveChangesAsync();
        }

        var modelStale = new CajaViewModel
        {
            Id = cajaInicial.Id,
            Codigo = cajaInicial.Codigo,
            Nombre = "Caja Principal (mi cambio)",
            Activa = cajaInicial.Activa,
            Estado = cajaInicial.Estado,
            RowVersion = rowVersionViejo
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cajaService.ActualizarCajaAsync(cajaInicial.Id, modelStale));

        Assert.Contains("modificada por otro usuario", ex.Message);
    }

    [Fact]
    public async Task EliminarCajaAsync_con_RowVersion_actual_elimina_soft_delete()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        db.Context.Cajas.Add(new Caja
        {
            Codigo = "CAJA01",
            Nombre = "Caja Principal",
            Activa = true,
            Estado = EstadoCaja.Cerrada
        });
        await db.Context.SaveChangesAsync();

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var cajaService = new CajaService(
            db.Context,
            mapper,
            NullLogger<CajaService>.Instance,
            new NoopNotificacionService());

        var caja = db.Context.Cajas.Single();
        var rowVersion = caja.RowVersion;

        await cajaService.EliminarCajaAsync(caja.Id, rowVersion);

        await using var ctx2 = db.CreateNewContext();
        var cajaEliminada = await ctx2.Cajas.IgnoreQueryFilters().SingleAsync(c => c.Id == caja.Id);
        Assert.True(cajaEliminada.IsDeleted);
    }

    [Fact]
    public async Task EliminarCajaAsync_con_RowVersion_viejo_lanza_conflicto_concurrencia()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        db.Context.Cajas.Add(new Caja
        {
            Codigo = "CAJA01",
            Nombre = "Caja Principal",
            Activa = true,
            Estado = EstadoCaja.Cerrada
        });
        await db.Context.SaveChangesAsync();

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var cajaService = new CajaService(
            db.Context,
            mapper,
            NullLogger<CajaService>.Instance,
            new NoopNotificacionService());

        var cajaInicial = db.Context.Cajas.Single();
        var rowVersionViejo = cajaInicial.RowVersion;

        // Otra sesión modifica la caja => cambia RowVersion
        await using (var ctx2 = db.CreateNewContext())
        {
            var cajaOtraSesion = await ctx2.Cajas.SingleAsync();
            cajaOtraSesion.Nombre = "Caja Principal (otro usuario)";
            cajaOtraSesion.RowVersion = new byte[] { 8, 8, 8, 8, 8, 8, 8, 8 };
            await ctx2.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cajaService.EliminarCajaAsync(cajaInicial.Id, rowVersionViejo));

        Assert.Contains("modificada por otro usuario", ex.Message);
    }
}
