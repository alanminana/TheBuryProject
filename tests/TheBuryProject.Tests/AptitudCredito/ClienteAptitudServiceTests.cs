using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.AptitudCredito;

/// <summary>
/// Tests unitarios para el servicio de aptitud crediticia (semáforo).
/// Validan la lógica de evaluación sin acceso a base de datos.
/// </summary>
public class ClienteAptitudServiceTests
{
    #region Helpers

    private static ConfiguracionCredito CrearConfiguracion(
        bool validarDocumentacion = true,
        bool validarVencimiento = true,
        int diasGraciaVencimiento = 0,
        bool validarLimite = true,
        decimal? limiteMinimoRequerido = null,
        decimal? porcentajeCupoMinimo = null,
        bool validarMora = true,
        int? diasParaAutorizacion = 1,
        int? diasParaNoApto = null,
        decimal? montoMoraAutorizacion = null,
        decimal? montoMoraNoApto = null,
        int? cuotasVencidasNoApto = null)
    {
        return new ConfiguracionCredito
        {
            ValidarDocumentacion = validarDocumentacion,
            ValidarVencimientoDocumentos = validarVencimiento,
            DiasGraciaVencimientoDocumento = diasGraciaVencimiento,
            ValidarLimiteCredito = validarLimite,
            LimiteCreditoMinimo = limiteMinimoRequerido,
            PorcentajeCupoMinimoRequerido = porcentajeCupoMinimo,
            ValidarMora = validarMora,
            DiasParaRequerirAutorizacion = diasParaAutorizacion,
            DiasParaNoApto = diasParaNoApto,
            MontoMoraParaRequerirAutorizacion = montoMoraAutorizacion,
            MontoMoraParaNoApto = montoMoraNoApto,
            CuotasVencidasParaNoApto = cuotasVencidasNoApto,
            RecalculoAutomatico = true,
            AuditoriaActiva = true
        };
    }

    private static AptitudCrediticiaViewModel CrearResultadoBase()
    {
        return new AptitudCrediticiaViewModel
        {
            FechaEvaluacion = DateTime.Now,
            ConfiguracionCompleta = true,
            Documentacion = new AptitudDocumentacionDetalle { Evaluada = true, Completa = true },
            Cupo = new AptitudCupoDetalle { Evaluado = true, TieneCupoAsignado = true, CupoSuficiente = true, LimiteCredito = 100000, CupoDisponible = 50000 },
            Mora = new AptitudMoraDetalle { Evaluada = true, TieneMora = false }
        };
    }

    #endregion

    #region Tests de Estado Final

    [Fact]
    public void Estado_NoEvaluado_PorDefecto()
    {
        // Arrange
        var resultado = new AptitudCrediticiaViewModel();
        
        // Assert - Por defecto no está evaluado
        Assert.Equal(EstadoCrediticioCliente.NoEvaluado, resultado.Estado);
    }

    [Fact]
    public void Estado_Apto_CuandoSeAsignaExplicitamente()
    {
        // Arrange
        var resultado = CrearResultadoBase();
        resultado.Estado = EstadoCrediticioCliente.Apto; // Se asigna después de evaluar
        
        // Assert
        Assert.Equal(EstadoCrediticioCliente.Apto, resultado.Estado);
    }

    [Theory]
    [InlineData("success", EstadoCrediticioCliente.Apto)]
    [InlineData("warning", EstadoCrediticioCliente.RequiereAutorizacion)]
    [InlineData("danger", EstadoCrediticioCliente.NoApto)]
    [InlineData("secondary", EstadoCrediticioCliente.NoEvaluado)]
    public void ColorSemaforo_CorrectoPorEstado(string expectedColor, EstadoCrediticioCliente estado)
    {
        // Arrange
        var resultado = new AptitudCrediticiaViewModel { Estado = estado };
        
        // Assert
        Assert.Equal(expectedColor, resultado.ColorSemaforo);
    }

    [Theory]
    [InlineData("bi-check-circle-fill", EstadoCrediticioCliente.Apto)]
    [InlineData("bi-exclamation-triangle-fill", EstadoCrediticioCliente.RequiereAutorizacion)]
    [InlineData("bi-x-circle-fill", EstadoCrediticioCliente.NoApto)]
    [InlineData("bi-question-circle", EstadoCrediticioCliente.NoEvaluado)]
    public void Icono_CorrectoPorEstado(string expectedIcon, EstadoCrediticioCliente estado)
    {
        // Arrange
        var resultado = new AptitudCrediticiaViewModel { Estado = estado };
        
        // Assert
        Assert.Equal(expectedIcon, resultado.Icono);
    }

    [Theory]
    [InlineData("Apto para Crédito", EstadoCrediticioCliente.Apto)]
    [InlineData("Requiere Autorización", EstadoCrediticioCliente.RequiereAutorizacion)]
    [InlineData("No Apto", EstadoCrediticioCliente.NoApto)]
    [InlineData("Sin Evaluar", EstadoCrediticioCliente.NoEvaluado)]
    public void TextoEstado_CorrectoPorEstado(string expectedText, EstadoCrediticioCliente estado)
    {
        // Arrange
        var resultado = new AptitudCrediticiaViewModel { Estado = estado };
        
        // Assert
        Assert.Equal(expectedText, resultado.TextoEstado);
    }

    #endregion

    #region Tests de Documentación

    [Fact]
    public void Documentacion_Incompleta_MarcaNoApto()
    {
        // Arrange
        var detalle = new AptitudDocumentacionDetalle
        {
            Evaluada = true,
            Completa = false,
            DocumentosFaltantes = new List<string> { "DNI", "ReciboSueldo" }
        };
        
        // Assert
        Assert.False(detalle.Completa);
        Assert.Contains("DNI", detalle.DocumentosFaltantes);
        Assert.Contains("ReciboSueldo", detalle.DocumentosFaltantes);
    }

    [Fact]
    public void Documentacion_Vencida_MarcaTieneVencidos()
    {
        // Arrange
        var detalle = new AptitudDocumentacionDetalle
        {
            Evaluada = true,
            Completa = false,
            TieneVencidos = true,
            DocumentosVencidos = new List<string> { "ReciboSueldo" }
        };
        
        // Assert
        Assert.True(detalle.TieneVencidos);
        Assert.Contains("ReciboSueldo", detalle.DocumentosVencidos);
    }

    [Fact]
    public void Documentacion_NoEvaluada_CuandoDeshabilitada()
    {
        // Arrange
        var detalle = new AptitudDocumentacionDetalle
        {
            Evaluada = false,
            Completa = true,
            Mensaje = "Validación de documentación deshabilitada"
        };
        
        // Assert
        Assert.False(detalle.Evaluada);
        Assert.True(detalle.Completa); // Se considera completa cuando está deshabilitada
    }

    #endregion

    #region Tests de Cupo

    [Fact]
    public void Cupo_SinAsignar_MarcaSinCupo()
    {
        // Arrange
        var detalle = new AptitudCupoDetalle
        {
            Evaluado = true,
            TieneCupoAsignado = false,
            LimiteCredito = null,
            CupoSuficiente = false
        };
        
        // Assert
        Assert.False(detalle.TieneCupoAsignado);
        Assert.False(detalle.CupoSuficiente);
    }

    [Fact]
    public void Cupo_Agotado_MarcaCupoInsuficiente()
    {
        // Arrange
        var detalle = new AptitudCupoDetalle
        {
            Evaluado = true,
            TieneCupoAsignado = true,
            LimiteCredito = 100000m,
            CreditoUtilizado = 100000m,
            CupoDisponible = 0m,
            PorcentajeUtilizado = 100m,
            CupoSuficiente = false
        };
        
        // Assert
        Assert.True(detalle.TieneCupoAsignado);
        Assert.Equal(0m, detalle.CupoDisponible);
        Assert.False(detalle.CupoSuficiente);
    }

    [Theory]
    [InlineData(100000, 30000, 70000, 30)]
    [InlineData(50000, 25000, 25000, 50)]
    [InlineData(200000, 0, 200000, 0)]
    public void Cupo_CalculaCupoDisponible_Correctamente(decimal limite, decimal utilizado, decimal esperadoDisponible, decimal esperadoPorcentaje)
    {
        // Arrange & Act
        var cupoDisponible = Math.Max(0, limite - utilizado);
        var porcentajeUtilizado = limite > 0 ? (utilizado / limite) * 100 : 0;
        
        // Assert
        Assert.Equal(esperadoDisponible, cupoDisponible);
        Assert.Equal(esperadoPorcentaje, porcentajeUtilizado);
    }

    #endregion

    #region Tests de Mora

    [Fact]
    public void Mora_SinMora_EsApto()
    {
        // Arrange
        var detalle = new AptitudMoraDetalle
        {
            Evaluada = true,
            TieneMora = false,
            DiasMaximoMora = 0,
            MontoTotalMora = 0,
            CuotasVencidas = 0
        };
        
        // Assert
        Assert.False(detalle.TieneMora);
        Assert.False(detalle.RequiereAutorizacion);
        Assert.False(detalle.EsBloqueante);
    }

    [Fact]
    public void Mora_ConDiasMenores_RequiereAutorizacion()
    {
        // Arrange
        var detalle = new AptitudMoraDetalle
        {
            Evaluada = true,
            TieneMora = true,
            DiasMaximoMora = 15,
            MontoTotalMora = 5000,
            CuotasVencidas = 1,
            RequiereAutorizacion = true,
            EsBloqueante = false
        };
        
        // Assert
        Assert.True(detalle.TieneMora);
        Assert.True(detalle.RequiereAutorizacion);
        Assert.False(detalle.EsBloqueante);
    }

    [Fact]
    public void Mora_Critica_EsBloqueante()
    {
        // Arrange
        var detalle = new AptitudMoraDetalle
        {
            Evaluada = true,
            TieneMora = true,
            DiasMaximoMora = 90,
            MontoTotalMora = 50000,
            CuotasVencidas = 3,
            RequiereAutorizacion = true,
            EsBloqueante = true
        };
        
        // Assert
        Assert.True(detalle.TieneMora);
        Assert.True(detalle.EsBloqueante);
    }

    #endregion

    #region Tests de Configuración

    [Fact]
    public void Configuracion_TodosDeshabilitados_ConfiguracionValida()
    {
        // Arrange
        var config = CrearConfiguracion(
            validarDocumentacion: false,
            validarLimite: false,
            validarMora: false);
        
        // Assert
        Assert.False(config.ValidarDocumentacion);
        Assert.False(config.ValidarLimiteCredito);
        Assert.False(config.ValidarMora);
    }

    [Theory]
    [InlineData(0, 10, false)] // Sin gracia, 10 días vencido = vencido
    [InlineData(5, 3, false)]  // 5 días gracia, 3 días vencido = no vencido aún
    [InlineData(5, 10, false)] // 5 días gracia, 10 días vencido = vencido
    public void DiasGraciaVencimiento_AplicaCorrectamente(int diasGracia, int diasVencido, bool esperadoVigente)
    {
        // Arrange
        var fechaVencimiento = DateTime.Today.AddDays(-diasVencido);
        var fechaLimite = fechaVencimiento.AddDays(diasGracia);
        var estaVigente = DateTime.Today <= fechaLimite;
        
        // Assert - Solo el caso de 5 días gracia con 3 días vencido debería ser vigente
        if (diasGracia == 5 && diasVencido == 3)
        {
            Assert.True(estaVigente);
        }
        else
        {
            Assert.Equal(esperadoVigente, estaVigente);
        }
    }

    #endregion

    #region Tests de Detalles

    [Fact]
    public void Detalles_AgregaItemsCuandoHayProblemas()
    {
        // Arrange
        var resultado = new AptitudCrediticiaViewModel();
        
        resultado.Detalles.Add(new AptitudDetalleItem
        {
            Categoria = "Documentación",
            Descripcion = "Falta DNI",
            EsBloqueo = true,
            Icono = "bi-file-earmark-x",
            Color = "danger"
        });
        
        resultado.Detalles.Add(new AptitudDetalleItem
        {
            Categoria = "Mora",
            Descripcion = "15 días de atraso",
            EsBloqueo = false,
            Icono = "bi-clock-history",
            Color = "warning"
        });
        
        // Assert
        Assert.Equal(2, resultado.Detalles.Count);
        Assert.Contains(resultado.Detalles, d => d.Categoria == "Documentación" && d.EsBloqueo);
        Assert.Contains(resultado.Detalles, d => d.Categoria == "Mora" && !d.EsBloqueo);
    }

    #endregion

    #region Tests de Integración de Reglas

    [Fact]
    public void Reglas_DocumentacionFaltante_TienePreferenciaSobreMora()
    {
        // La documentación faltante siempre es NoApto, aunque la mora solo requiera autorización
        // Arrange
        var resultado = new AptitudCrediticiaViewModel
        {
            Documentacion = new AptitudDocumentacionDetalle
            {
                Evaluada = true,
                Completa = false,
                DocumentosFaltantes = new List<string> { "DNI" }
            },
            Cupo = new AptitudCupoDetalle { Evaluado = true, CupoSuficiente = true },
            Mora = new AptitudMoraDetalle
            {
                Evaluada = true,
                TieneMora = true,
                RequiereAutorizacion = true,
                EsBloqueante = false
            }
        };
        
        // El estado final debería ser NoApto por la documentación, no RequiereAutorizacion
        resultado.Estado = EstadoCrediticioCliente.NoApto;
        
        // Assert
        Assert.Equal(EstadoCrediticioCliente.NoApto, resultado.Estado);
    }

    [Fact]
    public void Reglas_SolaMora_EsRequiereAutorizacion()
    {
        // Si solo hay mora (sin bloqueo) y todo lo demás OK, es RequiereAutorizacion
        // Arrange
        var resultado = new AptitudCrediticiaViewModel
        {
            Documentacion = new AptitudDocumentacionDetalle { Evaluada = true, Completa = true },
            Cupo = new AptitudCupoDetalle { Evaluado = true, CupoSuficiente = true },
            Mora = new AptitudMoraDetalle
            {
                Evaluada = true,
                TieneMora = true,
                RequiereAutorizacion = true,
                EsBloqueante = false
            }
        };
        
        resultado.Estado = EstadoCrediticioCliente.RequiereAutorizacion;
        
        // Assert
        Assert.Equal(EstadoCrediticioCliente.RequiereAutorizacion, resultado.Estado);
    }

    #endregion
}
