using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Services.Interfaces
{
    public interface IMovimientoStockService
    {
        Task<IEnumerable<MovimientoStock>> GetAllAsync();
        Task<MovimientoStock?> GetByIdAsync(int id);
        Task<IEnumerable<MovimientoStock>> GetByProductoIdAsync(int productoId);
        Task<IEnumerable<MovimientoStock>> GetByOrdenCompraIdAsync(int ordenCompraId);
        Task<IEnumerable<MovimientoStock>> GetByTipoAsync(TipoMovimiento tipo);
        Task<IEnumerable<MovimientoStock>> GetByFechaRangoAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<MovimientoStock> CreateAsync(MovimientoStock movimiento);
        Task<IEnumerable<MovimientoStock>> SearchAsync(
            int? productoId = null,
            TipoMovimiento? tipo = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            string? orderBy = null,
            string? orderDirection = "desc");
    }
}