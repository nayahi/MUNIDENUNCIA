using Microsoft.AspNetCore.Http;
using MUNIDENUNCIA.Models;
using System.ComponentModel.DataAnnotations;

namespace MUNIDENUNCIA.ViewModels
{
    /// <summary>
    /// ViewModel para editar una denuncia existente
    /// SEMANA 4: Permite actualizar datos y reemplazar archivo PDF
    /// </summary>
    public class EditarDenunciaViewModel
    {
        [Required]
        public int Id { get; set; }

        // ========================================================================
        // INFORMACIÓN DEL CIUDADANO - Editable
        // ========================================================================

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [RegularExpression(@"^\d{1}-\d{4}-\d{4}$", 
            ErrorMessage = "La cédula debe tener el formato: 1-0234-0567")]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100, MinimumLength = 5, 
            ErrorMessage = "El nombre debe tener entre 5 y 100 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{4}-\d{4}$", 
            ErrorMessage = "El teléfono debe tener el formato: 2222-3333")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        // ========================================================================
        // INFORMACIÓN DE LA DENUNCIA - Editable
        // ========================================================================

        [Required(ErrorMessage = "La categoría es obligatoria")]
        [Display(Name = "Categoría de la Denuncia")]
        public CategoriaDenuncia Categoria { get; set; }

        [Required(ErrorMessage = "La ubicación es obligatoria")]
        [StringLength(200, MinimumLength = 10,
            ErrorMessage = "La ubicación debe tener entre 10 y 200 caracteres")]
        [Display(Name = "Ubicación del Problema")]
        public string Ubicacion { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(1000, MinimumLength = 20,
            ErrorMessage = "La descripción debe tener entre 20 y 1000 caracteres")]
        [Display(Name = "Descripción Detallada")]
        [DataType(DataType.MultilineText)]
        public string Descripcion { get; set; }

        // ========================================================================
        // ARCHIVO PDF - SEMANA 4
        // ========================================================================

        /// <summary>
        /// Nuevo archivo PDF (opcional)
        /// Si se proporciona, reemplazará al archivo existente
        /// </summary>
        [Display(Name = "Reemplazar Evidencia (PDF)")]
        public IFormFile NuevoArchivoPdf { get; set; }

        /// <summary>
        /// Indica si ya existe un archivo PDF adjunto
        /// </summary>
        public bool TieneArchivoExistente { get; set; }

        /// <summary>
        /// Nombre del archivo PDF existente (para mostrarlo al usuario)
        /// </summary>
        public string NombreArchivoExistente { get; set; }

        /// <summary>
        /// Indica si el usuario quiere eliminar el archivo existente
        /// </summary>
        [Display(Name = "Eliminar archivo existente")]
        public bool EliminarArchivoExistente { get; set; }
    }
}
