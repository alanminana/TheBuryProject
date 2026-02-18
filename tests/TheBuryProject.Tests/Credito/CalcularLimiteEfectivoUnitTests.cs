using TheBuryProject.Services;
using Xunit;

namespace TheBuryProject.Tests.CreditoDisponibleTests;

public class CalcularLimiteEfectivoUnitTests
{
    [Fact]
    public void SinOverride_SumaPresetYExcepcion()
    {
        var resultado = CreditoDisponibleService.CalcularLimiteEfectivo(
            limiteBase: 100000m,
            limiteOverride: null,
            excepcionDeltaVigente: 20000m);

        Assert.Equal(120000m, resultado.Limite);
        Assert.Equal("Preset + Excepción", resultado.OrigenLimite);
    }

    [Fact]
    public void ConOverride_Reemplaza_NoSumaDelta()
    {
        var resultado = CreditoDisponibleService.CalcularLimiteEfectivo(
            limiteBase: 100000m,
            limiteOverride: 80000m,
            excepcionDeltaVigente: 20000m);

        Assert.Equal(80000m, resultado.Limite);
        Assert.Equal("Override absoluto", resultado.OrigenLimite);
    }

    [Fact]
    public void SinOverride_DeltaNegativo_NoReducePreset()
    {
        var resultado = CreditoDisponibleService.CalcularLimiteEfectivo(
            limiteBase: 100000m,
            limiteOverride: null,
            excepcionDeltaVigente: -5000m);

        Assert.Equal(100000m, resultado.Limite);
        Assert.Equal("Preset", resultado.OrigenLimite);
    }
}
