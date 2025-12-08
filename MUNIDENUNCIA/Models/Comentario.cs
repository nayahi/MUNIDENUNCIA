using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUNIDENUNCIA.Models
{

    /// <summary>
    /// Representa un comentario u observación realizada por un funcionario municipal
    /// sobre una solicitud de permiso de construcción
    /// </summary>
    public class Comentario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SolicitudPermisoId { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreFuncionario { get; set; }

        [Required]
        [StringLength(50)]
        public string CargoFuncionario { get; set; }

        [Required]
        [StringLength(2000)]
        public string TextoComentario { get; set; }

        [Required]
        public DateTime FechaComentario { get; set; }

        public bool EsAprobacion { get; set; }

        public bool EsRechazo { get; set; }

        // Navegación
        [ForeignKey("SolicitudPermisoId")]
        public virtual SolicitudPermiso SolicitudPermiso { get; set; }

        public Comentario()
        {
            FechaComentario = DateTime.UtcNow;
            EsAprobacion = false;
            EsRechazo = false;
        }
    }
}
