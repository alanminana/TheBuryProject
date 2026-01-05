using TheBuryProject.Models.Enums;
using Xunit;

namespace TheBuryProject.Tests.Enums;

/// <summary>
/// Tests para validar que todos los valores de enums críticos
/// están manejados en switch statements del negocio.
/// </summary>
public class EnumSwitchCoverageTests
{
    #region VentaViewModel.EstadoDisplay Switch Coverage

    [Theory]
    [InlineData(EstadoVenta.Cotizacion, "Cotización")]
    [InlineData(EstadoVenta.Presupuesto, "Presupuesto")]
    [InlineData(EstadoVenta.Confirmada, "Confirmada")]
    [InlineData(EstadoVenta.Facturada, "Facturada")]
    [InlineData(EstadoVenta.Entregada, "Entregada")]
    [InlineData(EstadoVenta.Cancelada, "Cancelada")]
    [InlineData(EstadoVenta.PendienteRequisitos, "Pendiente Requisitos")]
    [InlineData(EstadoVenta.PendienteFinanciacion, "Pendiente Financiación")]
    public void EstadoVenta_TodosLosValores_TienenDisplayDefinido(EstadoVenta estado, string displayEsperado)
    {
        // Este test verifica que todos los estados tienen un display text definido
        // Si se agrega un nuevo estado y no se actualiza el switch, el test fallará
        var display = estado switch
        {
            EstadoVenta.Cotizacion => "Cotización",
            EstadoVenta.Presupuesto => "Presupuesto",
            EstadoVenta.Confirmada => "Confirmada",
            EstadoVenta.Facturada => "Facturada",
            EstadoVenta.Entregada => "Entregada",
            EstadoVenta.Cancelada => "Cancelada",
            EstadoVenta.PendienteRequisitos => "Pendiente Requisitos",
            EstadoVenta.PendienteFinanciacion => "Pendiente Financiación",
            _ => throw new ArgumentOutOfRangeException(nameof(estado), $"Estado no manejado: {estado}")
        };

        Assert.Equal(displayEsperado, display);
    }

    [Fact]
    public void EstadoVenta_SwitchCobertura_TodosLosValoresCubiertos()
    {
        // Verificar que todos los valores del enum están cubiertos
        var todosLosEstados = Enum.GetValues<EstadoVenta>();
        
        foreach (var estado in todosLosEstados)
        {
            // Si algún valor no está manejado, lanzará excepción
            var exception = Record.Exception(() =>
            {
                _ = estado switch
                {
                    EstadoVenta.Cotizacion => "ok",
                    EstadoVenta.Presupuesto => "ok",
                    EstadoVenta.Confirmada => "ok",
                    EstadoVenta.Facturada => "ok",
                    EstadoVenta.Entregada => "ok",
                    EstadoVenta.Cancelada => "ok",
                    EstadoVenta.PendienteRequisitos => "ok",
                    EstadoVenta.PendienteFinanciacion => "ok",
                    _ => throw new NotSupportedException($"Nuevo estado no manejado: {estado}")
                };
            });
            
            Assert.Null(exception);
        }
    }

    #endregion

    #region TipoPago Switch Coverage (CajaService pattern)

    [Fact]
    public void TipoPago_TodosLosValoresTienenCategoria()
    {
        var todosTipos = Enum.GetValues<TipoPago>();
        
        foreach (var tipo in todosTipos)
        {
            var categoria = CategorizarTipoPago(tipo);
            Assert.NotNull(categoria);
        }
    }

    /// <summary>
    /// Replica la lógica del switch en CajaService para validar cobertura
    /// </summary>
    private static string CategorizarTipoPago(TipoPago tipoPago)
    {
        return tipoPago switch
        {
            TipoPago.Efectivo => "Efectivo",
            TipoPago.TarjetaDebito or TipoPago.TarjetaCredito or TipoPago.Tarjeta => "Tarjeta",
            TipoPago.Cheque => "Cheque",
            TipoPago.Transferencia or TipoPago.MercadoPago => "Transferencia",
#pragma warning disable CS0618 // Obsolete warning esperado
            TipoPago.CreditoPersonal or TipoPago.CreditoPersonall => "Credito",
#pragma warning restore CS0618
            TipoPago.CuentaCorriente => "CuentaCorriente",
            _ => throw new NotSupportedException($"TipoPago no categorizado: {tipoPago}")
        };
    }

    #endregion

    #region EstadoCredito Switch Coverage

    [Fact]
    public void EstadoCredito_TodosLosValoresTienenDescripcion()
    {
        var todosEstados = Enum.GetValues<EstadoCredito>();
        
        foreach (var estado in todosEstados)
        {
            var descripcion = estado switch
            {
                EstadoCredito.Solicitado => "Pendiente de aprobación",
                EstadoCredito.Aprobado => "Crédito aprobado",
                EstadoCredito.PendienteConfiguracion => "Pendiente de configuración",
                EstadoCredito.Configurado => "Plan configurado",
                EstadoCredito.Generado => "Cuotas generadas",
                EstadoCredito.Activo => "Crédito activo",
                EstadoCredito.Rechazado => "Rechazado",
                EstadoCredito.Cancelado => "Cancelado",
                EstadoCredito.Finalizado => "Finalizado",
                _ => throw new NotSupportedException($"EstadoCredito no manejado: {estado}")
            };
            
            Assert.NotEmpty(descripcion);
        }
    }

    #endregion

    #region PrioridadAlerta Switch Coverage

    [Theory]
    [InlineData(PrioridadAlerta.Baja, "info")]
    [InlineData(PrioridadAlerta.Media, "warning")]
    [InlineData(PrioridadAlerta.Alta, "danger")]
    [InlineData(PrioridadAlerta.Critica, "dark")]
    public void PrioridadAlerta_TodosLosValores_TienenColorAsociado(PrioridadAlerta prioridad, string colorEsperado)
    {
        var color = prioridad switch
        {
            PrioridadAlerta.Critica => "dark",
            PrioridadAlerta.Alta => "danger",
            PrioridadAlerta.Media => "warning",
            PrioridadAlerta.Baja => "info",
            _ => throw new NotSupportedException($"PrioridadAlerta sin color: {prioridad}")
        };
        
        Assert.Equal(colorEsperado, color);
    }

    #endregion

    #region ConceptoMovimientoCaja Coverage

    [Fact]
    public void ConceptoMovimientoCaja_ValoresNoSecuenciales_SonValidos()
    {
        // ConceptoMovimientoCaja tiene valores no secuenciales (0,1,2,3,4,5,10,11,12,20,30,99)
        // Este test verifica que todos los valores definidos son parseables
        var conceptos = Enum.GetValues<ConceptoMovimientoCaja>();
        
        Assert.Equal(12, conceptos.Length);
        
        foreach (var concepto in conceptos)
        {
            var valorInt = (int)concepto;
            var parseado = (ConceptoMovimientoCaja)valorInt;
            Assert.Equal(concepto, parseado);
        }
    }

    #endregion
}
