using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.ViewModels
{
    public class DocumentoClienteViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Cliente")]
        [Required(ErrorMessage = "Debe seleccionar un cliente")]
        public int ClienteId { get; set; }
        public string? ClienteNombre { get; set; }

        [Display(Name = "Tipo de Documento")]
        [Required(ErrorMessage = "Debe seleccionar el tipo de documento")]
        public TipoDocumentoCliente TipoDocumento { get; set; }

        public string TipoDocumentoNombre => TipoDocumento switch
        {
            TipoDocumentoCliente.DNI => "DNI",
            TipoDocumentoCliente.ReciboSueldo => "Recibo de Sueldo",
            TipoDocumentoCliente.ServicioLuz => "Servicio de Luz",
            TipoDocumentoCliente.ServicioGas => "Servicio de Gas",
            TipoDocumentoCliente.ServicioAgua => "Servicio de Agua",
            TipoDocumentoCliente.ConstanciaCUIL => "Constancia CUIL",
            TipoDocumentoCliente.DeclaracionJurada => "Declaración Jurada",
            TipoDocumentoCliente.Veraz => "Veraz",
            TipoDocumentoCliente.Otro => "Otro",
            _ => "Desconocido"
        };

        [Display(Name = "Archivo")]
        public IFormFile? Archivo { get; set; }

        public string? NombreArchivo { get; set; }
        public string? RutaArchivo { get; set; }
        public string? TipoMIME { get; set; }
        public long TamanoBytes { get; set; }
        public string TamanoFormateado => TamanoBytes > 1024 * 1024
            ? $"{TamanoBytes / (1024.0 * 1024.0):N2} MB"
            : $"{TamanoBytes / 1024.0:N2} KB";

        [Display(Name = "Estado")]
        public EstadoDocumento Estado { get; set; }

        public string EstadoNombre => Estado switch
        {
            EstadoDocumento.Pendiente => "Pendiente",
            EstadoDocumento.Verificado => "Verificado",
            EstadoDocumento.Rechazado => "Rechazado",
            EstadoDocumento.Vencido => "Vencido",
            _ => "Desconocido"
        };

        public string EstadoColor => Estado switch
        {
            EstadoDocumento.Pendiente => "warning",
            EstadoDocumento.Verificado => "success",
            EstadoDocumento.Rechazado => "danger",
            EstadoDocumento.Vencido => "secondary",
            _ => "secondary"
        };

        public string EstadoIcono => Estado switch
        {
            EstadoDocumento.Pendiente => "bi-clock",
            EstadoDocumento.Verificado => "bi-check-circle-fill",
            EstadoDocumento.Rechazado => "bi-x-circle-fill",
            EstadoDocumento.Vencido => "bi-exclamation-triangle",
            _ => "bi-question-circle"
        };

        [Display(Name = "Fecha de Subida")]
        public DateTime FechaSubida { get; set; }

        [Display(Name = "Fecha de Vencimiento")]
        [DataType(DataType.Date)]
        public DateTime? FechaVencimiento { get; set; }

        [Display(Name = "Fecha de Verificación")]
        public DateTime? FechaVerificacion { get; set; }

        [Display(Name = "Verificado Por")]
        public string? VerificadoPor { get; set; }

        [Display(Name = "Observaciones")]
        [DataType(DataType.MultilineText)]
        public string? Observaciones { get; set; }

        [Display(Name = "Motivo de Rechazo")]
        [DataType(DataType.MultilineText)]
        public string? MotivoRechazo { get; set; }

        // Para filtros
        public bool? SoloPendientes { get; set; }
        public bool? SoloVencidos { get; set; }
    }

    public class DocumentoClienteFilterViewModel
    {
        public int? ClienteId { get; set; }
        public string? Cliente { get; set; }
        public TipoDocumentoCliente? TipoDocumento { get; set; }
        public EstadoDocumento? Estado { get; set; }
        public bool SoloPendientes { get; set; }
        public bool SoloVencidos { get; set; }
        public int TotalResultados { get; set; }
        public List<DocumentoClienteViewModel> Documentos { get; set; } = new();
    }
}