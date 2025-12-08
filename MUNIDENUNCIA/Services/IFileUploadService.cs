using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MUNIDENUNCIA.Services
{
    /// <summary>
    /// Interfaz para el servicio de gestión segura de archivos PDF
    /// SEMANA 4: Manejo Seguro de Archivos y Uploads
    /// 
    /// Este servicio implementa las mejores prácticas para upload de archivos:
    /// - Validación de tipo MIME
    /// - Validación de extensión
    /// - Validación de tamaño
    /// - Nombres de archivo aleatorios (previene directory traversal)
    /// - Almacenamiento en directorio controlado
    /// - Prevención de ejecución de archivos maliciosos
    /// </summary>
    public interface IFileUploadService
    {
        /// <summary>
        /// Valida un archivo PDF según reglas de seguridad
        /// </summary>
        /// <param name="file">Archivo a validar</param>
        /// <returns>Resultado de validación con mensaje de error si falla</returns>
        Task<FileValidationResult> ValidatePdfFileAsync(IFormFile file);

        /// <summary>
        /// Guarda un archivo PDF de forma segura en el servidor
        /// </summary>
        /// <param name="file">Archivo a guardar</param>
        /// <param name="subfolder">Subcarpeta dentro del directorio de uploads (ej: "denuncias")</param>
        /// <returns>Resultado con información del archivo guardado</returns>
        Task<FileUploadResult> SavePdfFileAsync(IFormFile file, string subfolder);

        /// <summary>
        /// Elimina un archivo del servidor de forma segura
        /// </summary>
        /// <param name="filePath">Ruta completa del archivo a eliminar</param>
        /// <returns>True si se eliminó exitosamente, False si no existía o hubo error</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Obtiene la ruta física completa de un archivo
        /// </summary>
        /// <param name="relativePath">Ruta relativa del archivo</param>
        /// <returns>Ruta física completa</returns>
        string GetPhysicalPath(string relativePath);

        /// <summary>
        /// Verifica si un archivo existe en el servidor
        /// </summary>
        /// <param name="filePath">Ruta del archivo</param>
        /// <returns>True si existe, False si no</returns>
        bool FileExists(string filePath);
    }

    /// <summary>
    /// Resultado de validación de archivo
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public static FileValidationResult Success()
        {
            return new FileValidationResult { IsValid = true };
        }

        public static FileValidationResult Failure(string errorMessage)
        {
            return new FileValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = errorMessage 
            };
        }
    }

    /// <summary>
    /// Resultado de operación de upload de archivo
    /// </summary>
    public class FileUploadResult
    {
        /// <summary>
        /// Indica si el upload fue exitoso
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mensaje de error si falló
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Nombre original del archivo subido por el usuario
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// Nombre del archivo guardado en el servidor (aleatorio)
        /// </summary>
        public string ServerFileName { get; set; }

        /// <summary>
        /// Ruta relativa donde se guardó el archivo
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Ruta física completa del archivo en el servidor
        /// </summary>
        public string PhysicalPath { get; set; }

        /// <summary>
        /// Tamaño del archivo en bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Tipo MIME del archivo
        /// </summary>
        public string MimeType { get; set; }
    }
}
