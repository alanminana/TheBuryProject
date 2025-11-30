using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    public class CatalogLookupService : ICatalogLookupService
    {
        private readonly ICategoriaService _categoriaService;
        private readonly IMarcaService _marcaService;
        private readonly IProductoService _productoService;

        public CatalogLookupService(
            ICategoriaService categoriaService,
            IMarcaService marcaService,
            IProductoService productoService)
        {
            _categoriaService = categoriaService;
            _marcaService = marcaService;
            _productoService = productoService;
        }

        public async Task<(IEnumerable<Categoria> categorias, IEnumerable<Marca> marcas)> GetCategoriasYMarcasAsync()
        {
            var categoriasTask = _categoriaService.GetAllAsync();
            var marcasTask = _marcaService.GetAllAsync();

            await Task.WhenAll(categoriasTask, marcasTask);

            return (categoriasTask.Result, marcasTask.Result);
        }

        public async Task<(IEnumerable<Categoria> categorias, IEnumerable<Marca> marcas, IEnumerable<Producto> productos)> GetCategoriasMarcasYProductosAsync()
        {
            var categoriasTask = _categoriaService.GetAllAsync();
            var marcasTask = _marcaService.GetAllAsync();
            var productosTask = _productoService.GetAllAsync();

            await Task.WhenAll(categoriasTask, marcasTask, productosTask);

            return (categoriasTask.Result, marcasTask.Result, productosTask.Result);
        }
    }
}
