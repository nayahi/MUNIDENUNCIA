using System.ComponentModel.DataAnnotations;

namespace MUNIDENUNCIA.ViewModels
{
    /// <summary>
    /// ViewModel para cambiar el estado de una denuncia
    /// SEMANA 4: Solo funcionarios con rol apropiado pueden cambiar estados
    /// </summary>
    public class CambiarEstadoDenunciaViewModel
    {
        [Required]
        public int DenunciaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un nuevo estado")]
        [Display(Name = "Nuevo Estado")]
        public string NuevoEstado { get; set; }

        [Required(ErrorMessage = "Las observaciones son obligatorias")]
        [StringLength(500, MinimumLength = 10,
            ErrorMessage = "Las observaciones deben tener entre 10 y 500 caracteres")]
        [Display(Name = "Observaciones")]
        [DataType(DataType.MultilineText)]
        public string Observaciones { get; set; }

        // Información de la denuncia (solo lectura para mostrar contexto)
        public string CategoriaActual { get; set; }
        public string UbicacionActual { get; set; }
        public string EstadoActual { get; set; }
        public string NombreCiudadano { get; set; }
    }
}
