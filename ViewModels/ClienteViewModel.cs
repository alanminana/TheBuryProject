using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels
{
    /// <summary>
    /// ViewModel para crear y editar clientes
    /// NOTA: Los checkboxes de documentación fueron eliminados
    /// La documentación se sube a través de la sección de Documentación
    /// </summary>
    public class ClienteViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de documento es requerido")]
        public string TipoDocumento { get; set; } = "DNI";

        [Required(ErrorMessage = "El número de documento es requerido")]
        [StringLength(20)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido")]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public string? NombreCompleto { get; set; }
        public int? Edad { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        public string? EstadoCivil { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TelefonoAlternativo { get; set; }

        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "El domicilio es requerido")]
        [StringLength(200)]
        public string Domicilio { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Localidad { get; set; }

        [StringLength(100)]
        public string? Provincia { get; set; }

        [StringLength(10)]
        public string? CodigoPostal { get; set; }

        // ✅ DATOS LABORALES - PROPIEDADES REALES
        [StringLength(200)]
        public string? Empleador { get; set; }

        [StringLength(100)]
        public string? TipoEmpleo { get; set; }

        [Range(0, 999999999.99)]
        public decimal? Sueldo { get; set; }

        [StringLength(20)]
        public string? TelefonoLaboral { get; set; }

        /// <summary>
        /// Indica si el cliente presentó recibo de sueldo (compatibilidad con la entidad)
        /// </summary>
        public bool TieneReciboSueldo { get; set; } = false;

        [StringLength(50)]
        public string? TiempoTrabajo { get; set; }

        // ✅ PROPIEDADES DE CONTROL
        public decimal PuntajeRiesgo { get; set; } = 5.0m;

        public bool Activo { get; set; } = true;

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // ✅ GARANTE
        public int? GaranteId { get; set; }

        // ✅ HISTORIAL CREDITICIO (SOLO LECTURA)
        public int CreditosTotales { get; set; }
        public int CreditosActivos { get; set; }
        public int CuotasImpagas { get; set; }
        public decimal? MontoAdeudado { get; set; }

        // ALIASES PARA COMPATIBILIDAD CON CREATE.CSHTML
        // (que usa LugarTrabajo, IngresoMensual, TelefonoTrabajo)
        public string? LugarTrabajo 
        { 
            get => Empleador; 
            set => Empleador = value; 
        }

        public decimal? IngresoMensual 
        { 
            get => Sueldo; 
            set => Sueldo = value; 
        }

        public string? TelefonoTrabajo 
        { 
            get => TelefonoLaboral; 
            set => TelefonoLaboral = value; 
        }

        // Alias para Domicilio
        public string? Direccion => Domicilio;
    }
}