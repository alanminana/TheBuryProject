using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    public class DocumentoClienteService : IDocumentoClienteService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentoClienteService> _logger;
        private readonly IWebHostEnvironment _environment;
        private const string UPLOAD_FOLDER = "uploads/documentos-clientes";

        public DocumentoClienteService(
            AppDbContext context,
            IMapper mapper,
            ILogger<DocumentoClienteService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _environment = environment;
        }

        public async Task<List<DocumentoClienteViewModel>> GetAllAsync()
        {
            var documentos = await _context.Set<DocumentoCliente>()
                .Include(d => d.Cliente)
                .Where(d => !d.IsDeleted)
                .OrderByDescending(d => d.FechaSubida)
                .ToListAsync();

            return _mapper.Map<List<DocumentoClienteViewModel>>(documentos);
        }

        public async Task<DocumentoClienteViewModel?> GetByIdAsync(int id)
        {
            var documento = await _context.Set<DocumentoCliente>()
                .Include(d => d.Cliente)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            return documento != null ? _mapper.Map<DocumentoClienteViewModel>(documento) : null;
        }

        public async Task<List<DocumentoClienteViewModel>> GetByClienteIdAsync(int clienteId)
        {
            var documentos = await _context.Set<DocumentoCliente>()
                .Include(d => d.Cliente)
                .Where(d => d.ClienteId == clienteId && !d.IsDeleted)
                .OrderByDescending(d => d.FechaSubida)
                .ToListAsync();

            return _mapper.Map<List<DocumentoClienteViewModel>>(documentos);
        }

        public async Task<DocumentoClienteViewModel> UploadAsync(DocumentoClienteViewModel viewModel)
        {
            try
            {
                if (viewModel.Archivo == null || viewModel.Archivo.Length == 0)
                {
                    throw new Exception("Debe seleccionar un archivo");
                }

                // Validar tamaño (máximo 5MB)
                if (viewModel.Archivo.Length > 5 * 1024 * 1024)
                {
                    throw new Exception("El archivo no puede superar los 5 MB");
                }

                // Validar extensión
                var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                var extension = Path.GetExtension(viewModel.Archivo.FileName).ToLowerInvariant();
                if (!extensionesPermitidas.Contains(extension))
                {
                    throw new Exception($"Extensión no permitida. Solo se aceptan: {string.Join(", ", extensionesPermitidas)}");
                }

                // Crear carpeta si no existe
                var uploadPath = Path.Combine(_environment.WebRootPath, UPLOAD_FOLDER);
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Generar nombre único
                var nombreArchivo = $"{viewModel.ClienteId}_{viewModel.TipoDocumento}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var rutaCompleta = Path.Combine(uploadPath, nombreArchivo);

                // Guardar archivo
                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await viewModel.Archivo.CopyToAsync(stream);
                }

                _logger.LogInformation("Archivo guardado: {Ruta}", rutaCompleta);

                // Crear entidad
                var documento = new DocumentoCliente
                {
                    ClienteId = viewModel.ClienteId,
                    TipoDocumento = viewModel.TipoDocumento,
                    NombreArchivo = viewModel.Archivo.FileName,
                    RutaArchivo = Path.Combine(UPLOAD_FOLDER, nombreArchivo),
                    TipoMIME = viewModel.Archivo.ContentType,
                    TamanoBytes = viewModel.Archivo.Length,
                    Estado = EstadoDocumento.Pendiente,
                    FechaSubida = DateTime.Now,
                    FechaVencimiento = viewModel.FechaVencimiento,
                    Observaciones = viewModel.Observaciones
                };

                _context.Set<DocumentoCliente>().Add(documento);
                await _context.SaveChangesAsync();

                viewModel.Id = documento.Id;
                viewModel.NombreArchivo = documento.NombreArchivo;
                viewModel.RutaArchivo = documento.RutaArchivo;
                viewModel.TamanoBytes = documento.TamanoBytes;
                viewModel.FechaSubida = documento.FechaSubida;
                viewModel.Estado = documento.Estado;

                _logger.LogInformation("Documento {Id} subido exitosamente para cliente {ClienteId}", documento.Id, viewModel.ClienteId);

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir documento");
                throw;
            }
        }

        public async Task<bool> VerificarAsync(int id, string verificadoPor, string? observaciones = null)
        {
            try
            {
                var documento = await _context.Set<DocumentoCliente>()
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (documento == null)
                    return false;

                documento.Estado = EstadoDocumento.Verificado;
                documento.FechaVerificacion = DateTime.Now;
                documento.VerificadoPor = verificadoPor;
                if (!string.IsNullOrEmpty(observaciones))
                    documento.Observaciones = observaciones;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Documento {Id} verificado por {Usuario}", id, verificadoPor);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar documento {Id}", id);
                throw;
            }
        }

        public async Task<bool> RechazarAsync(int id, string motivo, string rechazadoPor)
        {
            try
            {
                var documento = await _context.Set<DocumentoCliente>()
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (documento == null)
                    return false;

                documento.Estado = EstadoDocumento.Rechazado;
                documento.FechaVerificacion = DateTime.Now;
                documento.VerificadoPor = rechazadoPor;
                documento.MotivoRechazo = motivo;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Documento {Id} rechazado por {Usuario}. Motivo: {Motivo}", id, rechazadoPor, motivo);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar documento {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var documento = await _context.Set<DocumentoCliente>()
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (documento == null)
                    return false;

                // Soft delete
                documento.IsDeleted = true;
                await _context.SaveChangesAsync();

                // Opcional: eliminar archivo físico
                var rutaCompleta = Path.Combine(_environment.WebRootPath, documento.RutaArchivo);
                if (File.Exists(rutaCompleta))
                {
                    File.Delete(rutaCompleta);
                    _logger.LogInformation("Archivo físico eliminado: {Ruta}", rutaCompleta);
                }

                _logger.LogInformation("Documento {Id} eliminado", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar documento {Id}", id);
                throw;
            }
        }

        public async Task<byte[]> DescargarArchivoAsync(int id)
        {
            try
            {
                var documento = await _context.Set<DocumentoCliente>()
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (documento == null)
                    throw new Exception("Documento no encontrado");

                var rutaCompleta = Path.Combine(_environment.WebRootPath, documento.RutaArchivo);

                if (!File.Exists(rutaCompleta))
                    throw new Exception("Archivo no encontrado en el servidor");

                return await File.ReadAllBytesAsync(rutaCompleta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar documento {Id}", id);
                throw;
            }
        }

        public async Task<List<DocumentoClienteViewModel>> BuscarAsync(DocumentoClienteFilterViewModel filtro)
        {
            var query = _context.Set<DocumentoCliente>()
                .Include(d => d.Cliente)
                .Where(d => !d.IsDeleted)
                .AsQueryable();

            if (filtro.ClienteId.HasValue)
                query = query.Where(d => d.ClienteId == filtro.ClienteId.Value);

            if (filtro.TipoDocumento.HasValue)
                query = query.Where(d => d.TipoDocumento == filtro.TipoDocumento.Value);

            if (filtro.Estado.HasValue)
                query = query.Where(d => d.Estado == filtro.Estado.Value);

            if (filtro.SoloPendientes)
                query = query.Where(d => d.Estado == EstadoDocumento.Pendiente);

            if (filtro.SoloVencidos)
                query = query.Where(d => d.Estado == EstadoDocumento.Vencido ||
                                        (d.FechaVencimiento.HasValue && d.FechaVencimiento.Value < DateTime.Today));

            var documentos = await query
                .OrderByDescending(d => d.FechaSubida)
                .ToListAsync();

            filtro.TotalResultados = documentos.Count;

            return _mapper.Map<List<DocumentoClienteViewModel>>(documentos);
        }

        public async Task MarcarVencidosAsync()
        {
            try
            {
                var documentosVencidos = await _context.Set<DocumentoCliente>()
                    .Where(d => !d.IsDeleted
                             && d.Estado == EstadoDocumento.Verificado
                             && d.FechaVencimiento.HasValue
                             && d.FechaVencimiento.Value < DateTime.Today)
                    .ToListAsync();

                foreach (var doc in documentosVencidos)
                {
                    doc.Estado = EstadoDocumento.Vencido;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Se marcaron {Count} documentos como vencidos", documentosVencidos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar documentos vencidos");
                throw;
            }
        }
    }
}