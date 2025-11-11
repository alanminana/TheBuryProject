using System.ComponentModel.DataAnnotations;
using TheBuryProject.Models.Base;

namespace TheBuryProject.Models.Entities
{
    /// <summary>
    /// Representa un cliente del sistema que puede solicitar cr�ditos
    /// </summary>
    public class Cliente : BaseEntity
    {
        // Datos Personales
        [Required]
        [StringLength(20)]
        public string TipoDocumento { get; set; } = "DNI"; // DNI, CUIL, CUIT

        [Required]
        [StringLength(20)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? NombreCompleto { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        [StringLength(50)]
        public string? EstadoCivil { get; set; } // Soltero, Casado, Divorciado, Viudo

        // Datos de Contacto
        [Required]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TelefonoAlternativo { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        // Domicilio
        [Required]
        [StringLength(200)]
        public string Domicilio { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Localidad { get; set; }

        [StringLength(100)]
        public string? Provincia { get; set; }

        [StringLength(10)]
        public string? CodigoPostal { get; set; }

        // Datos Laborales
        [StringLength(200)]
        public string? Empleador { get; set; }

        [StringLength(100)]
        public string? TipoEmpleo { get; set; } // Relaci�n de dependencia, Aut�nomo, Monotributista

        public decimal? Sueldo { get; set; }

        [StringLength(20)]
        public string? TelefonoLaboral { get; set; }

        // Documentaci�n
        public bool TieneReciboSueldo { get; set; } = false;
        public bool TieneVeraz { get; set; } = false;
        public bool TieneImpuesto { get; set; } = false;
        public bool TieneServicioLuz { get; set; } = false;
        public bool TieneServicioGas { get; set; } = false;
        public bool TieneServicioAgua { get; set; } = false;

        // Control de Riesgo
        public decimal PuntajeRiesgo { get; set; } = 5.0m; // 0 a 10 (5 = neutro)

        // Estado
        public bool Activo { get; set; } = true;

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Garante asociado (si el cliente tiene un garante asignado)
        public int? GaranteId { get; set; }

        // Navigation Properties
        public virtual Garante? Garante { get; set; }
        public virtual ICollection<Credito> Creditos { get; set; } = new List<Credito>();
        public virtual ICollection<Garante> ComoGarante { get; set; } = new List<Garante>();
    }
}