using TheBuryProject.ViewModels;

namespace TheBuryProject.ViewModels
{
    /// <summary>
    /// ViewModel unificado para la vista de Cat�logo (Categor�as + Marcas)
    /// </summary>
    public class CatalogoViewModel
    {
        public IEnumerable<CategoriaViewModel> Categorias { get; set; } = new List<CategoriaViewModel>();
        public IEnumerable<MarcaViewModel> Marcas { get; set; } = new List<MarcaViewModel>();
    }
}
