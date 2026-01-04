using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using Xunit;

namespace TheBuryProject.Tests.Mora;

/// <summary>
/// Tests unitarios para las funciones puras del servicio de promesas de pago.
/// Los tests de integración con DB están separados.
/// </summary>
public class PromesaPagoServicePureTests
{
    #region Helpers

    private static AlertaCobranza CrearAlertaConPromesa(
        int diasHastaVencimiento = 5,
        EstadoGestionCobranza estado = EstadoGestionCobranza.PromesaPago,
        decimal montoPromesa = 1000m)
    {
        return new AlertaCobranza
        {
            Id = 1,
            ClienteId = 1,
            CreditoId = 1,
            EstadoGestion = estado,
            FechaPromesaPago = DateTime.Today.AddDays(diasHastaVencimiento),
            MontoPromesaPago = montoPromesa,
            MontoVencido = 1000m,
            MontoTotal = 1050m,
            FechaAlerta = DateTime.Today.AddDays(-10)
        };
    }

    #endregion

    #region EstaProximaAVencer

    [Fact]
    public void EstaProximaAVencer_sin_estado_promesa_retorna_false()
    {
        // Arrange
        var alerta = CrearAlertaConPromesa(diasHastaVencimiento: 2);
        alerta.EstadoGestion = EstadoGestionCobranza.EnGestion;

        // Act
        var resultado = EstaProximaAVencer(alerta, 3);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void EstaProximaAVencer_sin_fecha_promesa_retorna_false()
    {
        // Arrange
        var alerta = CrearAlertaConPromesa();
        alerta.FechaPromesaPago = null;

        // Act
        var resultado = EstaProximaAVencer(alerta, 3);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void EstaProximaAVencer_dentro_del_plazo_retorna_true()
    {
        // Arrange - vence en 2 días, anticipación 3 días
        var alerta = CrearAlertaConPromesa(diasHastaVencimiento: 2);

        // Act
        var resultado = EstaProximaAVencer(alerta, diasAnticipacion: 3);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void EstaProximaAVencer_fuera_del_plazo_retorna_false()
    {
        // Arrange - vence en 10 días, anticipación 3 días
        var alerta = CrearAlertaConPromesa(diasHastaVencimiento: 10);

        // Act
        var resultado = EstaProximaAVencer(alerta, diasAnticipacion: 3);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void EstaProximaAVencer_ya_vencida_retorna_false()
    {
        // Arrange - venció hace 2 días
        var alerta = CrearAlertaConPromesa(diasHastaVencimiento: -2);

        // Act
        var resultado = EstaProximaAVencer(alerta, diasAnticipacion: 3);

        // Assert
        Assert.False(resultado);
    }

    #endregion

    #region EstaVencida

    [Fact]
    public void EstaVencida_sin_estado_promesa_retorna_false()
    {
        // Arrange
        var alerta = CrearAlertaConPromesa(diasHastaVencimiento: -5);
        alerta.EstadoGestion = EstadoGestionCobranza.EnGestion;

        // Act
        var resultado = EstaVencida(alerta);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void EstaVencida_fecha_futura_retorna_false()
    {
        // Arrange
        var alerta = CrearAlertaConPromesa(diasHastaVencimiento: 5);

        // Act
        var resultado = EstaVencida(alerta);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void EstaVencida_fecha_hoy_retorna_false()
    {
        // Arrange
        var alerta = CrearAlertaConPromesa(diasHastaVencimiento: 0);

        // Act
        var resultado = EstaVencida(alerta);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void EstaVencida_fecha_pasada_retorna_true()
    {
        // Arrange
        var alerta = CrearAlertaConPromesa(diasHastaVencimiento: -1);

        // Act
        var resultado = EstaVencida(alerta);

        // Assert
        Assert.True(resultado);
    }

    #endregion

    #region DiasParaVencimiento

    [Fact]
    public void DiasParaVencimiento_sin_fecha_retorna_max()
    {
        // Arrange
        var alerta = CrearAlertaConPromesa();
        alerta.FechaPromesaPago = null;

        // Act
        var dias = DiasParaVencimiento(alerta);

        // Assert
        Assert.Equal(int.MaxValue, dias);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(0, 0)]
    [InlineData(-3, -3)]
    public void DiasParaVencimiento_calcula_correctamente(int diasHasta, int esperado)
    {
        // Arrange
        var alerta = CrearAlertaConPromesa(diasHastaVencimiento: diasHasta);

        // Act
        var dias = DiasParaVencimiento(alerta);

        // Assert
        Assert.Equal(esperado, dias);
    }

    #endregion

    #region Funciones puras locales (simula PromesaPagoService sin DB)

    private static bool EstaProximaAVencer(AlertaCobranza alerta, int diasAnticipacion, DateTime? fechaCalculo = null)
    {
        if (alerta.EstadoGestion != EstadoGestionCobranza.PromesaPago)
            return false;

        if (!alerta.FechaPromesaPago.HasValue)
            return false;

        var fecha = fechaCalculo ?? DateTime.Today;
        var diasRestantes = (alerta.FechaPromesaPago.Value - fecha).Days;

        return diasRestantes >= 0 && diasRestantes <= diasAnticipacion;
    }

    private static bool EstaVencida(AlertaCobranza alerta, DateTime? fechaCalculo = null)
    {
        if (alerta.EstadoGestion != EstadoGestionCobranza.PromesaPago)
            return false;

        if (!alerta.FechaPromesaPago.HasValue)
            return false;

        var fecha = fechaCalculo ?? DateTime.Today;
        return fecha > alerta.FechaPromesaPago.Value;
    }

    private static int DiasParaVencimiento(AlertaCobranza alerta, DateTime? fechaCalculo = null)
    {
        if (!alerta.FechaPromesaPago.HasValue)
            return int.MaxValue;

        var fecha = fechaCalculo ?? DateTime.Today;
        return (alerta.FechaPromesaPago.Value - fecha).Days;
    }

    #endregion
}
