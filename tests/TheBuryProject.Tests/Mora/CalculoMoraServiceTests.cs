using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using Xunit;

namespace TheBuryProject.Tests.Mora;

/// <summary>
/// Tests unitarios para el motor de cálculo de mora.
/// Validan idempotencia, configuración y casos edge.
/// </summary>
public class CalculoMoraServiceTests
{
    private readonly CalculoMoraService _service = new();

    #region Helpers

    private static Cuota CrearCuota(
        int id = 1,
        int numeroCuota = 1,
        decimal capital = 1000m,
        decimal interes = 100m,
        DateTime? fechaVencimiento = null,
        decimal pagado = 0,
        EstadoCuota estado = EstadoCuota.Pendiente)
    {
        return new Cuota
        {
            Id = id,
            NumeroCuota = numeroCuota,
            MontoCapital = capital,
            MontoInteres = interes,
            MontoTotal = capital + interes,
            FechaVencimiento = fechaVencimiento ?? DateTime.Today.AddDays(-10),
            MontoPagado = pagado,
            Estado = estado
        };
    }

    private static ConfiguracionMora CrearConfiguracion(
        TipoTasaMora? tipoTasa = TipoTasaMora.Mensual,
        decimal? tasaBase = 5m, // 5% mensual
        BaseCalculoMora? baseCalculo = BaseCalculoMora.Capital,
        int? diasGracia = 3,
        bool escalonamiento = false,
        decimal? tasaPrimerMes = null,
        decimal? tasaSegundoMes = null,
        decimal? tasaTercerMes = null,
        bool topeActivo = false,
        TipoTopeMora? tipoTope = null,
        decimal? valorTope = null,
        decimal? moraMinima = null)
    {
        return new ConfiguracionMora
        {
            TipoTasaMora = tipoTasa,
            TasaMoraBase = tasaBase,
            BaseCalculoMora = baseCalculo,
            DiasGracia = diasGracia,
            EscalonamientoActivo = escalonamiento,
            TasaPrimerMes = tasaPrimerMes,
            TasaSegundoMes = tasaSegundoMes,
            TasaTercerMesEnAdelante = tasaTercerMes,
            TopeMaximoMoraActivo = topeActivo,
            TipoTopeMora = tipoTope,
            ValorTopeMora = valorTope,
            MoraMinima = moraMinima
        };
    }

    #endregion

    #region Idempotencia

    [Fact]
    public void CalcularMoraCuota_mismo_input_mismo_output()
    {
        // Arrange
        var cuota = CrearCuota(fechaVencimiento: new DateTime(2025, 12, 1));
        var config = CrearConfiguracion();
        var fechaCalculo = new DateTime(2025, 12, 20);

        // Act
        var resultado1 = _service.CalcularMoraCuota(cuota, config, fechaCalculo);
        var resultado2 = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert - mismo resultado exacto
        Assert.Equal(resultado1.TotalMora, resultado2.TotalMora);
        Assert.Equal(resultado1.Detalles[0].MoraFinal, resultado2.Detalles[0].MoraFinal);
        Assert.Equal(resultado1.Detalles[0].DiasAtrasoEfectivos, resultado2.Detalles[0].DiasAtrasoEfectivos);
    }

    [Fact]
    public void CalcularMoraCuota_no_modifica_cuota_original()
    {
        // Arrange
        var cuota = CrearCuota();
        var config = CrearConfiguracion();
        var capitalOriginal = cuota.MontoCapital;
        var punitorioOriginal = cuota.MontoPunitorio;

        // Act
        _service.CalcularMoraCuota(cuota, config);

        // Assert - cuota sin cambios
        Assert.Equal(capitalOriginal, cuota.MontoCapital);
        Assert.Equal(punitorioOriginal, cuota.MontoPunitorio);
    }

    #endregion

    #region Configuración Inválida

    [Fact]
    public void CalcularMoraCuota_sin_tipo_tasa_retorna_vacio()
    {
        // Arrange
        var cuota = CrearCuota();
        var config = CrearConfiguracion(tipoTasa: null);

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config);

        // Assert
        Assert.Equal(0, resultado.TotalMora);
        Assert.Equal(0, resultado.CuotasConMora);
    }

    [Fact]
    public void CalcularMoraCuota_tasa_cero_retorna_vacio()
    {
        // Arrange
        var cuota = CrearCuota();
        var config = CrearConfiguracion(tasaBase: 0);

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config);

        // Assert
        Assert.Equal(0, resultado.TotalMora);
    }

    [Fact]
    public void CalcularMoraCuotas_lista_vacia_retorna_vacio()
    {
        // Arrange
        var config = CrearConfiguracion();

        // Act
        var resultado = _service.CalcularMoraCuotas(Array.Empty<Cuota>(), config);

        // Assert
        Assert.Equal(0, resultado.CuotasProcesadas);
        Assert.Equal(0, resultado.TotalMora);
    }

    #endregion

    #region Días de Gracia

    [Fact]
    public void EstaVencida_dentro_de_gracia_retorna_false()
    {
        // Arrange - vencida hace 2 días, gracia de 3
        var cuota = CrearCuota(fechaVencimiento: DateTime.Today.AddDays(-2));

        // Act
        var resultado = _service.EstaVencida(cuota, diasGracia: 3);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void EstaVencida_pasada_gracia_retorna_true()
    {
        // Arrange - vencida hace 5 días, gracia de 3
        var cuota = CrearCuota(fechaVencimiento: DateTime.Today.AddDays(-5));

        // Act
        var resultado = _service.EstaVencida(cuota, diasGracia: 3);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void CalcularDiasAtrasoEfectivos_descuenta_gracia()
    {
        // Arrange
        var fechaVencimiento = DateTime.Today.AddDays(-10);

        // Act
        var diasEfectivos = _service.CalcularDiasAtrasoEfectivos(fechaVencimiento, diasGracia: 3);

        // Assert
        Assert.Equal(7, diasEfectivos); // 10 - 3 = 7
    }

    [Fact]
    public void CalcularDiasAtrasoEfectivos_no_retorna_negativo()
    {
        // Arrange - vencida hace 2 días, gracia de 5
        var fechaVencimiento = DateTime.Today.AddDays(-2);

        // Act
        var diasEfectivos = _service.CalcularDiasAtrasoEfectivos(fechaVencimiento, diasGracia: 5);

        // Assert
        Assert.Equal(0, diasEfectivos);
    }

    #endregion

    #region Cálculo Tasa Mensual

    [Fact]
    public void CalcularMoraCuota_tasa_mensual_calcula_correctamente()
    {
        // Arrange
        // Capital: 1000, Tasa: 5% mensual, Vencida hace 13 días, Gracia: 3
        // Días efectivos: 13 - 3 = 10
        // Tasa diaria: 5% / 30 = 0.1667%
        // Mora: 1000 * 0.001667 * 10 = 16.67
        var cuota = CrearCuota(
            capital: 1000m,
            fechaVencimiento: new DateTime(2025, 12, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Mensual,
            tasaBase: 5m,
            diasGracia: 3);
        var fechaCalculo = new DateTime(2025, 12, 14); // 13 días después

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert
        Assert.Equal(1, resultado.CuotasConMora);
        Assert.Equal(10, resultado.Detalles[0].DiasAtrasoEfectivos);
        // 1000 * (5/100/30) * 10 ≈ 16.67
        Assert.InRange(resultado.TotalMora, 16.6m, 16.7m);
    }

    #endregion

    #region Cálculo Tasa Diaria

    [Fact]
    public void CalcularMoraCuota_tasa_diaria_calcula_correctamente()
    {
        // Arrange
        // Capital: 1000, Tasa: 0.5% diaria, Vencida hace 13 días, Gracia: 3
        // Días efectivos: 10
        // Mora: 1000 * 0.005 * 10 = 50
        var cuota = CrearCuota(
            capital: 1000m,
            fechaVencimiento: new DateTime(2025, 12, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            tasaBase: 0.5m, // 0.5% diario
            diasGracia: 3);
        var fechaCalculo = new DateTime(2025, 12, 14);

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert
        Assert.Equal(50m, resultado.TotalMora);
    }

    #endregion

    #region Base de Cálculo

    [Fact]
    public void CalcularMoraCuota_base_capital_solo_usa_capital()
    {
        // Arrange
        var cuota = CrearCuota(
            capital: 1000m,
            interes: 200m,
            fechaVencimiento: new DateTime(2025, 12, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            tasaBase: 1m, // 1% diario
            baseCalculo: BaseCalculoMora.Capital,
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 11); // 10 días

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert - base = 1000 * 0.01 * 10 = 100
        Assert.Equal(100m, resultado.TotalMora);
    }

    [Fact]
    public void CalcularMoraCuota_base_capital_mas_interes_suma_ambos()
    {
        // Arrange
        var cuota = CrearCuota(
            capital: 1000m,
            interes: 200m,
            fechaVencimiento: new DateTime(2025, 12, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            tasaBase: 1m, // 1% diario
            baseCalculo: BaseCalculoMora.CapitalMasInteres,
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 11); // 10 días

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert - base = 1200 * 0.01 * 10 = 120
        Assert.Equal(120m, resultado.TotalMora);
    }

    [Fact]
    public void CalcularMoraCuota_descuenta_pagado_de_base()
    {
        // Arrange - pagó 300 de 1000 capital
        var cuota = CrearCuota(
            capital: 1000m,
            interes: 100m,
            pagado: 300m,
            fechaVencimiento: new DateTime(2025, 12, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            tasaBase: 1m,
            baseCalculo: BaseCalculoMora.Capital,
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 11); // 10 días

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert - base = (1000 - 300) * 0.01 * 10 = 70
        Assert.Equal(70m, resultado.TotalMora);
    }

    #endregion

    #region Escalonamiento

    [Fact]
    public void CalcularMoraCuota_escalonamiento_primer_mes_usa_tasa_primer_mes()
    {
        // Arrange - 20 días de atraso efectivo (< 30)
        var cuota = CrearCuota(
            capital: 1000m,
            fechaVencimiento: new DateTime(2025, 12, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            tasaBase: 0.5m,
            escalonamiento: true,
            tasaPrimerMes: 0.3m,   // 0.3% para primer mes
            tasaSegundoMes: 0.5m,  // 0.5% para segundo mes
            tasaTercerMes: 0.8m,   // 0.8% para 60+
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 21); // 20 días

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert - 1000 * 0.003 * 20 = 60
        Assert.Equal(60m, resultado.TotalMora);
    }

    [Fact]
    public void CalcularMoraCuota_escalonamiento_segundo_mes_usa_tasa_segundo_mes()
    {
        // Arrange - 45 días de atraso efectivo (31-60)
        var cuota = CrearCuota(
            capital: 1000m,
            fechaVencimiento: new DateTime(2025, 11, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            escalonamiento: true,
            tasaPrimerMes: 0.3m,
            tasaSegundoMes: 0.5m,
            tasaTercerMes: 0.8m,
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 16); // 45 días

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert - 1000 * 0.005 * 45 = 225
        Assert.Equal(225m, resultado.TotalMora);
    }

    [Fact]
    public void CalcularMoraCuota_escalonamiento_tercer_mes_usa_tasa_tercer_mes()
    {
        // Arrange - 70 días de atraso efectivo (> 60)
        var cuota = CrearCuota(
            capital: 1000m,
            fechaVencimiento: new DateTime(2025, 10, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            escalonamiento: true,
            tasaPrimerMes: 0.3m,
            tasaSegundoMes: 0.5m,
            tasaTercerMes: 0.8m,
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 10); // 70 días

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert - 1000 * 0.008 * 70 = 560
        Assert.Equal(560m, resultado.TotalMora);
    }

    #endregion

    #region Topes

    [Fact]
    public void CalcularMoraCuota_tope_porcentaje_limita_mora()
    {
        // Arrange - mora calculada sería 500, tope 30% del capital = 300
        var cuota = CrearCuota(
            capital: 1000m,
            fechaVencimiento: new DateTime(2025, 10, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            tasaBase: 1m, // 1% diario
            topeActivo: true,
            tipoTope: TipoTopeMora.Porcentaje,
            valorTope: 30m, // máximo 30% del capital
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 20); // ~80 días → 800 de mora bruta

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert - tope: 1000 * 30% = 300
        Assert.Equal(300m, resultado.TotalMora);
        Assert.True(resultado.Detalles[0].TopeAplicado);
    }

    [Fact]
    public void CalcularMoraCuota_tope_monto_fijo_limita_mora()
    {
        // Arrange
        var cuota = CrearCuota(
            capital: 1000m,
            fechaVencimiento: new DateTime(2025, 10, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            tasaBase: 1m,
            topeActivo: true,
            tipoTope: TipoTopeMora.MontoFijo,
            valorTope: 200m, // máximo $200
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 20); // ~80 días

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert
        Assert.Equal(200m, resultado.TotalMora);
        Assert.True(resultado.Detalles[0].TopeAplicado);
    }

    [Fact]
    public void CalcularMoraCuota_mora_minima_aplica_cuando_mora_es_menor()
    {
        // Arrange - mora calculada sería ~1.67, mínima 10
        var cuota = CrearCuota(
            capital: 1000m,
            fechaVencimiento: new DateTime(2025, 12, 1));
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Mensual,
            tasaBase: 5m,
            moraMinima: 10m,
            diasGracia: 12);
        var fechaCalculo = new DateTime(2025, 12, 14); // 1 día efectivo

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config, fechaCalculo);

        // Assert
        Assert.Equal(10m, resultado.TotalMora);
        Assert.True(resultado.Detalles[0].MinimoAplicado);
    }

    #endregion

    #region Cuotas Pagadas

    [Fact]
    public void CalcularMoraCuota_pagada_retorna_cero()
    {
        // Arrange
        var cuota = CrearCuota(estado: EstadoCuota.Pagada);
        var config = CrearConfiguracion();

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config);

        // Assert
        Assert.Equal(0, resultado.TotalMora);
        Assert.Equal(0, resultado.CuotasConMora);
    }

    [Fact]
    public void CalcularMoraCuota_con_fecha_pago_retorna_cero()
    {
        // Arrange
        var cuota = CrearCuota();
        cuota.FechaPago = DateTime.Today.AddDays(-1);
        var config = CrearConfiguracion();

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config);

        // Assert
        Assert.Equal(0, resultado.TotalMora);
    }

    #endregion

    #region Múltiples Cuotas

    [Fact]
    public void CalcularMoraCuotas_suma_total_correcto()
    {
        // Arrange
        var cuotas = new[]
        {
            CrearCuota(id: 1, capital: 1000m, fechaVencimiento: new DateTime(2025, 12, 1)),
            CrearCuota(id: 2, capital: 1000m, fechaVencimiento: new DateTime(2025, 12, 1)),
            CrearCuota(id: 3, capital: 1000m, fechaVencimiento: new DateTime(2025, 12, 1))
        };
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            tasaBase: 1m,
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 11); // 10 días

        // Act
        var resultado = _service.CalcularMoraCuotas(cuotas, config, fechaCalculo);

        // Assert
        Assert.Equal(3, resultado.CuotasProcesadas);
        Assert.Equal(3, resultado.CuotasConMora);
        Assert.Equal(300m, resultado.TotalMora); // 3 * 100
        Assert.Equal(3000m, resultado.TotalCapitalVencido);
    }

    [Fact]
    public void CalcularMoraCuotas_excluye_pagadas_del_total()
    {
        // Arrange
        var cuotas = new[]
        {
            CrearCuota(id: 1, capital: 1000m, fechaVencimiento: new DateTime(2025, 12, 1)),
            CrearCuota(id: 2, capital: 1000m, fechaVencimiento: new DateTime(2025, 12, 1), estado: EstadoCuota.Pagada),
            CrearCuota(id: 3, capital: 1000m, fechaVencimiento: new DateTime(2025, 12, 1))
        };
        var config = CrearConfiguracion(
            tipoTasa: TipoTasaMora.Diaria,
            tasaBase: 1m,
            diasGracia: 0);
        var fechaCalculo = new DateTime(2025, 12, 11);

        // Act
        var resultado = _service.CalcularMoraCuotas(cuotas, config, fechaCalculo);

        // Assert
        Assert.Equal(3, resultado.CuotasProcesadas);
        Assert.Equal(2, resultado.CuotasConMora); // solo 2 con mora
        Assert.Equal(200m, resultado.TotalMora);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalcularMoraCuota_cuota_no_vencida_retorna_cero()
    {
        // Arrange - vence en el futuro
        var cuota = CrearCuota(fechaVencimiento: DateTime.Today.AddDays(10));
        var config = CrearConfiguracion();

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config);

        // Assert
        Assert.Equal(0, resultado.TotalMora);
    }

    [Fact]
    public void CalcularMoraCuota_capital_cero_retorna_cero()
    {
        // Arrange
        var cuota = CrearCuota(capital: 0);
        var config = CrearConfiguracion();

        // Act
        var resultado = _service.CalcularMoraCuota(cuota, config);

        // Assert
        Assert.Equal(0, resultado.TotalMora);
    }

    [Fact]
    public void CalcularMoraCuota_argumentos_null_lanza_excepcion()
    {
        // Arrange
        var config = CrearConfiguracion();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.CalcularMoraCuota(null!, config));
        Assert.Throws<ArgumentNullException>(() => _service.CalcularMoraCuota(CrearCuota(), null!));
    }

    #endregion
}
