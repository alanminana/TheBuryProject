using TheBuryProject.Models.Entities;

namespace TheBuryProject.Services.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de gestión de productos
    /// </summary>
    public interface IProductoService
    {
        /// <summary>
        /// Obtiene todos los productos activos (no eliminados)
        /// </summary>
        /// <returns>Lista de productos con sus relaciones de Categoría y Marca</returns>
        Task<IEnumerable<Producto>> GetAllAsync();

        /// <summary>
        /// Obtiene un producto por su ID
        /// </summary>
        /// <param name="id">ID del producto</param>
        /// <returns>El producto con sus relaciones o null si no existe</returns>
        Task<Producto?> GetByIdAsync(int id);

        /// <summary>
        /// Obtiene productos por categoría
        /// </summary>
        /// <param name="categoriaId">ID de la categoría</param>
        /// <returns>Lista de productos de la categoría especificada</returns>
        Task<IEnumerable<Producto>> GetByCategoriaAsync(int categoriaId);

        /// <summary>
        /// Obtiene productos por marca
        /// </summary>
        /// <param name="marcaId">ID de la marca</param>
        /// <returns>Lista de productos de la marca especificada</returns>
        Task<IEnumerable<Producto>> GetByMarcaAsync(int marcaId);

        /// <summary>
        /// Obtiene productos con stock bajo (stock actual <= stock mínimo)
        /// </summary>
        /// <returns>Lista de productos con stock bajo</returns>
        Task<IEnumerable<Producto>> GetProductosConStockBajoAsync();

        /// <summary>
        /// Crea un nuevo producto
        /// </summary>
        /// <param name="producto">Producto a crear</param>
        /// <returns>El producto creado</returns>
        /// <exception cref="InvalidOperationException">Si el código ya existe</exception>
        Task<Producto> CreateAsync(Producto producto);

        /// <summary>
        /// Actualiza un producto existente
        /// </summary>
        /// <param name="producto">Producto con los datos actualizados</param>
        /// <returns>El producto actualizado</returns>
        /// <exception cref="InvalidOperationException">Si el código ya existe en otro producto</exception>
        Task<Producto> UpdateAsync(Producto producto);

        /// <summary>
        /// Elimina un producto (soft delete)
        /// </summary>
        /// <param name="id">ID del producto a eliminar</param>
        /// <returns>True si se eliminó correctamente, false si no se encontró</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Verifica si existe un código de producto
        /// </summary>
        /// <param name="codigo">Código a verificar</param>
        /// <param name="excludeId">ID del producto a excluir (para validación en edición)</param>
        /// <returns>True si el código ya existe, false en caso contrario</returns>
        Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null);

        /// <summary>
        /// Actualiza el stock de un producto
        /// </summary>
        /// <param name="id">ID del producto</param>
        /// <param name="cantidad">Cantidad a sumar o restar (positivo suma, negativo resta)</param>
        /// <returns>El producto actualizado</returns>
        /// <exception cref="InvalidOperationException">Si el stock resultante es negativo</exception>
        Task<Producto> ActualizarStockAsync(int id, decimal cantidad);
    }
}