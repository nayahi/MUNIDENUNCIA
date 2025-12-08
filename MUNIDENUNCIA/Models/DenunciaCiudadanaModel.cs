using System;

namespace MUNIDENUNCIA.Models
{
    /// <summary>
    /// ViewModel para crear y editar denuncia ciudadana
    /// ADVERTENCIA: Este modelo NO tiene validación - Es intencional para que los estudiantes la agreguen
    /// Los estudiantes deben agregar Data Annotations apropiados a cada propiedad
    /// </summary>
    public class DenunciaCiudadanaModel
    {
        // TAREA: Agregar atributos de validación apropiados a cada propiedad

        // ID para operaciones de edición
        public int Id { get; set; }

        // Información del Ciudadano
        public string Cedula { get; set; }
        
        public string NombreCompleto { get; set; }
        
        public string Email { get; set; }
        
        public string Telefono { get; set; }

        // Información de la Denuncia
        public CategoriaDenuncia Categoria { get; set; }
        
        public string Ubicacion { get; set; }
        
        public string Descripcion { get; set; }
    }
}
