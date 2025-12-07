using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels
{
    /// <summary>
    /// ViewModel para crear y editar marcas
    /// </summary>
    public class MarcaViewModel
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

        [Display(Name = "Marca Padre")]
        public int? ParentId { get; set; }

        [StringLength(100, ErrorMessage = "El pa�s de origen no puede tener m�s de 100 caracteres")]
        [Display(Name = "Pa�s de Origen")]
        public string? PaisOrigen { get; set; }

        // Para mostrar en el listado
        [Display(Name = "Nombre Marca Padre")]
        public string? ParentNombre { get; set; }
        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        /// <summary>
        /// RowVersion para control de concurrencia optimista
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}