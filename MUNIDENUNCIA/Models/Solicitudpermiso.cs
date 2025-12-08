using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUNIDENUNCIA.Models
{
    /// <summary>
    /// Entidad que representa una solicitud de permiso de construcción
    /// </summary>
    public class SolicitudPermiso
    {
        [Key]
        public int Id { get; set; }

        // Información del Propietario
        [Required]
        [StringLength(20)]
        public string CedulaPropietario { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreCompletoPropietario { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string EmailPropietario { get; set; }

        [Required]
        [StringLength(20)]
        public string TelefonoPropietario { get; set; }

        // Información de la Propiedad
        [Required]
        [StringLength(50)]
        public string Distrito { get; set; }

        [Required]
        [StringLength(300)]
        public string DireccionCompleta { get; set; }

        [Required]
        [StringLength(30)]
        public string PlanoCatastrado { get; set; }

        // Información del Proyecto
        [Required]
        public TipoConstruccion TipoConstruccion { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal AreaConstruccionM2 { get; set; }

        [Required]
        public int NumeroPlantas { get; set; }

        [Required]
        [StringLength(2000)]
        public string DescripcionProyecto { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PresupuestoEstimado { get; set; }

        // Control y Estado
        [Required]
        public EstadoSolicitud Estado { get; set; }

        [Required]
        public DateTime FechaSolicitud { get; set; }

        public DateTime? FechaRevision { get; set; }

        public DateTime? FechaAprobacion { get; set; }

        [StringLength(50)]
        public string? RevisadoPor { get; set; }

        // Navegación
        public virtual ICollection<Comentario> Comentarios { get; set; }

        public SolicitudPermiso()
        {
            FechaSolicitud = DateTime.UtcNow;
            Estado = EstadoSolicitud.Pendiente;
            Comentarios = new HashSet<Comentario>();
        }
    }

    /// <summary>
    /// </summary>
    public enum TipoConstruccion
    {
        [Display(Name = "Vivienda Unifamiliar")]
        ViviendaUnifamiliar = 1,

        [Display(Name = "Apartamentos Múltiples")]
        ApartamentosMultiples = 2,

        [Display(Name = "Local Comercial")]
        LocalComercial = 3,

        [Display(Name = "Ampliación de Vivienda")]
        AmpliacionVivienda = 4,

        [Display(Name = "Remodelación Estructural")]
        RemodelacionEstructural = 5,

        [Display(Name = "Oficinas")]
        Oficinas = 6,

        [Display(Name = "Industria Liviana")]
        IndustriaLiviana = 7
    }

    /// <summary>
    /// Estados posibles de una solicitud
    /// </summary>
    public enum EstadoSolicitud
    {
        [Display(Name = "Pendiente de Revisión")]
        Pendiente = 1,

        [Display(Name = "En Revisión")]
        EnRevision = 2,

        [Display(Name = "Requiere Correcciones")]
        RequiereCorrecciones = 3,

        [Display(Name = "Aprobada")]
        Aprobada = 4,

        [Display(Name = "Denegada")]
        Denegada = 5,

        [Display(Name = "Cancelada")]
        Cancelada = 6
    }
}
