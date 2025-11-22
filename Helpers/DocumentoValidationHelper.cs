using System.Security;

namespace TheBuryProject.Helpers
{
    /// <summary>
    /// Helper consolidado para validación de documentos de clientes
    /// Incluye: validación de archivos, normalización de rutas, y prevención de seguridad
    /// </summary>
    public static class DocumentoValidationHelper
    {
        // Configuración
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] VALID_EXTENSIONS = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        private static readonly Dictionary<string, string[]> VALID_MIME_TYPES = new()
        {
            { ".pdf", new[] { "application/pdf" } },
            { ".jpg", new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png", new[] { "image/png" } },
            { ".doc", new[] { "application/msword" } },
            { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } }
        };

        /// <summary>
        /// Valida un archivo de entrada verificando tamaño, extensión y MIME type
        /// </summary>
        /// <param name="archivo">Archivo a validar</param>
        /// <returns>Tupla (isValid, errorMessage)</returns>
        public static (bool IsValid, string ErrorMessage) ValidateFile(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return (false, "Debe seleccionar un archivo");

            // Validar tamaño
            if (archivo.Length > MAX_FILE_SIZE)
                return (false, $"Archivo no puede superar {MAX_FILE_SIZE / (1024 * 1024)} MB");

            // Validar extensión
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!VALID_EXTENSIONS.Contains(extension))
                return (false, $"Extensión no permitida. Formatos válidos: {string.Join(", ", VALID_EXTENSIONS)}");

            // Validar MIME type
            if (VALID_MIME_TYPES.TryGetValue(extension, out var validMimes))
            {
                if (!validMimes.Contains(archivo.ContentType))
                    return (false, $"Tipo de archivo inválido: {archivo.ContentType}");
            }

            // Validar magic bytes (primeros 4 bytes del archivo)
            if (!ValidateMagicBytes(archivo, extension))
                return (false, $"Archivo corrupto o falsificado: el contenido no coincide con la extensión {extension}");

            return (true, string.Empty);
        }

        /// <summary>
        /// Normaliza una ruta de archivo para prevenir path traversal attacks
        /// </summary>
        /// <param name="basePath">Ruta base segura</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Tupla (isValid, fullPath, errorMessage)</returns>
        public static (bool IsValid, string FullPath, string ErrorMessage) NormalizePath(string basePath, string fileName)
        {
            try
            {
                var fullPath = Path.Combine(basePath, fileName);
                var normalizedPath = Path.GetFullPath(fullPath);
                var normalizedBase = Path.GetFullPath(basePath);

                // Verificar que la ruta normalizada está dentro de basePath
                if (!normalizedPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
                    return (false, string.Empty, "Intento de path traversal detectado");

                return (true, normalizedPath, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error al normalizar ruta: {ex.Message}");
            }
        }

        /// <summary>
        /// Valida los magic bytes del archivo para verificar su tipo real
        /// </summary>
        private static bool ValidateMagicBytes(IFormFile archivo, string extension)
        {
            try
            {
                using var stream = archivo.OpenReadStream();
                var buffer = new byte[4];
                stream.Read(buffer, 0, 4);

                return extension.ToLowerInvariant() switch
                {
                    ".pdf" => buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46, // %PDF
                    ".jpg" or ".jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF, // FFD8FF
                    ".png" => buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47, // 89504E47
                    ".doc" => buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0, // D0CF11E0
                    ".docx" => buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04, // PK (ZIP)
                    _ => true // Extensiones no reconocidas se permiten
                };
            }
            catch
            {
                // Si no se pueden leer los magic bytes, permitir (podría ser un archivo pequeño)
                return true;
            }
        }

        /// <summary>
        /// Formatea el tamaño de un archivo en bytes a formato legible
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024.0:F2} KB",
                < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F2} MB",
                _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
            };
        }
    }
}