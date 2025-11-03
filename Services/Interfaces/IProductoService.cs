using TheBuryProject.Models.Entities;

namespace TheBuryProject.Services.Interfaces
{
    public interface IProductoService
    {
        Task<IEnumerable<Producto>> GetAllAsync();
        Task<Producto?> GetByIdAsync(int id);
        Task<IEnumerable<Producto>> GetByCategoriaAsync(int categoriaId);
        Task<IEnumerable<Producto>> GetByMarcaAsync(int marcaId);
        Task<IEnumerable<Producto>> GetProductosConStockBajoAsync();
        Task<Producto> CreateAsync(Producto producto);
        Task<Producto> UpdateAsync(Producto producto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null);
        Task<Producto> ActualizarStockAsync(int id, decimal cantidad);
        /// <summary>
        /// Busca y filtra productos según los criterios especificados
        /// </summary>
        Task<IEnumerable<Producto>> SearchAsync(
            string? searchTerm = null,
            int? categoriaId = null,
            int? marcaId = null,
            bool stockBajo = false,
            bool soloActivos = false,
            string? orderBy = null,
            string? orderDirection = "asc");
    }
}