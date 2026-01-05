using TheBuryProject.Helpers;
using TheBuryProject.Models.Enums;
using Xunit;

namespace TheBuryProject.Tests.Helpers;

/// <summary>
/// Tests para EnumHelper - conversiones y display de enums
/// </summary>
public class EnumHelperTests
{
    #region GetDisplayName Tests

    [Fact]
    public void GetDisplayName_ConDisplayAttribute_RetornaDisplayName()
    {
        // Arrange - NivelRiesgoCredito tiene [Display(Name = "...")]
        var nivel = NivelRiesgoCredito.AprobadoTotal;

        // Act
        var resultado = nivel.GetDisplayName();

        // Assert
        Assert.Equal("5 - Aprobado Total", resultado);
    }

    [Fact]
    public void GetDisplayName_SinDisplayAttribute_RetornaNombreMiembro()
    {
        // Arrange - TipoPago.Efectivo no tiene [Display]
        var tipoPago = TipoPago.Efectivo;

        // Act
        var resultado = tipoPago.GetDisplayName();

        // Assert
        Assert.Equal("Efectivo", resultado);
    }

    [Fact]
    public void GetDisplayName_CacheaResultados_MismaInstancia()
    {
        // Arrange
        var nivel1 = NivelRiesgoCredito.Rechazado;
        var nivel2 = NivelRiesgoCredito.Rechazado;

        // Act
        var resultado1 = nivel1.GetDisplayName();
        var resultado2 = nivel2.GetDisplayName();

        // Assert - Ambos deben retornar el mismo string (cacheado)
        Assert.Same(resultado1, resultado2);
    }

    [Theory]
    [InlineData(NivelRiesgoCredito.Rechazado, "1 - Rechazado")]
    [InlineData(NivelRiesgoCredito.RechazadoRevisar, "2 - Rechazado (Revisar)")]
    [InlineData(NivelRiesgoCredito.AprobadoCondicional, "3 - Aprobado Condicional")]
    [InlineData(NivelRiesgoCredito.AprobadoLimitado, "4 - Aprobado Limitado")]
    [InlineData(NivelRiesgoCredito.AprobadoTotal, "5 - Aprobado Total")]
    public void GetDisplayName_NivelRiesgoCredito_RetornaValoresCorrectos(
        NivelRiesgoCredito nivel, string esperado)
    {
        Assert.Equal(esperado, nivel.GetDisplayName());
    }

    #endregion

    #region GetSelectList Tests

    [Fact]
    public void GetSelectList_RetornaTodosLosValoresNoObsoletos()
    {
        // Act
        var items = EnumHelper.GetSelectList<EstadoVenta>().ToList();

        // Assert - EstadoVenta tiene 8 valores, ninguno obsoleto
        Assert.Equal(8, items.Count);
    }

    [Fact]
    public void GetSelectList_ExcluyeValoresObsoletos()
    {
        // Act - TipoPago tiene CreditoPersonall marcado como [Obsolete]
        var items = EnumHelper.GetSelectList<TipoPago>().ToList();

        // TipoPago tiene 10 nombres en el enum, pero:
        // - CreditoPersonall está [Obsolete] y se excluye
        // - CreditoPersonal (valor 5) NO se excluye por duplicado porque va primero
        // Resultado: 9 items (todos los no-obsoletos)
        Assert.Equal(9, items.Count);
        Assert.DoesNotContain(items, i => i.Text == "CreditoPersonall");
        Assert.Contains(items, i => i.Text == "CreditoPersonal");
    }

    [Fact]
    public void GetSelectList_ConValorSeleccionado_MarcaSelected()
    {
        // Act
        var items = EnumHelper.GetSelectList<EstadoVenta>(EstadoVenta.Confirmada).ToList();

        // Assert
        var confirmada = items.Single(i => i.Value == ((int)EstadoVenta.Confirmada).ToString());
        Assert.True(confirmada.Selected);

        // Los demás no deben estar seleccionados
        var otros = items.Where(i => i.Value != ((int)EstadoVenta.Confirmada).ToString());
        Assert.All(otros, i => Assert.False(i.Selected));
    }

    [Fact]
    public void GetSelectList_ValueEsElEntero_TextEsDisplayName()
    {
        // Act
        var items = EnumHelper.GetSelectList<NivelRiesgoCredito>().ToList();

        // Assert
        var rechazado = items.Single(i => i.Value == "1");
        Assert.Equal("1 - Rechazado", rechazado.Text);

        var aprobadoTotal = items.Single(i => i.Value == "5");
        Assert.Equal("5 - Aprobado Total", aprobadoTotal.Text);
    }

    #endregion
}
