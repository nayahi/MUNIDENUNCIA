using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MUNIDENUNCIA.Services
{
    /// <summary>
    /// Implementación del servicio de gestión segura de archivos PDF
    /// SEMANA 4: Manejo Seguro de Archivos y Uploads
    /// 
    /// Este servicio implementa las OWASP Top 10 mejores prácticas para uploads:
    /// 
    /// 1. VALIDACIÓN DE TIPO DE ARCHIVO
    ///    - Verifica extensión del archivo
    ///    - Verifica tipo MIME declarado
    ///    - Verifica firma de archivo (magic numbers) para PDFs
    /// 
    /// 2. LIMITACIÓN DE TAMAÑO
    ///    - Máximo 5MB por archivo
    ///    - Previene ataques de denegación de servicio
    /// 
    /// 3. NOMBRES DE ARCHIVO SEGUROS
    ///    - Genera GUIDs aleatorios
    ///    - Previene directory traversal (../, ..\)
    ///    - Previene path injection
    /// 
    /// 4. ALMACENAMIENTO SEGURO
    ///    - Directorio controlado fuera de wwwroot
    ///    - Sin permisos de ejecución
    ///    - Estructura de carpetas organizada
    /// 
    /// 5. PREVENCIÓN DE EJECUCIÓN
    ///    - Solo permite extensión .pdf
    ///    - No permite ejecutables disfrazados
    ///    - Content-Type correcto al servir archivos
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;

        // Configuración de seguridad
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private const string ALLOWED_EXTENSION = ".pdf";
        private const string ALLOWED_MIME_TYPE = "application/pdf";
        private const string UPLOADS_FOLDER = "uploads";

        /// <summary>
        /// Firma mágica (magic numbers) de archivos PDF
        /// Los archivos PDF siempre comienzan con estos bytes: %PDF
        /// </summary>
        private static readonly byte[] PDF_SIGNATURE = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Valida un archivo PDF según múltiples criterios de seguridad
        /// </summary>
        public async Task<FileValidationResult> ValidatePdfFileAsync(IFormFile file)
        {
            // ====================================================================
            // VALIDACIÓN 1: Archivo no nulo y con contenido
            // ====================================================================
            if (file == null || file.Length == 0)
            {
                return FileValidationResult.Failure(
                    "No se ha seleccionado ningún archivo o el archivo está vacío");
            }

            // ====================================================================
            // VALIDACIÓN 2: Tamaño máximo (5MB)
            // ====================================================================
            if (file.Length > MAX_FILE_SIZE)
            {
                return FileValidationResult.Failure(
                    $"El archivo excede el tamaño máximo permitido de 5MB. " +
                    $"Tamaño actual: {file.Length / 1024.0 / 1024.0:F2}MB");
            }

            // ====================================================================
            // VALIDACIÓN 3: Extensión del archivo
            // ====================================================================
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (string.IsNullOrEmpty(extension) || extension != ALLOWED_EXTENSION)
            {
                return FileValidationResult.Failure(
                    $"Solo se permiten archivos PDF. Extensión detectada: {extension}");
            }

            // ====================================================================
            // VALIDACIÓN 4: Tipo MIME declarado
            // ====================================================================
            if (file.ContentType != ALLOWED_MIME_TYPE)
            {
                return FileValidationResult.Failure(
                    $"El tipo de contenido no es válido. " +
                    $"Se esperaba: {ALLOWED_MIME_TYPE}, " +
                    $"Se recibió: {file.ContentType}");
            }

            // ====================================================================
            // VALIDACIÓN 5: Firma del archivo (magic numbers)
            // Esta es la validación más importante - verifica que el archivo
            // sea REALMENTE un PDF y no un ejecutable renombrado
            // ====================================================================
            using (var stream = file.OpenReadStream())
            {
                byte[] headerBytes = new byte[4];
                await stream.ReadAsync(headerBytes, 0, 4);

                if (!headerBytes.SequenceEqual(PDF_SIGNATURE))
                {
                    return FileValidationResult.Failure(
                        "El archivo no es un PDF válido. La firma del archivo no coincide.");
                }
            }

            // ====================================================================
            // VALIDACIÓN 6: Nombre de archivo seguro
            // Verifica que el nombre no contenga caracteres peligrosos
            // ====================================================================
            string fileName = Path.GetFileName(file.FileName);
            
            // Caracteres peligrosos que indican path traversal
            char[] dangerousChars = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
            
            if (fileName.IndexOfAny(dangerousChars) >= 0)
            {
                return FileValidationResult.Failure(
                    "El nombre del archivo contiene caracteres no permitidos");
            }

            // ====================================================================
            // Todas las validaciones pasaron
            // ====================================================================
            return FileValidationResult.Success();
        }

        /// <summary>
        /// Guarda un archivo PDF de forma segura en el servidor
        /// </summary>
        public async Task<FileUploadResult> SavePdfFileAsync(IFormFile file, string subfolder)
        {
            // Primero validar el archivo
            var validationResult = await ValidatePdfFileAsync(file);
            
            if (!validationResult.IsValid)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = validationResult.ErrorMessage
                };
            }

            try
            {
                // ================================================================
                // PASO 1: Crear estructura de directorios segura
                // ================================================================
                
                // Directorio base: /uploads
                string uploadsBasePath = Path.Combine(_environment.ContentRootPath, UPLOADS_FOLDER);
                
                // Subdirectorio específico: /uploads/denuncias
                string targetDirectory = Path.Combine(uploadsBasePath, subfolder);
                
                // Crear directorios si no existen
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                // ================================================================
                // PASO 2: Generar nombre de archivo seguro (GUID)
                // ================================================================
                
                // IMPORTANTE: Nunca usar el nombre original del usuario directamente
                // Razones:
                // - Previene directory traversal: ../../etc/passwd
                // - Previene colisiones de nombres
                // - Previene inyección de path
                // - Oculta información sensible del nombre original
                
                string serverFileName = $"{Guid.NewGuid()}{ALLOWED_EXTENSION}";
                string physicalPath = Path.Combine(targetDirectory, serverFileName);

                // ================================================================
                // PASO 3: Guardar archivo en disco
                // ================================================================
                
                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // ================================================================
                // PASO 4: Construir ruta relativa para almacenar en BD
                // ================================================================
                
                // Ejemplo: "uploads/denuncias/abc123-def456.pdf"
                string relativePath = Path.Combine(UPLOADS_FOLDER, subfolder, serverFileName)
                    .Replace("\\", "/"); // Normalizar a forward slashes

                // ================================================================
                // PASO 5: Retornar resultado exitoso
                // ================================================================
                
                return new FileUploadResult
                {
                    Success = true,
                    OriginalFileName = file.FileName,
                    ServerFileName = serverFileName,
                    RelativePath = relativePath,
                    PhysicalPath = physicalPath,
                    FileSizeBytes = file.Length,
                    MimeType = file.ContentType
                };
            }
            catch (Exception ex)
            {
                // En producción, registrar el error detallado en logs
                // Pero al usuario solo mostrar mensaje genérico
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "Error al guardar el archivo. Por favor intente nuevamente."
                };
            }
        }

        /// <summary>
        /// Elimina un archivo del servidor de forma segura
        /// </summary>
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                // Validar que la ruta no esté vacía
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                // Obtener ruta física
                string physicalPath = GetPhysicalPath(filePath);

                // Verificar que el archivo existe
                if (!File.Exists(physicalPath))
                {
                    return false; // Ya no existe, considerar éxito
                }

                // ============================================================
                // VALIDACIÓN DE SEGURIDAD CRÍTICA
                // Verificar que el archivo está dentro del directorio permitido
                // Esto previene eliminación de archivos del sistema
                // ============================================================
                
                string uploadsBasePath = Path.Combine(_environment.ContentRootPath, UPLOADS_FOLDER);
                string normalizedPhysicalPath = Path.GetFullPath(physicalPath);
                string normalizedBasePath = Path.GetFullPath(uploadsBasePath);

                if (!normalizedPhysicalPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
                {
                    // Intento de eliminación fuera del directorio permitido
                    throw new InvalidOperationException("Intento de eliminación de archivo no autorizado");
                }

                // Eliminar archivo
                File.Delete(physicalPath);
                
                return true;
            }
            catch (Exception ex)
            {
                // En producción, registrar el error
                return false;
            }
        }

        /// <summary>
        /// Obtiene la ruta física completa de un archivo
        /// </summary>
        public string GetPhysicalPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            // Normalizar la ruta (convertir / a \ en Windows)
            string normalizedPath = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString());
            
            // Combinar con la raíz del proyecto
            string physicalPath = Path.Combine(_environment.ContentRootPath, normalizedPath);
            
            return physicalPath;
        }

        /// <summary>
        /// Verifica si un archivo existe en el servidor
        /// </summary>
        public bool FileExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            try
            {
                string physicalPath = GetPhysicalPath(filePath);
                return File.Exists(physicalPath);
            }
            catch
            {
                return false;
            }
        }
    }

    // ========================================================================
    // NOTAS PEDAGÓGICAS ADICIONALES SOBRE UPLOAD SEGURO DE ARCHIVOS
    // ========================================================================
    //
    // 1. ¿POR QUÉ VALIDAR LA FIRMA DEL ARCHIVO (MAGIC NUMBERS)?
    //    
    //    Un atacante podría:
    //    - Renombrar un ejecutable malicioso de "virus.exe" a "documento.pdf"
    //    - Cambiar el Content-Type en el request HTTP
    //    
    //    Pero NO puede cambiar la firma interna del archivo.
    //    Los primeros bytes de un PDF SIEMPRE son: %PDF (0x25504446)
    //    Los primeros bytes de un EXE SIEMPRE son: MZ (0x4D5A)
    //    
    //    Esta validación detecta archivos renombrados maliciosamente.
    //
    // 2. ¿POR QUÉ USAR GUIDS PARA NOMBRES DE ARCHIVO?
    //    
    //    Problemas con nombres originales:
    //    - "../../etc/passwd.pdf" → Directory traversal
    //    - "file<script>.pdf" → XSS si se muestra sin sanitizar
    //    - "documento.pdf" → Múltiples usuarios, colisión de nombres
    //    - "cedula_1-0234-0567.pdf" → Expone información sensible
    //    
    //    GUIDs son:
    //    - Únicos globalmente (no hay colisiones)
    //    - Sin caracteres especiales
    //    - Sin información sensible
    //    - Imposibles de predecir
    //
    // 3. ¿POR QUÉ ALMACENAR FUERA DE WWWROOT?
    //    
    //    Si guardamos en wwwroot/uploads/file.pdf:
    //    - El archivo es accesible públicamente en https://sitio.com/uploads/file.pdf
    //    - No podemos controlar QUIÉN descarga el archivo
    //    - No podemos implementar autorización
    //    
    //    Al guardar fuera de wwwroot:
    //    - Solo accesible mediante un controlador
    //    - Podemos verificar [Authorize] antes de servir el archivo
    //    - Podemos registrar auditoría de descargas
    //    - Podemos implementar rate limiting
    //
    // 4. EXTENSIONES PELIGROSAS A BLOQUEAR SIEMPRE:
    //    
    //    Nunca permitir: .exe, .dll, .bat, .cmd, .ps1, .sh, .vbs, .js
    //    Peligrosos si mal configurados: .php, .asp, .aspx, .jsp
    //    
    //    Incluso con validación de extensión, validar magic numbers.
    //
    // 5. LÍMITES DE TAMAÑO:
    //    
    //    Sin límites, un atacante puede:
    //    - Llenar el disco del servidor (DoS)
    //    - Hacer que el servidor use toda la RAM procesando el archivo
    //    - Subir archivos masivos que ralenticen backups
    //    
    //    5MB es suficiente para documentos PDF normales.
    //
    // 6. VALIDACIÓN DEL LADO SERVIDOR ES OBLIGATORIA:
    //    
    //    Validación cliente (JavaScript): Fácil de bypassear
    //    Validación servidor: Última línea de defensa crítica
    //    
    //    Siempre asumir que el cliente es malicioso.
    //
    // 7. CONSIDERACIONES ADICIONALES PARA PRODUCCIÓN:
    //    
    //    - Escaneo antivirus de archivos subidos
    //    - Almacenamiento en blob storage (Azure, S3) en lugar de disco local
    //    - CDN para servir archivos públicos de forma eficiente
    //    - Compresión automática de archivos grandes
    //    - Versionado de archivos si se reemplazan
    //    - Limpieza automática de archivos huérfanos
    //    - Cuotas por usuario (ej: máximo 100 archivos)
    //    - Rate limiting en endpoints de upload
    //
}
