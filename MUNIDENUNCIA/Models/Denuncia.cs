using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUNIDENUNCIA.Models
{
    /// <summary>
    /// Entidad que representa una denuncia ciudadana en el sistema
    /// municipal de San José
    /// </summary>
    public class Denuncia
    {
        [Key]
        public int Id { get; set; }

        // Información del Ciudadano
        [Required]
        [StringLength(20)]
        public string Cedula { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreCompleto { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefono { get; set; }

        // Información de la Denuncia
        [Required]
        public CategoriaDenuncia Categoria { get; set; }

        [Required]
        [StringLength(200)]
        public string Ubicacion { get; set; }

        [Required]
        [StringLength(1000)]
        public string Descripcion { get; set; }

        // Control del Sistema
        [Required]
        public string Estado { get; set; } // Pendiente, EnRevision, Resuelta, Cerrada

        [Required]
        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaResolucion { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public Denuncia()
        {
            FechaCreacion = DateTime.UtcNow;
            Estado = "Pendiente";
        }
    }

    /// <summary>
    /// Categorías de denuncias ciudadanas en San José
    /// </summary>
    public enum CategoriaDenuncia
    {
        [Display(Name = "Bache en Calle")]
        BacheCalle = 1,

        [Display(Name = "Alumbrado Público Defectuoso")]
        AlumbradoPublico = 2,

        [Display(Name = "Acumulación de Basura")]
        AcumulacionBasura = 3,

        [Display(Name = "Problema con Alcantarillado")]
        ProblemaAlcantarillado = 4,

        [Display(Name = "Vandalismo o Grafiti")]
        VandalismoGrafiti = 5,

        [Display(Name = "Señalización Vial Dañada")]
        SenalizacionDanada = 6,

        [Display(Name = "Parque o Área Verde Descuidada")]
        ParqueDescuidado = 7,

        [Display(Name = "Ruido Excesivo")]
        RuidoExcesivo = 8,

        [Display(Name = "Otros")]
        Otros = 99
    }
}
