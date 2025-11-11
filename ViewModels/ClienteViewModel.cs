using System.ComponentModel.DataAnnotations;

namespace TheBuryProject.ViewModels
{
    public class ClienteViewModel
    {
        public int Id { get; set; }

        // Datos Personales
        [Required(ErrorMessage = "El tipo de documento es requerido")]
        [Display(Name = "Tipo de Documento")]
        public string TipoDocumento { get; set; } = "DNI";

        [Required(ErrorMessage = "El número de documento es requerido")]
        [Display(Name = "Número de Documento")]
        [StringLength(20)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido")]
        [Display(Name = "Apellido")]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Nombre Completo")]
        public string? NombreCompleto { get; set; }

        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        [Display(Name = "Edad")]
        public int? Edad { get; set; }

        [Display(Name = "Estado Civil")]
        public string? EstadoCivil { get; set; }

        // Datos de Contacto
        [Required(ErrorMessage = "El teléfono es requerido")]
        [Display(Name = "Teléfono")]
        [Phone]
        public string Telefono { get; set; } = string.Empty;

        [Display(Name = "Teléfono Alternativo")]
        [Phone]
        public string? TelefonoAlternativo { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        public string? Email { get; set; }

        // Domicilio
        [Required(ErrorMessage = "El domicilio es requerido")]
        [Display(Name = "Domicilio")]
        [StringLength(200)]
        public string Domicilio { get; set; } = string.Empty;

        // Alias para compatibilidad con vistas
        public string? Direccion => Domicilio;

        [Display(Name = "Localidad")]
        public string? Localidad { get; set; }

        [Display(Name = "Provincia")]
        public string? Provincia { get; set; }

        [Display(Name = "Código Postal")]
        public string? CodigoPostal { get; set; }

        // Datos Laborales
        [Display(Name = "Empleador")]
        public string? Empleador { get; set; }

        // Alias para compatibilidad con vistas
        public string? LugarTrabajo => Empleador;

        [Display(Name = "Tipo de Empleo")]
        public string? TipoEmpleo { get; set; }

        [Display(Name = "Sueldo")]
        [DataType(DataType.Currency)]
        public decimal? Sueldo { get; set; }

        // Alias para compatibilidad con vistas
        public decimal? IngresoMensual => Sueldo;

        [Display(Name = "Tiempo en el Trabajo")]
        public string? TiempoTrabajo { get; set; }

        [Display(Name = "Teléfono Laboral")]
        [Phone]
        public string? TelefonoLaboral { get; set; }

        // Alias para compatibilidad con vistas
        public string? TelefonoTrabajo => TelefonoLaboral;

        // Documentación
        [Display(Name = "Tiene Recibo de Sueldo")]
        public bool TieneReciboSueldo { get; set; }

        [Display(Name = "Tiene Veraz")]
        public bool TieneVeraz { get; set; }

        [Display(Name = "Tiene Impuesto")]
        public bool TieneImpuesto { get; set; }

        [Display(Name = "Tiene Servicio de Luz")]
        public bool TieneServicioLuz { get; set; }

        [Display(Name = "Tiene Servicio de Gas")]
        public bool TieneServicioGas { get; set; }

        [Display(Name = "Tiene Servicio de Agua")]
        public bool TieneServicioAgua { get; set; }

        // Control
        [Display(Name = "Puntaje de Riesgo")]
        public decimal PuntajeRiesgo { get; set; } = 5.0m;

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Observaciones")]
        [DataType(DataType.MultilineText)]
        public string? Observaciones { get; set; }

        // Info adicional - Historial Crediticio
        [Display(Name = "Créditos Activos")]
        public int CreditosActivos { get; set; }

        [Display(Name = "Créditos Totales")]
        public int CreditosTotales { get; set; }

        [Display(Name = "Cuotas Impagas")]
        public int CuotasImpagas { get; set; }

        [Display(Name = "Total Adeudado")]
        [DataType(DataType.Currency)]
        public decimal TotalAdeudado { get; set; }

        // Alias para compatibilidad con vistas
        public decimal? MontoAdeudado => TotalAdeudado;

        // Garante
        public int? GaranteId { get; set; }

        // Auditoría
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}