using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestDoubles;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;
using Caja = TheBuryProject.Models.Entities.Caja;

namespace TheBuryProject.Tests.CajaTests;

public class CajaFlowTests
{
    [Fact]
    public async Task Abrir_registrar_movimientos_y_cerrar_funciona_y_actualiza_estados_y_totales()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        db.Context.Cajas.Add(new TheBuryProject.Models.Entities.Caja
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

        var apertura = await cajaService.AbrirCajaAsync(new AbrirCajaViewModel
        {
            CajaId = caja.Id,
            MontoInicial = 100m,
            ObservacionesApertura = "test"
        }, usuario: "tester");

        var cajaTrasAbrir = await db.Context.Cajas.FindAsync(caja.Id);
        Assert.NotNull(cajaTrasAbrir);
        Assert.Equal(EstadoCaja.Abierta, cajaTrasAbrir!.Estado);

        await cajaService.RegistrarMovimientoAsync(new MovimientoCajaViewModel
        {
            AperturaCajaId = apertura.Id,
            Tipo = TipoMovimientoCaja.Ingreso,
            Concepto = ConceptoMovimientoCaja.Otro,
            Monto = 50m,
            Descripcion = "Ingreso test"
        }, usuario: "tester");

        await cajaService.RegistrarMovimientoAsync(new MovimientoCajaViewModel
        {
            AperturaCajaId = apertura.Id,
            Tipo = TipoMovimientoCaja.Egreso,
            Concepto = ConceptoMovimientoCaja.Otro,
            Monto = 20m,
            Descripcion = "Egreso test"
        }, usuario: "tester");

        var saldo = await cajaService.CalcularSaldoActualAsync(apertura.Id);
        Assert.Equal(130m, saldo);

        var cierre = await cajaService.CerrarCajaAsync(new CerrarCajaViewModel
        {
            AperturaCajaId = apertura.Id,
            MontoEsperadoSistema = saldo,
            EfectivoContado = 130m,
            ChequesContados = 0m,
            ValesContados = 0m,
            ObservacionesCierre = "ok"
        }, usuario: "tester");

        Assert.False(cierre.TieneDiferencia);
        Assert.Equal(100m, cierre.MontoInicialSistema);
        Assert.Equal(50m, cierre.TotalIngresosSistema);
        Assert.Equal(20m, cierre.TotalEgresosSistema);
        Assert.Equal(130m, cierre.MontoEsperadoSistema);
        Assert.Equal(130m, cierre.MontoTotalReal);

        var aperturaTrasCerrar = await db.Context.AperturasCaja.FindAsync(apertura.Id);
        Assert.NotNull(aperturaTrasCerrar);
        Assert.True(aperturaTrasCerrar!.Cerrada);

        var cajaTrasCerrar = await db.Context.Cajas.FindAsync(caja.Id);
        Assert.NotNull(cajaTrasCerrar);
        Assert.Equal(EstadoCaja.Cerrada, cajaTrasCerrar!.Estado);
    }

    [Fact]
    public async Task No_permite_registrar_movimiento_en_apertura_cerrada()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var caja = new TheBuryProject.Models.Entities.Caja
        {
            Codigo = "CAJA01",
            Nombre = "Caja Principal",
            Activa = true,
            Estado = EstadoCaja.Cerrada
        };
        db.Context.Cajas.Add(caja);
        await db.Context.SaveChangesAsync();

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }, NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var cajaService = new CajaService(
            db.Context,
            mapper,
            NullLogger<CajaService>.Instance,
            new NoopNotificacionService());

        var apertura = await cajaService.AbrirCajaAsync(new AbrirCajaViewModel
        {
            CajaId = caja.Id,
            MontoInicial = 0m
        }, usuario: "tester");

        await cajaService.CerrarCajaAsync(new CerrarCajaViewModel
        {
            AperturaCajaId = apertura.Id,
            MontoEsperadoSistema = 0m,
            EfectivoContado = 0m
        }, usuario: "tester");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cajaService.RegistrarMovimientoAsync(new MovimientoCajaViewModel
            {
                AperturaCajaId = apertura.Id,
                Tipo = TipoMovimientoCaja.Ingreso,
                Concepto = ConceptoMovimientoCaja.Otro,
                Monto = 1m,
                Descripcion = "no debe"
            }, usuario: "tester"));
    }
}
