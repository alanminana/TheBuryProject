using TheBuryProject.Models.Enums;
using Xunit;

namespace TheBuryProject.Tests.Enums;

/// <summary>
/// Tests de compatibilidad y consistencia de enums persistidos en DB.
/// Validan que los valores numéricos no cambien (romperían datos existentes).
/// </summary>
public class EnumCompatibilityTests
{
    #region TipoPago - Crítico (persistido en Venta)

    [Theory]
    [InlineData(TipoPago.Efectivo, 0)]
    [InlineData(TipoPago.Transferencia, 1)]
    [InlineData(TipoPago.TarjetaDebito, 2)]
    [InlineData(TipoPago.TarjetaCredito, 3)]
    [InlineData(TipoPago.Cheque, 4)]
    [InlineData(TipoPago.CreditoPersonal, 5)]
    [InlineData(TipoPago.MercadoPago, 6)]
    [InlineData(TipoPago.CuentaCorriente, 7)]
    [InlineData(TipoPago.Tarjeta, 8)]
    public void TipoPago_ValoresNumericos_NoDebenCambiar(TipoPago tipoPago, int valorEsperado)
    {
        Assert.Equal(valorEsperado, (int)tipoPago);
    }

    [Fact]
    public void TipoPago_CreditoPersonal_TieneMismoValorQueTypo()
    {
        // El alias correcto y el typo deben tener el mismo valor numérico
        // para mantener compatibilidad con datos existentes
#pragma warning disable CS0618 // Obsolete warning esperado
        Assert.Equal((int)TipoPago.CreditoPersonal, (int)TipoPago.CreditoPersonall);
#pragma warning restore CS0618
    }

    #endregion

    #region EstadoVenta - Crítico (persistido)

    [Theory]
    [InlineData(EstadoVenta.Cotizacion, 0)]
    [InlineData(EstadoVenta.Presupuesto, 1)]
    [InlineData(EstadoVenta.Confirmada, 2)]
    [InlineData(EstadoVenta.Facturada, 3)]
    [InlineData(EstadoVenta.Entregada, 4)]
    [InlineData(EstadoVenta.Cancelada, 5)]
    [InlineData(EstadoVenta.PendienteRequisitos, 6)]
    [InlineData(EstadoVenta.PendienteFinanciacion, 7)]
    public void EstadoVenta_ValoresNumericos_NoDebenCambiar(EstadoVenta estado, int valorEsperado)
    {
        Assert.Equal(valorEsperado, (int)estado);
    }

    #endregion

    #region EstadoCredito - Crítico (persistido)

    [Theory]
    [InlineData(EstadoCredito.Solicitado, 0)]
    [InlineData(EstadoCredito.Aprobado, 1)]
    [InlineData(EstadoCredito.Activo, 2)]
    [InlineData(EstadoCredito.Finalizado, 3)]
    [InlineData(EstadoCredito.Rechazado, 4)]
    [InlineData(EstadoCredito.Cancelado, 5)]
    [InlineData(EstadoCredito.PendienteConfiguracion, 6)]
    [InlineData(EstadoCredito.Configurado, 7)]
    [InlineData(EstadoCredito.Generado, 8)]
    public void EstadoCredito_ValoresNumericos_NoDebenCambiar(EstadoCredito estado, int valorEsperado)
    {
        Assert.Equal(valorEsperado, (int)estado);
    }

    #endregion

    #region PrioridadAlerta vs PrioridadNotificacion - Valores Diferentes

    [Fact]
    public void PrioridadAlerta_IniciaEn1()
    {
        // PrioridadAlerta: Baja=1, Media=2, Alta=3, Critica=4
        Assert.Equal(1, (int)PrioridadAlerta.Baja);
        Assert.Equal(4, (int)PrioridadAlerta.Critica);
    }

    [Fact]
    public void PrioridadNotificacion_IniciaEn0()
    {
        // PrioridadNotificacion: Baja=0, Media=1, Alta=2, Critica=3
        Assert.Equal(0, (int)PrioridadNotificacion.Baja);
        Assert.Equal(3, (int)PrioridadNotificacion.Critica);
    }

    [Fact]
    public void Prioridades_NoSonIntercambiables()
    {
        // Este test documenta que los dos enums tienen valores diferentes
        // y NO deben mezclarse en código
        Assert.NotEqual((int)PrioridadAlerta.Baja, (int)PrioridadNotificacion.Baja);
        Assert.NotEqual((int)PrioridadAlerta.Media, (int)PrioridadNotificacion.Media);
    }

    #endregion

    #region Enums que inician en 1 (verificar default)

    [Fact]
    public void EstadoDocumento_IniciaEn1_DefaultEsInvalido()
    {
        // EstadoDocumento inicia en 1, el valor default(int) = 0 no es válido
        var valores = Enum.GetValues<EstadoDocumento>();
        Assert.DoesNotContain(valores, e => (int)e == 0);
        Assert.Equal(1, (int)EstadoDocumento.Pendiente);
    }

    [Fact]
    public void EstadoAlerta_IniciaEn1_DefaultEsInvalido()
    {
        var valores = Enum.GetValues<EstadoAlerta>();
        Assert.DoesNotContain(valores, e => (int)e == 0);
        Assert.Equal(1, (int)EstadoAlerta.Pendiente);
    }

    [Fact]
    public void EstadoGestionCobranza_IniciaEn1_DefaultEsInvalido()
    {
        var valores = Enum.GetValues<EstadoGestionCobranza>();
        Assert.DoesNotContain(valores, e => (int)e == 0);
        Assert.Equal(1, (int)EstadoGestionCobranza.Pendiente);
    }

    #endregion

    #region Conteo de miembros (detectar adiciones/eliminaciones inesperadas)

    [Fact]
    public void TipoPago_Tiene10Miembros()
    {
        // 10 incluyendo el alias obsoleto CreditoPersonall (CreditoPersonal y CreditoPersonall comparten valor 5)
        var count = Enum.GetValues<TipoPago>().Length;
        Assert.Equal(10, count);
    }

    [Fact]
    public void EstadoVenta_Tiene8Miembros()
    {
        var count = Enum.GetValues<EstadoVenta>().Length;
        Assert.Equal(8, count);
    }

    [Fact]
    public void NivelRiesgoCredito_Tiene5Miembros()
    {
        var count = Enum.GetValues<NivelRiesgoCredito>().Length;
        Assert.Equal(5, count);
    }

    #endregion
}
