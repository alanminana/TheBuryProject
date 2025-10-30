using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels
{
    /// <summary>
    /// ViewModel para crear y editar categor�as
    /// </summary>
    public class CategoriaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El c�digo es obligatorio")]
        [StringLength(20, ErrorMessage = "El c�digo no puede tener m�s de 20 caracteres")]
        [Display(Name = "C�digo")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener m�s de 100 caracteres")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripci�n no puede tener m�s de 500 caracteres")]
        [Display(Name = "Descripci�n")]
        public string? Descripcion { get; set; }

        [Display(Name = "Categor�a Padre")]
        public int? ParentId { get; set; }

        [Display(Name = "Control de Serie por Defecto")]
        public bool ControlSerieDefault { get; set; }

        // Para el dropdown de categor�as padre
        [Display(Name = "Nombre Categor�a Padre")]
        public string? ParentNombre { get; set; }
    }
}