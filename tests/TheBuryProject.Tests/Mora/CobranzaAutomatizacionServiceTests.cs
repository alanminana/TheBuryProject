using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Services.Models;
using Xunit;

namespace TheBuryProject.Tests.Mora;

/// <summary>
/// Tests unitarios para el servicio de automatización por tramos.
/// </summary>
public class CobranzaAutomatizacionServiceTests
{
    private readonly CobranzaAutomatizacionService _service;

    public CobranzaAutomatizacionServiceTests()
    {
        _service = new CobranzaAutomatizacionService(NullLogger<CobranzaAutomatizacionService>.Instance);
    }

    #region Helpers

    private static ConfiguracionMora CrearConfiguracion(
        int? diasGracia = 3,
        int? diasMedia = 15,
        int? diasAlta = 30,
        int? diasCritica = 60,
        bool alertasPreventivas = false,
        int? diasPreventiva = 5,
        bool bloqueoActivo = false,
        int? diasBloqueo = 90,
        int? cuotasBloqueo = null,
        decimal? montoBloqueo = null,
        bool procesoActivo = true)
    {
        return new ConfiguracionMora
        {
            DiasGracia = diasGracia,
            DiasParaPrioridadMedia = diasMedia,
            DiasParaPrioridadAlta = diasAlta,
            DiasParaPrioridadCritica = diasCritica,
            AlertasPreventivasActivas = alertasPreventivas,
            DiasAntesAlertaPreventiva = diasPreventiva,
            BloqueoAutomaticoActivo = bloqueoActivo,
            DiasParaBloquear = diasBloqueo,
            CuotasVencidasParaBloquear = cuotasBloqueo,
            MontoMoraParaBloquear = montoBloqueo,
            ProcesoAutomaticoActivo = procesoActivo
        };
    }

    private static AlertaCobranza CrearAlerta(
        int id = 1,
        int diasAtraso = 10,
        decimal montoVencido = 1000m,
        decimal montoMora = 50m,
        PrioridadAlerta prioridad = PrioridadAlerta.Baja,
        EstadoGestionCobranza estado = EstadoGestionCobranza.Pendiente,
        DateTime? fechaPromesa = null,
        int cuotasVencidas = 1)
    {
        return new AlertaCobranza
        {
            Id = id,
            ClienteId = 1,
            CreditoId = 1,
            DiasAtraso = diasAtraso,
            MontoVencido = montoVencido,
            MontoMoraCalculada = montoMora,
            MontoTotal = montoVencido + montoMora,
            Prioridad = prioridad,
            EstadoGestion = estado,
            FechaPromesaPago = fechaPromesa,
            CuotasVencidas = cuotasVencidas,
            FechaAlerta = DateTime.Today.AddDays(-diasAtraso)
        };
    }

    #endregion

    #region GenerarTramos

    [Fact]
    public void GenerarTramos_configuracion_basica_genera_tramos_ordenados()
    {
        // Arrange
        var config = CrearConfiguracion();

        // Act
        var tramos = _service.GenerarTramos(config);

        // Assert
        Assert.NotEmpty(tramos);
        Assert.True(tramos.Count >= 4); // Al menos: gracia, mora inicial, media, alta, crítica
        
        // Verificar orden ascendente
        for (int i = 1; i < tramos.Count; i++)
        {
            Assert.True(tramos[i].DiasDesde >= tramos[i - 1].DiasDesde);
        }
    }

    [Fact]
    public void GenerarTramos_con_alertas_preventivas_incluye_tramo_negativo()
    {
        // Arrange
        var config = CrearConfiguracion(alertasPreventivas: true, diasPreventiva: 5);

        // Act
        var tramos = _service.GenerarTramos(config);

        // Assert
        var tramoPreventivo = tramos.FirstOrDefault(t => t.DiasDesde < 0);
        Assert.NotNull(tramoPreventivo);
        Assert.Equal("Preventivo", tramoPreventivo!.Nombre);
        Assert.Equal(-5, tramoPreventivo!.DiasDesde);
    }

    [Fact]
    public void GenerarTramos_con_bloqueo_incluye_accion_bloqueo()
    {
        // Arrange
        var config = CrearConfiguracion(bloqueoActivo: true, diasBloqueo: 90);

        // Act
        var tramos = _service.GenerarTramos(config);

        // Assert
        var tramoCritico = tramos.FirstOrDefault(t => t.Nombre == "Mora Crítica");
        Assert.NotNull(tramoCritico);
        Assert.Contains(tramoCritico!.Acciones, a => a.Tipo == TipoAccionAutomatica.BloquearCliente);
    }

    #endregion

    #region ObtenerTramo

    [Theory]
    [InlineData(-3, "Preventivo")]
    [InlineData(2, "Período de Gracia")]
    [InlineData(5, "Mora Inicial")]
    [InlineData(20, "Mora Media")]
    [InlineData(45, "Mora Alta")]
    [InlineData(90, "Mora Crítica")]
    public void ObtenerTramo_segun_dias_atraso(int diasAtraso, string nombreEsperado)
    {
        // Arrange
        var config = CrearConfiguracion(
            diasGracia: 3,
            diasMedia: 15,
            diasAlta: 30,
            diasCritica: 60,
            alertasPreventivas: true,
            diasPreventiva: 5);
        var tramos = _service.GenerarTramos(config);

        // Act
        var tramo = _service.ObtenerTramo(diasAtraso, tramos);

        // Assert
        Assert.NotNull(tramo);
        Assert.Equal(nombreEsperado, tramo!.Nombre);
    }

    #endregion

    #region CalcularPrioridad

    [Fact]
    public void CalcularPrioridad_por_dias_escala_correctamente()
    {
        // Arrange
        var config = CrearConfiguracion(diasMedia: 15, diasAlta: 30, diasCritica: 60);

        // Act & Assert
        Assert.Equal(PrioridadAlerta.Baja, _service.CalcularPrioridad(CrearAlerta(diasAtraso: 10), config));
        Assert.Equal(PrioridadAlerta.Media, _service.CalcularPrioridad(CrearAlerta(diasAtraso: 20), config));
        Assert.Equal(PrioridadAlerta.Alta, _service.CalcularPrioridad(CrearAlerta(diasAtraso: 45), config));
        Assert.Equal(PrioridadAlerta.Critica, _service.CalcularPrioridad(CrearAlerta(diasAtraso: 70), config));
    }

    [Fact]
    public void CalcularPrioridad_por_monto_escala_correctamente()
    {
        // Arrange
        var config = new ConfiguracionMora
        {
            MontoParaPrioridadMedia = 5000m,
            MontoParaPrioridadAlta = 20000m,
            MontoParaPrioridadCritica = 50000m
        };

        // Act & Assert
        var alertaBaja = CrearAlerta(diasAtraso: 5, montoVencido: 1000m, montoMora: 0);
        Assert.Equal(PrioridadAlerta.Baja, _service.CalcularPrioridad(alertaBaja, config));

        var alertaMedia = CrearAlerta(diasAtraso: 5, montoVencido: 6000m, montoMora: 0);
        Assert.Equal(PrioridadAlerta.Media, _service.CalcularPrioridad(alertaMedia, config));

        var alertaAlta = CrearAlerta(diasAtraso: 5, montoVencido: 25000m, montoMora: 0);
        Assert.Equal(PrioridadAlerta.Alta, _service.CalcularPrioridad(alertaAlta, config));

        var alertaCritica = CrearAlerta(diasAtraso: 5, montoVencido: 55000m, montoMora: 0);
        Assert.Equal(PrioridadAlerta.Critica, _service.CalcularPrioridad(alertaCritica, config));
    }

    #endregion

    #region DebeBloquearCliente

    [Fact]
    public void DebeBloquearCliente_bloqueo_desactivado_retorna_false()
    {
        // Arrange
        var config = CrearConfiguracion(bloqueoActivo: false, diasBloqueo: 30);

        // Act
        var resultado = _service.DebeBloquearCliente(100, 5, 10000m, config);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void DebeBloquearCliente_por_dias_bloquea()
    {
        // Arrange
        var config = CrearConfiguracion(bloqueoActivo: true, diasBloqueo: 60);

        // Act & Assert
        Assert.False(_service.DebeBloquearCliente(50, 1, 100m, config));
        Assert.True(_service.DebeBloquearCliente(60, 1, 100m, config));
        Assert.True(_service.DebeBloquearCliente(90, 1, 100m, config));
    }

    [Fact]
    public void DebeBloquearCliente_por_cuotas_bloquea()
    {
        // Arrange
        var config = CrearConfiguracion(bloqueoActivo: true, cuotasBloqueo: 3);

        // Act & Assert
        Assert.False(_service.DebeBloquearCliente(10, 2, 100m, config));
        Assert.True(_service.DebeBloquearCliente(10, 3, 100m, config));
    }

    [Fact]
    public void DebeBloquearCliente_por_monto_bloquea()
    {
        // Arrange
        var config = CrearConfiguracion(bloqueoActivo: true, montoBloqueo: 5000m);

        // Act & Assert
        Assert.False(_service.DebeBloquearCliente(10, 1, 4000m, config));
        Assert.True(_service.DebeBloquearCliente(10, 1, 5000m, config));
    }

    #endregion

    #region DeterminarAcciones

    [Fact]
    public void DeterminarAcciones_proceso_desactivado_retorna_vacio()
    {
        // Arrange
        var config = CrearConfiguracion(procesoActivo: false);
        var alerta = CrearAlerta(diasAtraso: 30);

        // Act
        var acciones = _service.DeterminarAcciones(alerta, config);

        // Assert
        Assert.Empty(acciones);
    }

    [Fact]
    public void DeterminarAcciones_promesa_vencida_incluye_accion_incumplida()
    {
        // Arrange
        var config = CrearConfiguracion(procesoActivo: true);
        config.DiasParaCumplirPromesa = 2;

        var alerta = CrearAlerta(
            diasAtraso: 20,
            estado: EstadoGestionCobranza.PromesaPago,
            fechaPromesa: DateTime.Today.AddDays(-5)); // Venció hace 5 días

        // Act
        var acciones = _service.DeterminarAcciones(alerta, config, DateTime.Today);

        // Assert
        Assert.Contains(acciones, a => a.Tipo == TipoAccionAutomatica.MarcarPromesaIncumplida);
    }

    [Fact]
    public void DeterminarAcciones_promesa_dentro_tolerancia_no_marca_incumplida()
    {
        // Arrange
        var config = CrearConfiguracion(procesoActivo: true);
        config.DiasParaCumplirPromesa = 5;

        var alerta = CrearAlerta(
            diasAtraso: 20,
            estado: EstadoGestionCobranza.PromesaPago,
            fechaPromesa: DateTime.Today.AddDays(-3)); // Venció hace 3 días, tolerancia 5

        // Act
        var acciones = _service.DeterminarAcciones(alerta, config, DateTime.Today);

        // Assert
        Assert.DoesNotContain(acciones, a => a.Tipo == TipoAccionAutomatica.MarcarPromesaIncumplida);
    }

    #endregion

    #region ProcesarAlertasAsync

    [Fact]
    public async Task ProcesarAlertasAsync_lista_vacia_retorna_vacio()
    {
        // Arrange
        var config = CrearConfiguracion(procesoActivo: true);

        // Act
        var resultado = await _service.ProcesarAlertasAsync(Array.Empty<AlertaCobranza>(), config);

        // Assert
        Assert.True(resultado.Exitoso);
        Assert.Equal(0, resultado.AlertasProcesadas);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_proceso_desactivado_retorna_vacio()
    {
        // Arrange
        var config = CrearConfiguracion(procesoActivo: false);
        var alertas = new[] { CrearAlerta(diasAtraso: 30) };

        // Act
        var resultado = await _service.ProcesarAlertasAsync(alertas, config);

        // Assert
        Assert.True(resultado.Exitoso);
        Assert.Equal(0, resultado.AccionesEjecutadas);
    }

    [Fact]
    public async Task ProcesarAlertasAsync_escala_prioridad_correctamente()
    {
        // Arrange
        var config = CrearConfiguracion(
            procesoActivo: true,
            diasMedia: 15,
            diasAlta: 30);

        var alerta = CrearAlerta(
            diasAtraso: 15, // Justo en el límite de media
            prioridad: PrioridadAlerta.Baja);

        // Act
        var resultado = await _service.ProcesarAlertasAsync(new[] { alerta }, config);

        // Assert
        Assert.True(resultado.Exitoso);
        // La prioridad debería haberse escalado
        Assert.Equal(PrioridadAlerta.Media, alerta.Prioridad);
    }

    #endregion
}

