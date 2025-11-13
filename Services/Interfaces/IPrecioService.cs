using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Services.Interfaces;

/// <summary>
/// Servicio para gestión avanzada de precios con historial
/// Soporta simulación, autorización, aplicación y reversión de cambios masivos
/// </summary>
public interface IPrecioService
{
    // ============================================
    // GESTIÓN DE LISTAS DE PRECIOS
    // ============================================

    /// <summary>
    /// Obtiene todas las listas de precios activas
    /// </summary>
    Task<List<ListaPrecio>> GetAllListasAsync(bool soloActivas = true);

    /// <summary>
    /// Obtiene una lista de precios por ID
    /// </summary>
    Task<ListaPrecio?> GetListaByIdAsync(int id);

    /// <summary>
    /// Obtiene la lista predeterminada del sistema
    /// </summary>
    Task<ListaPrecio?> GetListaPredeterminadaAsync();

    /// <summary>
    /// Crea una nueva lista de precios
    /// </summary>
    Task<ListaPrecio> CreateListaAsync(ListaPrecio lista);

    /// <summary>
    /// Actualiza una lista de precios existente
    /// </summary>
    Task<ListaPrecio> UpdateListaAsync(ListaPrecio lista);

    /// <summary>
    /// Elimina (soft delete) una lista de precios
    /// </summary>
    Task<bool> DeleteListaAsync(int id);

    // ============================================
    // CONSULTA DE PRECIOS VIGENTES
    // ============================================

    /// <summary>
    /// Obtiene el precio vigente de un producto en una lista específica
    /// </summary>
    /// <param name="productoId">ID del producto</param>
    /// <param name="listaId">ID de la lista de precios</param>
    /// <param name="fecha">Fecha para la cual obtener el precio (null = hoy)</param>
    Task<ProductoPrecioLista?> GetPrecioVigenteAsync(int productoId, int listaId, DateTime? fecha = null);

    /// <summary>
    /// Obtiene todos los precios vigentes de un producto en todas las listas
    /// </summary>
    Task<List<ProductoPrecioLista>> GetPreciosProductoAsync(int productoId, DateTime? fecha = null);

    /// <summary>
    /// Obtiene el historial completo de precios de un producto en una lista
    /// </summary>
    Task<List<ProductoPrecioLista>> GetHistorialPreciosAsync(int productoId, int listaId);

    // ============================================
    // GESTIÓN DE PRECIOS INDIVIDUALES
    // ============================================

    /// <summary>
    /// Establece un precio manual para un producto en una lista
    /// Genera una nueva vigencia
    /// </summary>
    Task<ProductoPrecioLista> SetPrecioManualAsync(
        int productoId,
        int listaId,
        decimal precio,
        decimal costo,
        DateTime? vigenciaDesde = null,
        string? notas = null);

    /// <summary>
    /// Calcula el precio automático basado en costo y reglas de la lista
    /// </summary>
    Task<decimal> CalcularPrecioAutomaticoAsync(int productoId, int listaId, decimal costo);

    // ============================================
    // CAMBIOS MASIVOS - SIMULACIÓN
    // ============================================

    /// <summary>
    /// Simula un cambio masivo de precios y retorna el batch en estado Simulado
    /// </summary>
    /// <param name="nombre">Nombre descriptivo del cambio</param>
    /// <param name="tipoCambio">Tipo de cambio a aplicar</param>
    /// <param name="tipoAplicacion">Cómo se aplica el cambio</param>
    /// <param name="valorCambio">Valor del cambio (% o absoluto)</param>
    /// <param name="listasIds">IDs de listas afectadas</param>
    /// <param name="categoriaIds">IDs de categorías afectadas (null = todas)</param>
    /// <param name="marcaIds">IDs de marcas afectadas (null = todas)</param>
    /// <param name="productoIds">IDs específicos de productos (null = por categoría/marca)</param>
    Task<PriceChangeBatch> SimularCambioMasivoAsync(
        string nombre,
        TipoCambio tipoCambio,
        TipoAplicacion tipoAplicacion,
        decimal valorCambio,
        List<int> listasIds,
        List<int>? categoriaIds = null,
        List<int>? marcaIds = null,
        List<int>? productoIds = null);

    /// <summary>
    /// Obtiene el detalle de una simulación existente
    /// </summary>
    Task<PriceChangeBatch?> GetSimulacionAsync(int batchId);

    /// <summary>
    /// Obtiene los items de una simulación con paginación
    /// </summary>
    Task<List<PriceChangeItem>> GetItemsSimulacionAsync(int batchId, int skip = 0, int take = 50);

    // ============================================
    // CAMBIOS MASIVOS - AUTORIZACIÓN
    /// ============================================

    /// <summary>
    /// Aprueba un batch de cambios de precios
    /// Cambia el estado a Aprobado
    /// </summary>
    Task<PriceChangeBatch> AprobarBatchAsync(int batchId, string aprobadoPor, string? notas = null);

    /// <summary>
    /// Rechaza un batch de cambios de precios
    /// Cambia el estado a Rechazado
    /// </summary>
    Task<PriceChangeBatch> RechazarBatchAsync(int batchId, string rechazadoPor, string motivo);

    /// <summary>
    /// Cancela un batch antes de aplicarlo
    /// </summary>
    Task<PriceChangeBatch> CancelarBatchAsync(int batchId, string canceladoPor, string? motivo = null);

    /// <summary>
    /// Verifica si un batch requiere autorización según umbrales configurados
    /// </summary>
    Task<bool> RequiereAutorizacionAsync(int batchId);

    // ============================================
    // CAMBIOS MASIVOS - APLICACIÓN
    // ============================================

    /// <summary>
    /// Aplica un batch de cambios de precios aprobado
    /// Genera nuevas vigencias para todos los productos afectados
    /// Transaccional: todo o nada
    /// </summary>
    Task<PriceChangeBatch> AplicarBatchAsync(int batchId, string aplicadoPor, DateTime? fechaVigencia = null);

    /// <summary>
    /// Revierte un batch de cambios aplicado
    /// Restaura la vigencia anterior o crea una nueva con los precios previos
    /// </summary>
    Task<PriceChangeBatch> RevertirBatchAsync(int batchId, string revertidoPor, string motivo);

    // ============================================
    // REPORTES Y ESTADÍSTICAS
    // ============================================

    /// <summary>
    /// Obtiene todos los batches con filtros
    /// </summary>
    Task<List<PriceChangeBatch>> GetBatchesAsync(
        EstadoBatch? estado = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int skip = 0,
        int take = 50);

    /// <summary>
    /// Obtiene estadísticas de un batch aplicado
    /// </summary>
    Task<Dictionary<string, object>> GetEstadisticasBatchAsync(int batchId);

    /// <summary>
    /// Exporta el historial de precios de productos a formato tabular
    /// </summary>
    Task<byte[]> ExportarHistorialPreciosAsync(List<int> productoIds, DateTime fechaDesde, DateTime fechaHasta);

    // ============================================
    // VALIDACIONES Y UTILIDADES
    // ============================================

    /// <summary>
    /// Valida que un precio cumpla con el margen mínimo configurado
    /// </summary>
    Task<(bool esValido, string? mensaje)> ValidarMargenMinimoAsync(decimal precio, decimal costo, int listaId);

    /// <summary>
    /// Calcula el margen de ganancia
    /// </summary>
    decimal CalcularMargen(decimal precio, decimal costo);

    /// <summary>
    /// Aplica redondeo según reglas de la lista
    /// </summary>
    decimal AplicarRedondeo(decimal precio, string? reglaRedondeo = null);
}